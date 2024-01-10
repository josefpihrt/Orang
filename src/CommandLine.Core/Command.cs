﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Orang;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class Command
{
    public Command(
        string name,
        string description,
        CommandGroup group,
        string? alias = null,
        IEnumerable<CommandArgument>? arguments = null,
        IEnumerable<CommandOption>? options = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description;
        Group = group;
        Alias = alias;
        Arguments = arguments?.ToImmutableArray() ?? ImmutableArray<CommandArgument>.Empty;
        Options = options?.ToImmutableArray() ?? ImmutableArray<CommandOption>.Empty;
    }

    public string Name { get; }

    public string Description { get; }

    public CommandGroup Group { get; }

    public string? Alias { get; }

    public string DisplayName => Alias ?? Name;

    public ImmutableArray<CommandArgument> Arguments { get; }

    public ImmutableArray<CommandOption> Options { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => DisplayName + "  " + Description;

    public Command WithArguments(IEnumerable<CommandArgument> arguments)
    {
        return new(Name, Description, Group, Alias, arguments, Options);
    }

    public Command WithOptions(IEnumerable<CommandOption> options)
    {
        return new(Name, Description, Group, Alias, Arguments, options);
    }
}
