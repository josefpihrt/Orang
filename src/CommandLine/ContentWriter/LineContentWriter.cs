﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Orang.CommandLine
{
    internal class LineContentWriter : ContentWriter
    {
        private int _lastEndIndex;
        protected int _solIndex;
        protected int _eolIndex;
        protected int _lineNumber;
        private ValueWriter? _valueWriter;

        public LineContentWriter(
            string input,
            ContentWriterOptions options,
            IResultStorage? storage = null) : this(input, ContentTextWriter.Default, options, storage)
        {
        }

        public LineContentWriter(
            string input,
            ContentTextWriter? writer,
            ContentWriterOptions options,
            IResultStorage? storage = null) : base(input, writer, options)
        {
            ResultStorage = storage;
            MatchingLineCount = 0;
        }

        public IResultStorage? ResultStorage { get; }

        protected override ValueWriter ValueWriter
        {
            get
            {
                return _valueWriter ??= (Options.IncludeLineNumber)
                    ? new LineNumberValueWriter(Writer, Options.Indent, includeEndingIndent: false)
                    : new ValueWriter(Writer, Options.Indent, includeEndingIndent: false);
            }
        }

        protected override void WriteStartMatches()
        {
            _lastEndIndex = -1;
            _solIndex = 0;
            _eolIndex = -1;
            _lineNumber = 1;
        }

        protected override void WriteStartMatch(ICapture capture)
        {
            int index = capture.Index;

            int prevLineNumber = _lineNumber;

            if (Options.IncludeLineNumber)
            {
                _lineNumber += TextHelpers.CountLines(Input, _solIndex, index - _solIndex);
                ((LineNumberValueWriter)ValueWriter).LineNumber = _lineNumber;
            }

            bool isSameBlock = index < _eolIndex;

            int solIndex = FindStartOfLine(index);

            if (isSameBlock)
            {
                int endIndex = index;

                if (endIndex > _lastEndIndex)
                {
                    if (Input[endIndex - 1] == '\r')
                        endIndex--;

                    Write(Input, _lastEndIndex, endIndex - _lastEndIndex);
                }
            }
            else
            {
                int startIndex = 0;

                if (_lastEndIndex >= 0)
                {
                    WriteEndLine();
                    Write(Options.Separator);

                    if (Options.ContextAfter > 0)
                    {
                        startIndex = WriteContextAfter(_eolIndex, solIndex, prevLineNumber);
                    }
                    else
                    {
                        startIndex = _eolIndex;
                    }
                }

                if (Options.ContextBefore > 0)
                    WriteContextBefore(startIndex, solIndex, _lineNumber);

                Write(Options.Indent);

                if (Options.IncludeLineNumber)
                    WriteLineNumber(_lineNumber);

                MatchingLineCount++;
            }

            int length = capture.Length;

            if (length > 0)
            {
                if (Input[index + length - 1] == '\n')
                    length--;

                MatchingLineCount += TextHelpers.CountLines(Input, index, length);
            }

            _solIndex = solIndex;
            _eolIndex = FindEndOfLine(capture);

            if (!isSameBlock)
            {
                WriteStartLine(_solIndex, index);

                if (ResultStorage != null)
                {
                    int endIndex = _eolIndex;

                    if (length > 0
                        && endIndex > 0
                        && Input[endIndex - 1] == '\n')
                    {
                        endIndex--;

                        if (endIndex > 0
                            && Input[endIndex - 1] == '\r')
                        {
                            endIndex--;
                        }
                    }

                    ResultStorage.Add(Input, _solIndex, endIndex - _solIndex);
                }
            }
        }

        protected override void WriteEndMatch(ICapture capture)
        {
            _lastEndIndex = capture.Index + capture.Length;
        }

        protected override void WriteMatchSeparator()
        {
        }

        protected override void WriteStartReplacement(ICapture capture, string? result)
        {
        }

        protected override void WriteEndReplacement(ICapture capture, string? result)
        {
        }

        protected override void WriteEndMatches()
        {
            WriteEndLine();

            if (Options.ContextAfter > 0)
                WriteContextAfter(_eolIndex, Input.Length, _lineNumber);
        }

        public override void Dispose()
        {
        }

        private void WriteEndLine()
        {
            WriteEndLine(_lastEndIndex, _eolIndex);
        }
    }
}
