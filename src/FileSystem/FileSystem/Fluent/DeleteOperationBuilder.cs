﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

#pragma warning disable RCS1223 // Mark publicly visible type with DebuggerDisplay attribute.

namespace Orang.FileSystem.Fluent;

public class DeleteOperationBuilder
{
    private readonly Search _search;
    private readonly string _directoryPath;
    private bool _contentOnly;
    private bool _includingBom;
    private bool _dryRun;
    private Action<OperationProgress>? _logOperation;

    internal DeleteOperationBuilder(Search search, string directoryPath)
    {
        if (search is null)
            throw new ArgumentNullException(nameof(search));

        if (directoryPath is null)
            throw new ArgumentNullException(nameof(directoryPath));

        _search = search;
        _directoryPath = directoryPath;
    }

    public DeleteOperationBuilder ContentOnly()
    {
        _contentOnly = true;

        return this;
    }

    public DeleteOperationBuilder IncludingBom()
    {
        _includingBom = true;

        return this;
    }

    public DeleteOperationBuilder DryRun()
    {
        _dryRun = true;

        return this;
    }

    public DeleteOperationBuilder LogOperation(Action<OperationProgress> logOperation)
    {
        _logOperation = logOperation;

        return this;
    }

    public IOperationResult Run(CancellationToken cancellationToken = default)
    {
        var options = new DeleteOptions()
        {
            ContentOnly = _contentOnly,
            IncludingBom = _includingBom,
            DryRun = _dryRun,
            LogOperation = _logOperation
        };

        return _search.Delete(_directoryPath, options, cancellationToken);
    }
}