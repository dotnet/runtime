﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed class SourceWriter
    {
        private readonly StringBuilder _sb = new();
        private static readonly char[] s_newLine = Environment.NewLine.ToCharArray();
        private int _indentation;

        public void WriteBlockStart(string? declaration = null)
        {
            if (declaration is not null)
            {
                WriteLine(declaration);
            }
            WriteLine("{");
            _indentation++;
        }

        public void WriteBlockEnd(string? extra = null)
        {
            _indentation--;
            Debug.Assert(_indentation > -1);
            WriteLine($"}}{extra}");
        }

        public void WriteLine(string source)
        {
            _sb.Append(' ', 4 * _indentation);
            _sb.AppendLine(source);
        }

        public void WriteBlock(string source)
        {
            bool isFinalLine;
            ReadOnlySpan<char> remainingText = source.AsSpan();

            do
            {
                ReadOnlySpan<char> line = GetNextLine(ref remainingText, out isFinalLine);
                switch (line)
                {
                    case "{":
                        {
                            WriteBlockStart();
                        }
                        break;
                    case "}":
                        {
                            WriteBlockEnd();
                        }
                        break;
                    default:
                        {
                            WriteLine(line);
                        }
                        break;
                }
            } while (!isFinalLine);
        }

        public void WriteBlankLine() => _sb.AppendLine();

        public SourceText ToSourceText()
        {
            Debug.Assert(_indentation == 0 && _sb.Length > 0);
            return SourceText.From(_sb.ToString(), Encoding.UTF8);
        }

        private static ReadOnlySpan<char> GetNextLine(ref ReadOnlySpan<char> remainingText, out bool isFinalLine)
        {
            if (remainingText.IsEmpty)
            {
                isFinalLine = true;
                return default;
            }

            ReadOnlySpan<char> next;
            ReadOnlySpan<char> rest;

            remainingText = remainingText.Trim();

            int lineLength = remainingText.IndexOf(s_newLine);
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

            next = remainingText.Slice(0, lineLength);
            remainingText = rest;
            return next;
        }

        private unsafe void WriteLine(ReadOnlySpan<char> source)
        {
            _sb.Append(' ', 4 * _indentation);
            fixed (char* ptr = source)
            {
                _sb.Append(ptr, source.Length);
                WriteBlankLine();
            }
        }
    }
}
