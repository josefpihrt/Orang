﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Orang.CommandLine
{
    internal class EmptyContentWriter : ContentWriter
    {
        public EmptyContentWriter(
            ContentWriterOptions options,
            IResultStorage? resultStorage = null) : base("", options)
        {
            ResultStorage = resultStorage;
        }

        protected override ValueWriter ValueWriter => throw new NotSupportedException();

        public IResultStorage? ResultStorage { get; }

        protected override void WriteMatch(ICapture capture)
        {
            ResultStorage?.Add(capture.Value);
        }

        protected override void WriteStartMatches()
        {
        }

        protected override void WriteStartMatch(ICapture capture) => throw new NotSupportedException();

        protected override void WriteEndMatch(ICapture capture) => throw new NotSupportedException();

        protected override void WriteMatchSeparator()
        {
        }

        protected override void WriteEndMatches()
        {
        }

        protected override void WriteStartReplacement(ICapture capture, string? result) => throw new NotSupportedException();

        protected override void WriteEndReplacement(ICapture capture, string? result) => throw new NotSupportedException();

        public override void Dispose()
        {
        }
    }
}
