﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using Orang;
using Orang.FileSystem;
using Orang.FileSystem.Fluent;

namespace N;

public static class Program
{
    public static void Main()
    {
        IOperationResult result = new SearchBuilder()
            .DirectoryName(Pattern.Any("bin", "obj", PatternOptions.Equals))
            .SkipDirectory(Pattern.Any(".git", ".vs", PatternOptions.Equals))
            .WithDelete("<DIRECTORY_PATH>")
            .ContentOnly()
            .DryRun()
            .LogOperation(o => Console.WriteLine(o.Path))
            .Run(CancellationToken.None);

        Console.WriteLine(result.Telemetry.DirectoryCount);
        Console.WriteLine(result.Telemetry.MatchingDirectoryCount);

        var search = new Search(
            new DirectoryMatcher()
            {
                Name = new Matcher(Pattern.Any(new[] { "bin", "obj" }, PatternOptions.Equals)),
            },
            new SearchOptions()
            {
                SearchDirectory = new DirectoryMatcher()
                {
                    Name = new Matcher(Pattern.Any(new[] { ".git", ".vs" }, PatternOptions.Equals), invert: true)
                }
            });

        result = search.Delete(
            "<DIRECTORY_PATH>",
            new DeleteOptions()
            {
                ContentOnly = true,
                DryRun = true,
                LogOperation = o => Console.WriteLine(o.Path),
            },
            CancellationToken.None);

        Console.WriteLine(result.Telemetry.DirectoryCount);
        Console.WriteLine(result.Telemetry.MatchingDirectoryCount);
    }
}
