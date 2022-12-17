﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Orang.FileSystem;

[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct OperationProgress
{
    internal OperationProgress(FileMatch fileMatch, OperationKind kind, Exception? exception = null)
        : this(fileMatch, null, kind, exception)
    {
    }

    internal OperationProgress(FileMatch fileMatch, string? newPath, OperationKind kind, Exception? exception = null)
    {
        FileMatch = fileMatch;
        NewPath = newPath;
        Kind = kind;
        Exception = exception;
    }

    //TODO: Match
    public FileMatch FileMatch { get; }

    public string Path => FileMatch.Path;

    public string? NewPath { get; }

    internal OperationKind Kind { get; }

    public Exception? Exception { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => (FileMatch is not null) ? $"{Kind}  {FileMatch.Path}" : "Uninitialized";
}
