﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Orang.CommandLine.Annotations;
using Orang.CommandLine.Help;
using static Orang.Logger;

namespace Orang.CommandLine
{
    internal class HelpCommand : AbstractCommand<HelpCommandOptions>
    {
        public HelpCommand(HelpCommandOptions options) : base(options)
        {
        }

        protected override CommandResult ExecuteCore(CancellationToken cancellationToken = default)
        {
            try
            {
                WriteHelp(
                    commandName: Options.Command,
                    online: Options.Online,
                    manual: Options.Manual,
                    includeValues: ConsoleOut.Verbosity > Verbosity.Normal,
                    filter: Options.Filter);
            }
            catch (ArgumentException ex)
            {
                WriteError(ex);
                return CommandResult.Fail;
            }

            return CommandResult.Success;
        }

        private static void WriteHelp(
            string[] commandName,
            bool online,
            bool manual,
            bool includeValues,
            Filter? filter = null)
        {
            var isCompoundCommand = false;
            string? commandName2 = commandName.FirstOrDefault();

            if (commandName2 != null)
                isCompoundCommand = CommandUtility.IsCompoundCommand(commandName2);

            if (commandName.Length > 0)
                CommandUtility.CheckCommandName(ref commandName, showErrorMessage: false);

            commandName2 = commandName.FirstOrDefault();

            if (online)
            {
                OpenHelpInBrowser(commandName2);
            }
            else if (commandName2 != null)
            {
                Command? command = null;

                if (!isCompoundCommand)
                    command = CommandLoader.LoadCommand(typeof(HelpCommand).Assembly, commandName2);

                if (command == null)
                    throw new InvalidOperationException($"Command '{string.Join(' ', commandName)}' does not exist.");

                WriteCommandHelp(command, includeValues: includeValues, filter: filter);
            }
            else if (manual)
            {
                WriteManual(includeValues: includeValues, filter: filter);
            }
            else
            {
                WriteCommandsHelp(includeValues: includeValues, filter: filter);
            }
        }

        // https://github.com/dotnet/corefx/issues/10361
        private static void OpenHelpInBrowser(string? commandName)
        {
            var url = "http://pihrt.net/redirect?id=orang-cli";

            if (commandName != null)
                url += "-" + commandName;

            url += $"&version={PackageInfo.Version}";
#if DEBUG
            WriteLine(url, Verbosity.Normal);
#endif
            try
            {
                Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var psi = new ProcessStartInfo()
                    {
                        FileName = url,
                        UseShellExecute = true
                    };

                    Process.Start(psi);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }

        public static void WriteCommandHelp(Command command, bool includeValues = false, Filter? filter = null)
        {
            var writer = new ConsoleHelpWriter(new HelpWriterOptions(filter: filter));

            IEnumerable<CommandOption> options = command.Options
                .Where(f => !f.PropertyInfo.GetCustomAttributes().Any(f => f is HideFromHelpAttribute || f is HideFromConsoleHelpAttribute))
                .OrderBy(f => f, CommandOptionComparer.Name);

            command = command.WithOptions(options);

            CommandHelp commandHelp = CommandHelp.Create(command, OptionValueProviders.Providers, filter: filter);

            writer.WriteCommand(commandHelp);

            if (includeValues)
            {
                writer.WriteValues(commandHelp.Values);
            }
            else
            {
                IEnumerable<string> metaValues = OptionValueProviders.GetProviders(
                    commandHelp.Options.Select(f => f.Option),
                    OptionValueProviders.Providers)
                    .Select(f => f.Name);

                if (metaValues.Any())
                {
                    WriteLine();
                    Write($"Run 'orang help {command.DisplayName} -v d' to display list of option values for ");
                    Write(TextHelpers.Join(", ", " and ", metaValues));
                    WriteLine(".");
                }
            }
        }

        public static void WriteCommandsHelp(bool includeValues = false, Filter? filter = null)
        {
            IEnumerable<Command> commands = LoadCommands().Where(f => f.Name != "help");

            CommandsHelp commandsHelp = CommandsHelp.Create(commands, OptionValueProviders.Providers, filter: filter);

            var writer = new ConsoleHelpWriter(new HelpWriterOptions(filter: filter));

            writer.WriteCommands(commandsHelp);

            if (includeValues)
                writer.WriteValues(commandsHelp.Values);

            WriteLine();
            WriteLine(GetFooterText());
        }

        private static void WriteManual(bool includeValues = false, Filter? filter = null)
        {
            IEnumerable<Command> commands = LoadCommands();

            var writer = new ConsoleHelpWriter(new HelpWriterOptions(filter: filter));

            IEnumerable<CommandHelp> commandHelps = commands.Select(f => CommandHelp.Create(f, filter: filter))
                .Where(f => f.Arguments.Any() || f.Options.Any())
                .ToImmutableArray();

            ImmutableArray<CommandItem> commandItems = HelpProvider.GetCommandItems(commandHelps.Select(f => f.Command));

            ImmutableArray<OptionValueItemList> values = ImmutableArray<OptionValueItemList>.Empty;

            if (commandItems.Any())
            {
                values = HelpProvider.GetOptionValues(
                    commandHelps.SelectMany(f => f.Command.Options),
                    OptionValueProviders.Providers,
                    filter);

                var commandsHelp = new CommandsHelp(commandItems, values);

                writer.WriteCommands(commandsHelp);

                foreach (CommandHelp commandHelp in commandHelps)
                {
                    WriteSeparator();
                    WriteLine();
                    WriteLine($"Command: {commandHelp.DisplayName}");
                    WriteLine();

                    string description = commandHelp.Description;

                    if (!string.IsNullOrEmpty(description))
                    {
                        WriteLine(description);
                        WriteLine();
                    }

                    writer.WriteCommand(commandHelp);
                }

                if (includeValues)
                    WriteSeparator();
            }
            else
            {
                WriteLine();
                WriteLine("No command found");

                if (includeValues)
                {
                    values = HelpProvider.GetOptionValues(
                        commands.Select(f => CommandHelp.Create(f)).SelectMany(f => f.Command.Options),
                        OptionValueProviders.Providers,
                        filter);
                }
            }

            if (includeValues)
                writer.WriteValues(values);

            static void WriteSeparator()
            {
                WriteLine();
                WriteLine("----------");
            }
        }

        private static IEnumerable<Command> LoadCommands()
        {
            return CommandLoader.LoadCommands(typeof(HelpCommand).Assembly)
                .OrderBy(f => f.Name, StringComparer.CurrentCulture);
        }

        internal static string GetHeadingText()
        {
            return $"Orang Command-Line Tool version {typeof(Program).GetTypeInfo().Assembly.GetName().Version}";
        }

        internal static string GetFooterText(string? command = null)
        {
            return $"Run 'orang help {command ?? "[command]"}' for more information on a command."
                + Environment.NewLine
                + $"Run 'orang help {command ?? "[command]"} -v d' for more information on option values.";
        }
    }
}
