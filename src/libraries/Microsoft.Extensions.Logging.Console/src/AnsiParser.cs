// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;

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
            int startIndex = -1;
            int length = 0;
            int escapeCode;
            ConsoleColor? foreground = null;
            ConsoleColor? background = null;
            var span = message.AsSpan();
            const char EscapeChar = '\x1B';
            ConsoleColor? color = null;
            bool isBright = false;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] == EscapeChar && span.Length >= i + 4 && span[i + 1] == '[')
                {
                    if (span[i + 3] == 'm')
                    {
                        // Example: \x1B[1m
                        if (IsDigit(span[i + 2]))
                        {
                            escapeCode = (int)(span[i + 2] - '0');
                            if (startIndex != -1)
                            {
                                _onParseWrite(message, startIndex, length, background, foreground);
                                startIndex = -1;
                                length = 0;
                            }
                            if (escapeCode == 1)
                                isBright = true;
                            i += 3;
                            continue;
                        }
                    }
                    else if (span.Length >= i + 5 && span[i + 4] == 'm')
                    {
                        // Example: \x1B[40m
                        if (IsDigit(span[i + 2]) && IsDigit(span[i + 3]))
                        {
                            escapeCode = (int)(span[i + 2] - '0') * 10 + (int)(span[i + 3] - '0');
                            if (startIndex != -1)
                            {
                                _onParseWrite(message, startIndex, length, background, foreground);
                                startIndex = -1;
                                length = 0;
                            }
                            if (TryGetForegroundColor(escapeCode, isBright, out color))
                            {
                                foreground = color;
                                isBright = false;
                            }
                            else if (TryGetBackgroundColor(escapeCode, out color))
                            {
                                background = color;
                            }
                            i += 4;
                            continue;
                        }
                    }
                }
                if (startIndex == -1)
                {
                    startIndex = i;
                }
                int nextEscapeIndex = -1;
                if (i < message.Length - 1)
                {
                    nextEscapeIndex = message.IndexOf(EscapeChar, i + 1);
                }
                if (nextEscapeIndex < 0)
                {
                    length = message.Length - startIndex;
                    break;
                }
                length = nextEscapeIndex - startIndex;
                i = nextEscapeIndex - 1;
            }
            if (startIndex != -1)
            {
                _onParseWrite(message, startIndex, length, background, foreground);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDigit(char c) => (uint)(c - '0') <= ('9' - '0');

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

        private static bool TryGetForegroundColor(int number, bool isBright, out ConsoleColor? color)
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

        private static bool TryGetBackgroundColor(int number, out ConsoleColor? color)
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
