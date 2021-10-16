﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Orang.CommandLine
{
    internal sealed class RegexSplitCommandOptions : RegexCommandOptions
    {
        internal RegexSplitCommandOptions()
        {
        }

        public bool OmitGroups { get; internal set; }

        protected override void WriteDiagnosticCore(DiagnosticWriter writer)
        {
            writer.WriteRegexSplitCommand(this);
        }
    }
}
