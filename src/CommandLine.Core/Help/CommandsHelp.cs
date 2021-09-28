﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Orang.CommandLine.Help
{
    public class CommandsHelp
    {
        public CommandsHelp(
            ImmutableArray<CommandItem> commands,
            ImmutableArray<OptionValueItemList> values)
        {
            Commands = commands;
            Values = values;
        }

        public ImmutableArray<CommandItem> Commands { get; }

        public ImmutableArray<OptionValueItemList> Values { get; }

        public static CommandsHelp Create(
            IEnumerable<Command> commands,
            IEnumerable<OptionValueProvider>? providers = null,
            Filter? filter = null)
        {
            ImmutableArray<CommandItem> commandsHelp = HelpProvider.GetCommandItems(commands, filter);

            ImmutableArray<OptionValueItemList> values = HelpProvider.GetOptionValues(
                commands.SelectMany(f => f.Options),
                providers ?? ImmutableArray<OptionValueProvider>.Empty,
                filter);

            return new CommandsHelp(commandsHelp, values);
        }
    }
}
