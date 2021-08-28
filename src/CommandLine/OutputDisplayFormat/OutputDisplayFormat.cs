﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Orang.CommandLine
{
    internal class OutputDisplayFormat
    {
        public OutputDisplayFormat(
            ContentDisplayStyle contentDisplayStyle,
            PathDisplayStyle pathDisplayStyle = PathDisplayStyle.Full,
            LineDisplayOptions lineOptions = LineDisplayOptions.None,
            LineContext lineContext = default,
            DisplayParts displayParts = DisplayParts.None,
            IEnumerable<FileProperty>? fileProperties = null,
            string? indent = null,
            string? separator = null,
            bool alignColumns = true)
        {
            ContentDisplayStyle = contentDisplayStyle;
            PathDisplayStyle = pathDisplayStyle;
            LineOptions = lineOptions;
            LineContext = lineContext;
            DisplayParts = displayParts;
            FileProperties = fileProperties?.ToImmutableArray() ?? ImmutableArray<FileProperty>.Empty;
            Indent = indent ?? ApplicationOptions.Default.ContentIndent;
            Separator = separator;
            AlignColumns = alignColumns;
        }

        public ContentDisplayStyle ContentDisplayStyle { get; }

        public PathDisplayStyle PathDisplayStyle { get; }

        public LineDisplayOptions LineOptions { get; }

        public LineContext LineContext { get; }

        public DisplayParts DisplayParts { get; }

        public ImmutableArray<FileProperty> FileProperties { get; }

        public string Indent { get; }

        public string? Separator { get; }

        public bool AlignColumns { get; }

        public bool Includes(LineDisplayOptions options) => (LineOptions & options) == options;

        public bool Includes(DisplayParts parts) => (DisplayParts & parts) == parts;
    }
}
