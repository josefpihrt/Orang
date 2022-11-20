﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Orang.CommandLine;

internal sealed class HelpCommandOptions : AbstractCommandOptions
{
    internal HelpCommandOptions()
    {
    }

    public string[] Command { get; internal set; } = null!;

    public Filter? Filter { get; internal set; }

    public bool Manual { get; internal set; }

    public bool Online { get; internal set; }

    protected override void WriteDiagnosticCore(DiagnosticWriter writer)
    {
        writer.WriteHelpCommand(this);
    }
}
