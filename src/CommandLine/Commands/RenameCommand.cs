﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Orang.FileSystem;
using Orang.Text.RegularExpressions;
using static Orang.CommandLine.LogHelpers;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal class RenameCommand : DeleteOrRenameCommand<RenameCommandOptions>
    {
        public RenameCommand(RenameCommandOptions options) : base(options)
        {
            Debug.Assert(options.ContentFilter?.IsNegative != true);
        }

        public ConflictResolution ConflictResolution
        {
            get { return Options.ConflictResolution; }
            protected set { Options.ConflictResolution = value; }
        }

        protected override FileSystemFilterOptions CreateFilterOptions()
        {
            return new FileSystemFilterOptions(
                searchTarget: Options.SearchTarget,
                recurseSubdirectories: Options.RecurseSubdirectories,
                canEnumerate: Options.DryRun,
                partOnly: true,
                encoding: Options.DefaultEncoding);
        }

        protected override void ProcessMatch(
            FileMatch fileMatch,
            SearchContext context,
            string baseDirectoryPath = null)
        {
            ExecuteOrAddMatch(fileMatch, context, baseDirectoryPath);
        }

        protected override void ExecuteMatch(
            FileMatch fileMatch,
            SearchContext context,
            string baseDirectoryPath,
            ColumnWidths columnWidths)
        {
            string indent = GetPathIndent(baseDirectoryPath);

            List<ReplaceItem> replaceItems = GetReplaceItems(fileMatch, context.CancellationToken);

            string path = fileMatch.Path;
            string newPath = GetNewPath(fileMatch, replaceItems);
            bool changed = !string.Equals(path, newPath, StringComparison.Ordinal);

            if (!Options.OmitPath
                && changed)
            {
                LogHelpers.WritePath(
                    fileMatch,
                    replaceItems,
                    baseDirectoryPath,
                    relativePath: Options.DisplayRelativePath,
                    colors: Colors.Matched_Path,
                    matchColors: (Options.HighlightMatch) ? Colors.Match : default,
                    replaceColors: (Options.HighlightReplacement) ? Colors.Replacement : default,
                    indent: indent,
                    verbosity: Verbosity.Minimal);

                WriteProperties(context, fileMatch, columnWidths);
                WriteLine(Verbosity.Minimal);
            }

            ListCache<ReplaceItem>.Free(replaceItems);

            if (!changed)
                return;

            if (FileSystemHelpers.ContainsInvalidFileNameChars(newPath, FileSystemHelpers.GetFileNameIndex(path)))
            {
                WriteWarning($"{indent}New file name contains invalid character(s).", verbosity: Verbosity.Normal);
                return;
            }

            bool fileExists = File.Exists(newPath);
            bool directoryExists = !fileExists && Directory.Exists(newPath);

            if (fileExists
                || directoryExists)
            {
                if (ConflictResolution == ConflictResolution.Skip)
                    return;
            }

            string question;
            if (fileExists)
            {
                question = "Rename (and delete existing file)?";
            }
            else if (directoryExists)
            {
                question = "Rename (and delete existing directory)?";
            }
            else
            {
                question = "Rename?";
            }

            if ((fileExists || directoryExists)
                && ConflictResolution == ConflictResolution.Ask)
            {
                if (!AskToOverwrite(context, question, indent))
                    return;
            }
            else if (Options.Ask)
            {
                if (!AskToExecute(context, question, indent))
                    return;
            }

            bool renamed = false;

            try
            {
                if (!Options.DryRun)
                {
                    if (fileExists)
                    {
                        File.Delete(newPath);
                    }
                    else if (directoryExists)
                    {
                        Directory.Delete(newPath, recursive: true);
                    }

                    if (fileMatch.IsDirectory)
                    {
                        Directory.Move(path, newPath);
                    }
                    else
                    {
                        File.Move(path, newPath);
                    }

                    renamed = true;
                }

                if (fileMatch.IsDirectory)
                {
                    context.Telemetry.ProcessedDirectoryCount++;
                }
                else
                {
                    context.Telemetry.ProcessedFileCount++;
                }
            }
            catch (Exception ex) when (ex is IOException
                || ex is UnauthorizedAccessException)
            {
                WriteFileError(ex, indent: indent);
            }

            if (fileMatch.IsDirectory
                && renamed)
            {
                OnDirectoryChanged(new DirectoryChangedEventArgs(path, newPath));
            }
        }

        private List<ReplaceItem> GetReplaceItems(FileMatch fileMatch, CancellationToken cancellationToken)
        {
            List<Match> matches = GetMatches(fileMatch.NameMatch);

            int offset = 0;
            List<ReplaceItem> items = ListCache<ReplaceItem>.GetInstance();

            foreach (Match match in matches)
            {
                string value = Options.ReplaceOptions.Replace(match);

                items.Add(new ReplaceItem(match, value, match.Index + offset));

                offset += value.Length - match.Length;

                cancellationToken.ThrowIfCancellationRequested();
            }

            ListCache<Match>.Free(matches);

            return items;

            static List<Match> GetMatches(Match match)
            {
                List<Match> matches = ListCache<Match>.GetInstance();

                do
                {
                    matches.Add(match);

                    match = match.NextMatch();

                } while (match.Success);

                if (matches.Count > 1
                    && matches[0].Index > matches[1].Index)
                {
                    matches.Reverse();
                }

                return matches;
            }
        }

        private string GetNewPath(FileMatch fileMatch, List<ReplaceItem> items)
        {
            StringBuilder sb = StringBuilderCache.GetInstance();

            string path = fileMatch.Path;
            NamePart part = fileMatch.Part;

            sb.Append(path, 0, part.Index);

            int lastPos = part.Index;

            foreach (ReplaceItem item in items)
            {
                Match match = item.Match;

                sb.Append(path, lastPos, part.Index + match.Index - lastPos);
                sb.Append(item.Value);

                lastPos = part.Index + match.Index + match.Length;
            }

            sb.Append(path, lastPos, path.Length - lastPos);

            return StringBuilderCache.GetStringAndFree(sb);
        }

        protected override void WriteSummary(SearchTelemetry telemetry, Verbosity verbosity)
        {
            WriteSearchedFilesAndDirectories(telemetry, Options.SearchTarget, verbosity);
            WriteProcessedFilesAndDirectories(telemetry, Options.SearchTarget, "Renamed files", "Renamed directories", Options.DryRun, verbosity);
        }

        private bool AskToOverwrite(SearchContext context, string question, string indent)
        {
            DialogResult result = ConsoleHelpers.Ask(question, indent);

            switch (result)
            {
                case DialogResult.Yes:
                    {
                        return true;
                    }
                case DialogResult.YesToAll:
                    {
                        ConflictResolution = ConflictResolution.Overwrite;
                        return true;
                    }
                case DialogResult.No:
                case DialogResult.None:
                    {
                        return false;
                    }
                case DialogResult.NoToAll:
                    {
                        ConflictResolution = ConflictResolution.Skip;
                        return false;
                    }
                case DialogResult.Cancel:
                    {
                        context.TerminationReason = TerminationReason.Canceled;
                        return false;
                    }
                default:
                    {
                        throw new InvalidOperationException($"Unknown enum value '{result}'.");
                    }
            }
        }
    }
}
