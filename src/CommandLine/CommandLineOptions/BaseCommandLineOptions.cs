﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using CommandLine;

namespace Orang.CommandLine
{
    internal class BaseCommandLineOptions : AbstractCommandLineOptions
    {
        [Option(shortName: OptionShortNames.Help, longName: OptionNames.Help,
            HelpText = "Show command line help.")]
        public bool Help { get; set; }
    }
}
