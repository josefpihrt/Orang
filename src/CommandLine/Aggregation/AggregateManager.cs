﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Orang.CommandLine;
using static Orang.CommandLine.LogHelpers;
using static Orang.Logger;
using Orang.FileSystem;

namespace Orang.Aggregation
{
    internal sealed class AggregateManager
    {
        private AggregateManager(
            ListResultStorage storage,
            List<StorageSection>? sections,
            FindCommandOptions options)
        {
            Storage = storage;
            Sections = sections;
            Options = options;

            if (ShouldLog(Verbosity.Minimal))
            {
                PathWriter = new PathWriter(
                    pathColors: Colors.Matched_Path,
                    relativePath: Options.DisplayRelativePath,
                    indent: "  ");
            }
        }

        public ListResultStorage Storage { get; set; }

        public List<StorageSection>? Sections { get; set; }

        public FindCommandOptions Options { get; }

        public ModifyOptions ModifyOptions => Options.ModifyOptions;

        private PathWriter? PathWriter { get; }

        public static AggregateManager? TryCreate(FindCommandOptions Options)
        {
            ModifyOptions modifyOptions = Options.ModifyOptions;

            bool shouldCreate = modifyOptions.HasFunction(ModifyFunctions.Except_Intersect_Group)
                || modifyOptions.Aggregate
                || Options.SaveAggregatedValues)

            if (!shouldCreate)
                return null;

            var storage = new ListResultStorage();

            List<StorageSection>? sections = null;

            if (modifyOptions.HasFunction(ModifyFunctions.Except_Intersect))
            {
                sections = new List<StorageSection>();
            }
            else if ((Options.ContentFilter != null || Options.NameFilter != null)
                && modifyOptions.HasFunction(ModifyFunctions.Group))
            {
                sections = new List<StorageSection>();
            }

            return new AggregateManager(storage, sections, Options);
        }

        public void WriteAggregatedValues(CancellationToken cancellationToken)
        {
            PathInfo pathInfo = Options.Paths[0];

            bool savingAggregatedValues = Options.SaveAggregatedValues
                && pathInfo.Origin != PathOrigin.CurrentDirectory
                && File.Exists(pathInfo.Path);

            if (savingAggregatedValues)
            {
                TextWriterWithVerbosity? originalOut = Out;

                try
                {
                    using (var stream = new FileStream(pathInfo.Path, FileMode.Create, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        if (originalOut != null)
                        {
                            var writer2 = new TextWriter<TextWriter, TextWriter>(originalOut.Writer, writer);
                            Out = new TextWriterWithVerbosity(writer2) { Verbosity = originalOut.Verbosity };
                        }
                        else
                        {
                            Out = new TextWriterWithVerbosity(writer) { Verbosity = Verbosity.Normal };
                        }

                        WriteAggregatedValuesImpl(savingAggregatedValues, cancellationToken);
                    }
                }
                finally
                {
                    Out = originalOut;
                }
            }
            else
            {
                WriteAggregatedValuesImpl(savingAggregatedValues, cancellationToken);
            }
        }

        private void WriteAggregatedValuesImpl(bool savingAggregatedValues, CancellationToken cancellationToken)
        {
            int count = 0;

            IEnumerable<string> values = Storage.Values;

            if (Sections?.Count > 1
                && (ModifyOptions.HasFunction(ModifyFunctions.Except_Intersect)))
            {
                if (ModifyOptions.HasFunction(ModifyFunctions.Except))
                {
                    values = ExceptOrIntersect(Storage.Values, (x, y, c) => x.Except(y, c));
                }
                else if (ModifyOptions.HasFunction(ModifyFunctions.Intersect))
                {
                    values = ExceptOrIntersect(Storage.Values, (x, y, c) => x.Intersect(y, c));
                }
            }

            Dictionary<string, List<StorageSection>>? valuesMap = null;

            if (Sections?.Count > 0
                && ModifyOptions.HasFunction(ModifyFunctions.Group))
            {
                if (Options.ContentFilter != null)
                {
                    valuesMap = GroupByValues(Storage.Values);
                }
                else
                {
                    valuesMap = Storage.Values
                        .Zip(Sections)
                        .GroupBy(f => f.First, ModifyOptions.StringComparer)
                        .ToDictionary(f => f.Key, f => f.Select(f => f.Second).ToList(), ModifyOptions.StringComparer);
                }

                values = valuesMap
                    .Select(f => f.Key)
                    .Modify(ModifyOptions, filter: ModifyFunctions.Enumerable & ~ModifyFunctions.Distinct);
            }
            else
            {
                values = values.Modify(ModifyOptions, filter: ModifyFunctions.Enumerable);
            }

            using (IEnumerator<string> en = values.GetEnumerator())
            {
                if (en.MoveNext())
                {
                    OutputSymbols symbols = OutputSymbols.Create(Options.HighlightOptions);

                    ConsoleColors colors = ((Options.HighlightOptions & HighlightOptions.Match) != 0)
                        ? Colors.Match
                        : default;

                    ConsoleColors boundaryColors = (Options.HighlightBoundary) ? Colors.MatchBoundary : default;
                    var valueWriter = new ValueWriter(new ContentTextWriter(Verbosity.Minimal), includeEndingIndent: false);

                    if (!Options.AggregateOnly)
                    {
                        ConsoleOut.WriteLineIf(ShouldWriteLine(ConsoleOut.Verbosity));

                        if (Out != null
                            && ShouldWriteLine(Out.Verbosity))
                        {
                            if (Out.Writer is TextWriter<TextWriter, TextWriter> writer2)
                            {
                                writer2.Writer1.WriteLine();
                            }
                            else if (!savingAggregatedValues)
                            {
                                Out.WriteLine();
                            }
                        }
                    }

                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        valueWriter.Write(en.Current, symbols, colors, boundaryColors);

                        if (valuesMap != null
                            && ShouldLog(Verbosity.Detailed))
                        {
                            int groupCount = valuesMap[en.Current].Count;

                            Write("  ", Colors.Message_OK, Verbosity.Detailed);
                            Write(groupCount.ToString("n0"), Colors.Message_OK, Verbosity.Detailed);
                        }

                        WriteLine(Verbosity.Minimal);

                        if (valuesMap != null)
                            WriteGroup(valuesMap[en.Current], cancellationToken);

                        count++;

                    } while (en.MoveNext());

                    if (Options.IncludeSummary
                        || ShouldLog(Verbosity.Detailed))
                    {
                        Verbosity verbosity = (Options.IncludeSummary)
                            ? Verbosity.Minimal
                            : Verbosity.Detailed;

                        if (valuesMap != null)
                            count = valuesMap.Sum(f => f.Value.Count);

                        WriteLine(verbosity);

                        if (valuesMap != null)
                        {
                            WriteCount("Groups", valuesMap.Count, verbosity: verbosity);
                            Write("  ", verbosity);
                        }

                        WriteCount("Values", count, verbosity: verbosity);
                        WriteLine(verbosity);
                    }
                }
            }

            bool ShouldWriteLine(Verbosity verbosity)
            {
                if (verbosity > Verbosity.Minimal)
                    return true;

                return verbosity == Verbosity.Minimal
                    && (Options.PathDisplayStyle != PathDisplayStyle.Omit || Options.IncludeSummary);
            }
        }

