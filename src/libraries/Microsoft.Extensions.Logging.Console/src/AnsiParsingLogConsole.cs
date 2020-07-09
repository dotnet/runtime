// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.Logging.Console
{
    internal class AnsiParsingLogConsole : IConsole
    {
        private readonly TextWriter _textWriter;

        public AnsiParsingLogConsole(bool stdErr = false)
        {
            _textWriter = stdErr ? System.Console.Error : System.Console.Out;
        }

        private bool SetColor(ConsoleColor? background, ConsoleColor? foreground)
        {
            var backgroundChanged = SetBackgroundColor(background);
            return SetForegroundColor(foreground) || backgroundChanged;
        }

        private bool SetBackgroundColor(ConsoleColor? background)
        {
            if (background.HasValue)
            {
                System.Console.BackgroundColor = background.Value;
                return true;
            }
            return false;
        }

        private bool SetForegroundColor(ConsoleColor? foreground)
        {
            if (foreground.HasValue)
            {
                System.Console.ForegroundColor = foreground.Value;
                return true;
            }
            return false;
        }

        private void ResetColor()
        {
            System.Console.ResetColor();
        }

        public void Write(ReadOnlySpan<char> span, ConsoleColor? background, ConsoleColor? foreground)
        {
            var colorChanged = SetColor(background, foreground);
            _textWriter.Write(span);
            if (colorChanged)
            {
                ResetColor();
            }
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
        public void Write(string message)
        {
            // writes content with the embedded foreground and background ansi colored. it skips non-color related ansi codes
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
                    if (ushort.TryParse(span.Slice(i + 2, length: 1), out ushort escapeCode))
                    {
                        if (escapeCode == 1)
                            isBright = true;
                        i += 3;
                    }
                }
                else if (span.Length >= i + 5 && span[i + 4] == 'm')
                {
                    if (ushort.TryParse(span.Slice(i + 2, length: 2), out ushort escapeCode))
                    {
                        if (SetsForegroundColor(escapeCode, isBright, out color))
                        {
                            if (content.startIndex != -1)
                            {
                                Write(span.Slice(content.startIndex, content.length), content.bg, content.fg);
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
                                Write(span.Slice(content.startIndex, content.length), content.bg, content.fg);
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
                Write(span.Slice(content.startIndex, content.length), content.bg, content.fg);
            }
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
