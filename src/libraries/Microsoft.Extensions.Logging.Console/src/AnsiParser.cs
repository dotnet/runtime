// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.Logging.Console
{
    internal class AnsiParser
    {
        private readonly Action<string, int, int, ConsoleColor?, ConsoleColor?> _onParseWrite;
        public AnsiParser(Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite)
        {
            if (onParseWrite == null)
            {
                throw new ArgumentNullException(nameof(onParseWrite));
            }
            _onParseWrite = onParseWrite;
        }

        /// <summary>
        /// Parses a subset of display attributes
        /// Set Display Attributes
        /// Set Attribute Mode [{attr1};...;{attrn}m
        /// Sets multiple display attribute settings. The following lists standard attributes that are getting parsed:
        /// 1 Bright
        /// Foreground Colours
        /// 30 Black
        /// 31 Red
        /// 32 Green
        /// 33 Yellow
        /// 34 Blue
        /// 35 Magenta
        /// 36 Cyan
        /// 37 White
        /// Background Colours
        /// 40 Black
        /// 41 Red
        /// 42 Green
        /// 43 Yellow
        /// 44 Blue
        /// 45 Magenta
        /// 46 Cyan
        /// 47 White
        /// </summary>
        public void Parse(string message)
        {
            (int startIndex, int length, ConsoleColor? bg, ConsoleColor? fg) content = (-1, 0, null, null);
            var span = message.AsSpan();
            const char EscapeChar = '\x1B';
            ConsoleColor? color = null;
            bool isBright = false;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] != EscapeChar || span.Length < i + 4 || span[i + 1] != '[')
                {
                    if (content.startIndex == -1)
                    {
                        content.startIndex = i;
                    }
                    int nextEscapeIndex = span.Slice(i, span.Length - i).IndexOf(EscapeChar);
                    if (nextEscapeIndex == -1)
                    {
                        content.length += span.Length - i;
                        break;
                    }
                    content.length += nextEscapeIndex;
                    i += nextEscapeIndex - 1;
                }
                else if (span[i + 3] == 'm')
                {
#if NETSTANDARD2_0
                    if (ushort.TryParse(span.Slice(i + 2, length: 1).ToString(), out ushort escapeCode))
#else
                    if (ushort.TryParse(span.Slice(i + 2, length: 1), out ushort escapeCode))
#endif
                    {
                        if (escapeCode == 1)
                            isBright = true;
                        i += 3;
                    }
                }
                else if (span.Length >= i + 5 && span[i + 4] == 'm')
                {
#if NETSTANDARD2_0
                    if (ushort.TryParse(span.Slice(i + 2, length: 2).ToString(), out ushort escapeCode))
#else
                    if (ushort.TryParse(span.Slice(i + 2, length: 2), out ushort escapeCode))
#endif
                    {
                        if (SetsForegroundColor(escapeCode, isBright, out color))
                        {
                            if (content.startIndex != -1)
                            {
                                _onParseWrite(message, content.startIndex, content.length, content.bg, content.fg);
                                content.startIndex = -1;
                                content.length = 0;
                            }
                            content.fg = color;
                            isBright = false;
                        }
                        else if (SetsBackgroundColor(escapeCode, out color))
                        {
                            if (content.startIndex != -1)
                            {
                                _onParseWrite(message, content.startIndex, content.length, content.bg, content.fg);
                                content.startIndex = -1;
                                content.length = 0;
                            }
                            content.bg = color;
                        }
                        i += 4;
                    }
                }
            }
            if (content.startIndex != -1)
            {
                _onParseWrite(message, content.startIndex, content.length, content.bg, content.fg);
            }
        }

        internal const string DefaultForegroundColor = "\x1B[39m\x1B[22m"; // reset to default foreground color
        internal const string DefaultBackgroundColor = "\x1B[49m"; // reset to the background color

        internal static string GetForegroundColorEscapeCode(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Black => "\x1B[30m",
                ConsoleColor.DarkRed => "\x1B[31m",
                ConsoleColor.DarkGreen => "\x1B[32m",
                ConsoleColor.DarkYellow => "\x1B[33m",
                ConsoleColor.DarkBlue => "\x1B[34m",
                ConsoleColor.DarkMagenta => "\x1B[35m",
                ConsoleColor.DarkCyan => "\x1B[36m",
                ConsoleColor.Gray => "\x1B[37m",
                ConsoleColor.Red => "\x1B[1m\x1B[31m",
                ConsoleColor.Green => "\x1B[1m\x1B[32m",
                ConsoleColor.Yellow => "\x1B[1m\x1B[33m",
                ConsoleColor.Blue => "\x1B[1m\x1B[34m",
                ConsoleColor.Magenta => "\x1B[1m\x1B[35m",
                ConsoleColor.Cyan => "\x1B[1m\x1B[36m",
                ConsoleColor.White => "\x1B[1m\x1B[37m",
                _ => DefaultForegroundColor // default foreground color
            };
        }

        internal static string GetBackgroundColorEscapeCode(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Black => "\x1B[40m",
                ConsoleColor.DarkRed => "\x1B[41m",
                ConsoleColor.DarkGreen => "\x1B[42m",
                ConsoleColor.DarkYellow => "\x1B[43m",
                ConsoleColor.DarkBlue => "\x1B[44m",
                ConsoleColor.DarkMagenta => "\x1B[45m",
                ConsoleColor.DarkCyan => "\x1B[46m",
                ConsoleColor.Gray => "\x1B[47m",
                _ => DefaultBackgroundColor // Use default background color
            };
        }

        private static bool SetsForegroundColor(int number, bool isBright, out ConsoleColor? color)
        {
            color = number switch
            {
                30 => ConsoleColor.Black,
                31 => isBright ? ConsoleColor.Red: ConsoleColor.DarkRed,
                32 => isBright ? ConsoleColor.Green: ConsoleColor.DarkGreen,
                33 => isBright ? ConsoleColor.Yellow: ConsoleColor.DarkYellow,
                34 => isBright ? ConsoleColor.Blue: ConsoleColor.DarkBlue,
                35 => isBright ? ConsoleColor.Magenta: ConsoleColor.DarkMagenta,
                36 => isBright ? ConsoleColor.Cyan: ConsoleColor.DarkCyan,
                37 => isBright ? ConsoleColor.White: ConsoleColor.Gray,
                _ => null
            };
            return color != null || number == 39;
        }

        private static bool SetsBackgroundColor(int number, out ConsoleColor? color)
        {
            color = number switch
            {
                40 => ConsoleColor.Black,
                41 => ConsoleColor.DarkRed,
                42 => ConsoleColor.DarkGreen,
                43 => ConsoleColor.DarkYellow,
                44 => ConsoleColor.DarkBlue,
                45 => ConsoleColor.DarkMagenta,
                46 => ConsoleColor.DarkCyan,
                47 => ConsoleColor.Gray,
                _ => null
            };
            return color != null || number == 49;
        }
    }
}
