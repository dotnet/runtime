// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Text
{
    internal static class TemplateHelper
    {
        /// <summary>
        /// Parses the message template and throws exception for malformed messages.
        /// </summary>
        internal static string Parse(this string msg, List<string>? templates)
        {
            ReadOnlySpan<char> format = msg.AsSpan();
            var builder = new StringBuilder(msg.Length);
            var pos = 0;
            var len = format.Length;
            var ch = '\0';
            var segments = new List<Segment>();
            var numArgs = 0;

            while (true)
            {
                var segStart = builder.Length;
                while (pos < len)
                {
                    ch = format[pos];

                    pos++;
                    if (ch == '}')
                    {
                        if (pos < len && format[pos] == '}')
                        {
                            // double }, treat as escape sequence
                            pos++;
                        }
                        else
                        {
                            // dangling }, fail
                            throw new ArgumentException($"Dangling }} in format string at position {pos}", nameof(format));
                        }
                    }
                    else if (ch == '{')
                    {
                        if (pos < len && format[pos] == '{')
                        {
                            // double {, treat as escape sequence
                            pos++;
                        }
                        else
                        {
                            // start of a format specification
                            pos--;
                            break;
                        }
                    }

                    builder.Append(ch);
                }

                if (pos == len)
                {
                    var totalLit = builder.Length - segStart;
                    while (totalLit > 0)
                    {
                        var num = totalLit;
                        if (num > short.MaxValue)
                        {
                            num = short.MaxValue;
                        }

                        segments.Add(new Segment((short)num, -1, 0, string.Empty));
                        totalLit -= num;
                    }

                    return builder.ToString();
                }

                // extract the argument index
                var argIndex = 0;
                if (templates == null)
                {
                    // classic composite format string

                    pos++;
                    if (pos == len || (ch = format[pos]) < '0' || ch > '9')
                    {
                        // we need an argument index
                        throw new ArgumentException($"Missing argument index in format string at position {pos}", nameof(format));
                    }

                    do
                    {
                        argIndex = (argIndex * 10) + (ch - '0');
                        pos++;

                        // make sure we get a suitable end to the argument index
                        if (pos == len)
                        {
                            throw new ArgumentException($"Invalid argument index in format string at position {pos}", nameof(format));
                        }

                        ch = format[pos];
                    }
                    while (ch >= '0' && ch <= '9');
                }
                else
                {
                    // template-based format string

                    pos++;
                    if (pos == len)
                    {
                        // we need a template name
                        throw new ArgumentException($"Missing template name in format string at position {pos}", nameof(format));
                    }

                    ch = format[pos];
                    if (!ValidTemplateNameChar(ch, true))
                    {
                        // we need a template name
                        throw new ArgumentException($"Missing template name in format string at position {pos}", nameof(format));
                    }

                    // extract the template name
                    var start = pos;
                    do
                    {
                        pos++;

                        // make sure we get a suitable end
                        if (pos == len)
                        {
                            throw new ArgumentException($"Invalid template name in format string at position {pos}", nameof(format));
                        }

                        ch = format[pos];
                    }
                    while (ValidTemplateNameChar(ch, false));

                    // get an argument index for the given template
                    var template = format.Slice(start, pos - start).ToString();
                    argIndex = templates.IndexOf(template);
                    if (argIndex < 0)
                    {
                        templates.Add(template);
                        argIndex = numArgs;
                    }
                }

                if (argIndex >= numArgs)
                {
                    // new max arg count
                    numArgs = argIndex + 1;
                }

                // skip whitespace
                while (pos < len && (ch = format[pos]) == ' ')
                {
                    pos++;
                }

                // parse the optional field width
                var leftAligned = false;
                var argWidth = 0;
                if (ch == ',')
                {
                    pos++;

                    // skip whitespace
                    while (pos < len && format[pos] == ' ')
                    {
                        pos++;
                    }

                    // did we run out of steam
                    if (pos == len)
                    {
                        throw new ArgumentException($"Invalid field width for argument {numArgs + 1} in format string", nameof(format));
                    }

                    ch = format[pos];
                    if (ch == '-')
                    {
                        leftAligned = true;
                        pos++;

                        // did we run out of steam?
                        if (pos == len)
                        {
                            throw new ArgumentException($"Invalid field width for argument {numArgs + 1} in format string", nameof(format));
                        }

                        ch = format[pos];
                    }

                    if (ch < '0' || ch > '9')
                    {
                        throw new ArgumentException($"Invalid character in field width for argument {numArgs + 1} in format string", nameof(format));
                    }

                    var val = 0;
                    do
                    {
                        val = (val * 10) + (ch - '0');
                        pos++;

                        // did we run out of steam?
                        if (pos == len)
                        {
                            throw new ArgumentException($"Incomplete field width at position {pos}", nameof(format));
                        }

                        // did we get a number that's too big?
                        if (val > short.MaxValue)
                        {
                            throw new ArgumentException($"Field width value exceeds limit for argument {numArgs + 1} in format string", nameof(format));
                        }

                        ch = format[pos];
                    }
                    while (ch >= '0' && ch <= '9');

                    argWidth = val;
                }

                if (leftAligned)
                {
                    argWidth = -argWidth;
                }

                // skip whitespace
                while (pos < len && (ch = format[pos]) == ' ')
                {
                    pos++;
                }

                // parse the optional argument format string

                var argFormat = string.Empty;
                if (ch == ':')
                {
                    pos++;
                    var argFormatStart = pos;

                    while (true)
                    {
                        if (pos == len)
                        {
                            throw new ArgumentException($"Unterminated format specification at position {pos}", nameof(format));
                        }

                        ch = format[pos];
                        pos++;
                        if (ch == '{')
                        {
                            throw new ArgumentException($"Nested {{ in format specification at position {pos}", nameof(format));
                        }
                        else if (ch == '}')
                        {
                            // end of format specification
                            pos--;
                            break;
                        }
                    }

                    if (pos != argFormatStart)
                    {
                        argFormat = format.Slice(argFormatStart, pos - argFormatStart).ToString();
                    }
                }

                if (ch != '}')
                {
                    throw new ArgumentException("Unterminated format specification", nameof(format));
                }

                // skip over the closing brace
                pos++;

                if (numArgs >= short.MaxValue)
                {
                    throw new ArgumentException("Must have less than 32768 arguments", nameof(format));
                }

                var total = builder.Length - segStart;
                while (total > short.MaxValue)
                {
                    segments.Add(new Segment(short.MaxValue, -1, 0, string.Empty));
                    total -= short.MaxValue;
                }

                segments.Add(new Segment((short)total, (short)argIndex, (short)argWidth, argFormat));
            }
        }

        private static bool ValidTemplateNameChar(char ch, bool first)
        {
            if (first)
            {
                return char.IsLetter(ch) || ch == '_';
            }

            return char.IsLetterOrDigit(ch) || ch == '_';
        }

        /// <summary>
        /// A chunk of formatting information.
        /// </summary>
        private readonly struct Segment
        {
            public Segment(short literalCount, short argIndex, short argWidth, string argFormat)
            {
                LiteralCount = literalCount;
                ArgIndex = argIndex;
                ArgWidth = argWidth;
                ArgFormat = argFormat;
            }

            /// <summary>
            /// Gets the number of chars of literal text consumed by this segment.
            /// </summary>
            public short LiteralCount { get; }

            /// <summary>
            /// Gets the index of the argument to be formatted, -1 to skip argument formatting.
            /// </summary>
            public short ArgIndex { get; }

            /// <summary>
            /// Gets the width of the formatted value in characters. If this is negative, it indicates to left-justify
            /// and the field width is then the absolute value.
            /// </summary>
            public short ArgWidth { get; }

            /// <summary>
            /// Gets the custom format string to use when formatting the argument.
            /// </summary>
            public string ArgFormat { get; }
        }
    }
}
