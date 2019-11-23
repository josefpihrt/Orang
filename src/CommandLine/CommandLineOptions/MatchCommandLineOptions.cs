﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using CommandLine;
using static Orang.CommandLine.ParseHelpers;

namespace Orang.CommandLine
{
    [Verb("match", HelpText = "Searches the input string for occurrences of the regular expression.")]
    [OptionValueProvider(nameof(Content), OptionValueProviderNames.PatternOptionsWithoutPartAndNegative)]
    [OptionValueProvider(nameof(Highlight), OptionValueProviderNames.MatchHighlightOptions)]
    internal class MatchCommandLineOptions : RegexCommandLineOptions
    {
        [Option(shortName: OptionShortNames.Content, longName: OptionNames.Content,
            Required = true,
            HelpText = "Regular expression for the input string. Syntax is <PATTERN> [<PATTERN_OPTIONS>].",
            MetaValue = MetaValues.Regex)]
        public IEnumerable<string> Content { get; set; }

        [Option(shortName: OptionShortNames.MaxCount, longName: OptionNames.MaxCount,
            Default = -1,
            HelpText = "Maximum number of matches returned.",
            MetaValue = MetaValues.Number)]
        public int MaxCount { get; set; }

        public bool TryParse(ref MatchCommandOptions options)
        {
            var baseOptions = (RegexCommandOptions)options;

            if (!TryParse(ref baseOptions))
                return false;

            options = (MatchCommandOptions)baseOptions;

            if (!TryParseFilter(Content, OptionNames.Content, out Filter filter, provider: OptionValueProviders.PatternOptionsWithoutPartAndNegativeProvider))
                return false;

            if (!TryParseAsEnumFlags(Highlight, OptionNames.Highlight, out HighlightOptions highlightOptions, defaultValue: HighlightOptions.Match, provider: OptionValueProviders.MatchHighlightOptionsProvider))
                return false;

            options.Filter = filter;
            options.HighlightOptions = highlightOptions;
            options.MaxCount = MaxCount;

            return true;
        }
    }
}
