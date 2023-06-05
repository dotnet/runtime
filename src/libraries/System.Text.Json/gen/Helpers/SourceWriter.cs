// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;

namespace System.Text.Json.SourceGeneration
{
    internal sealed class SourceWriter
    {
        private readonly StringBuilder _sb = new();
        private int _indentation;

        public SourceWriter()
        {
            IndentationChar = ' ';
            CharsPerIndentation = 4;
        }

        public SourceWriter(char indentationChar, int charsPerIndentation)
        {
            if (!char.IsWhiteSpace(indentationChar))
            {
                throw new ArgumentOutOfRangeException(nameof(indentationChar));
            }

            if (charsPerIndentation < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(charsPerIndentation));
            }

            IndentationChar = indentationChar;
            CharsPerIndentation = charsPerIndentation;
        }

        public char IndentationChar { get; }
        public int CharsPerIndentation { get; }

        public int Length => _sb.Length;
        public int Indentation
        {
            get => _indentation;
            set
            {
                if (value < 0)
                {
                    Throw();
                    static void Throw() => throw new ArgumentOutOfRangeException(nameof(value));
                }

                _indentation = value;
            }
        }

        public void WriteLine(char value)
        {
            AddIndentation();
            _sb.Append(value);
            _sb.AppendLine();
        }

        public void WriteLine(string text)
        {
            if (_indentation == 0)
            {
                _sb.AppendLine(text);
                return;
            }

            bool isFinalLine;
            ReadOnlySpan<char> remainingText = text.AsSpan();
            do
            {
                ReadOnlySpan<char> nextLine = GetNextLine(ref remainingText, out isFinalLine);

                AddIndentation();
                AppendSpan(_sb, nextLine);
                _sb.AppendLine();
            }
            while (!isFinalLine);
        }

        public void WriteLine() => _sb.AppendLine();

        public SourceText ToSourceText()
        {
            Debug.Assert(_indentation == 0 && _sb.Length > 0);
            return SourceText.From(_sb.ToString(), Encoding.UTF8);
        }

        private void AddIndentation()
            => _sb.Append(IndentationChar, CharsPerIndentation * _indentation);

        private static ReadOnlySpan<char> GetNextLine(ref ReadOnlySpan<char> remainingText, out bool isFinalLine)
        {
            if (remainingText.IsEmpty)
            {
                isFinalLine = true;
                return default;
            }

            ReadOnlySpan<char> next;
            ReadOnlySpan<char> rest;

            int lineLength = remainingText.IndexOf('\n');
            if (lineLength == -1)
            {
                lineLength = remainingText.Length;
                isFinalLine = true;
                rest = default;
            }
            else
            {
                rest = remainingText.Slice(lineLength + 1);
                isFinalLine = false;
            }

            if ((uint)lineLength > 0 && remainingText[lineLength - 1] == '\r')
            {
                lineLength--;
            }

            next = remainingText.Slice(0, lineLength);
            remainingText = rest;
            return next;
        }

        private static unsafe void AppendSpan(StringBuilder builder, ReadOnlySpan<char> span)
        {
            fixed (char* ptr = span)
            {
                builder.Append(ptr, span.Length);
            }
        }
    }
}