        private List<string> ExceptOrIntersect(
            List<string> values,
            Func<IEnumerable<string>, IEnumerable<string>, IEqualityComparer<string>, IEnumerable<string>> operation)
        {
            var list = new List<string>(GetRange(values, 0, Sections![0].Count));

            for (int i = 1; i < Sections.Count; i++)
            {
                if (list.Count == 0)
                    break;

                IEnumerable<string> second = GetRange(
                    values,
                    Sections[i - 1].Count,
                    Sections[i].Count);

                list = operation(list, second, ModifyOptions.StringComparer).ToList();
            }

            return list;
        }

        private Dictionary<string, List<StorageSection>> GroupByValues(List<string> values)
        {
            var sectionsValues = new List<(StorageSection section, IEnumerable<string> values)>()
            {
                (Sections![0], GetRange(values, 0, Sections[0].Count))
            };

            for (int i = 1; i < Sections.Count; i++)
            {
                IEnumerable<string> second = GetRange(
                    values,
                    Sections[i - 1].Count,
                    Sections[i].Count);

                sectionsValues.Add((Sections[i], second));
            }

            return sectionsValues
                .SelectMany(f => f.values.Select(value => (f.section, value)))
                .GroupBy(f => f.value, ModifyOptions.StringComparer)
                .ToDictionary(f => f.Key, f => f.Select(g => g.section).ToList(), ModifyOptions.StringComparer);
        }

        private IEnumerable<string> GetRange(List<string> values, int start, int end)
        {
            for (int i = start; i < end; i++)
                yield return values[i];
        }

        private void WriteGroup(
            IEnumerable<StorageSection> sections,
            CancellationToken cancellationToken)
        {
            SortOptions? sortOptions = Options.SortOptions;

            if (sortOptions?.Descriptors.Any() == true)
            {
                List<SearchResult>? results = sections
                    .Select(f => new SearchResult(f.FileMatch, f.BaseDirectoryPath))
                    .ToList();

                PathDisplayStyle pathDisplayStyle = (Options.PathDisplayStyle == PathDisplayStyle.Relative)
                    ? PathDisplayStyle.Relative
                    : PathDisplayStyle.Full;

                sections = SortHelpers.SortResults(results, sortOptions, pathDisplayStyle)
                    .Select(f => new StorageSection(f.FileMatch, f.BaseDirectoryPath, -1));
            }

            foreach (IGrouping<StorageSection, StorageSection>? grouping in sections
                .GroupBy(f => f, StorageSectionComparer.Path))
            {
                cancellationToken.ThrowIfCancellationRequested();

                PathWriter?.WritePath(grouping.Key.FileMatch, grouping.Key.BaseDirectoryPath);

                int pathCount = grouping.Count();

                if (pathCount > 1)
                {
                    Write("  ", Colors.Message_OK, Verbosity.Minimal);
                    Write(pathCount.ToString("n0"), Colors.Message_OK, Verbosity.Minimal);
                }

                WriteLine(Verbosity.Minimal);
            }
        }
    }
}
