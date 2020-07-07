// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.Extensions.Logging.Test.Console
{
    public class TestConsole : IConsole
    {
        public static readonly ConsoleColor? DefaultBackgroundColor;
        public static readonly ConsoleColor? DefaultForegroundColor;

        private ConsoleSink _sink;

        public TestConsole(ConsoleSink sink)
        {
            _sink = sink;
            BackgroundColor = DefaultBackgroundColor;
            ForegroundColor = DefaultForegroundColor;
        }

        public ConsoleColor? BackgroundColor { get; private set; }

        public ConsoleColor? ForegroundColor { get; private set; }

        public void Write(string message)
        {
            // writes content with the embedded foreground and background ansi colored. it skips non-color related ansi codes
            (int startIndex, int length, ConsoleColor? bg, ConsoleColor? fg) content = (-1, 0, null, null);
            var span = message.AsSpan();
            const char EscapeChar = '\x1B';
            ConsoleColor? color = null;
            bool isDarkColor = true;
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i] != EscapeChar || span.Length < i + 3 || span[i + 1] != '[')
                {
                    // not an escape char
                    content.length++;
                    if (content.startIndex == -1)
                    {
                        content.startIndex = i;
                    }
                }
                else if (span[i + 3] == 'm')
                {
                    if (int.TryParse(span.Slice(i + 2, length: 1), out int escapeCode))
                    {
                        if (escapeCode == 1)
                            isDarkColor = false;
                        // parsing only ansi color codes
                        i += 3;
                    }
                }
                else if (span[i + 4] == 'm')
                {
                    if (int.TryParse(span.Slice(i + 2, length: 2), out int escapeCode))
                    {
                        if (SetsForegroundColor(escapeCode, isDarkColor, out color))
                        {
                            if (content.startIndex != -1)
                            {
                                Write(span.Slice(content.startIndex, content.length), content.bg, content.fg);
                                content.startIndex = -1;
                                content.length = 0;
                                isDarkColor = true;
                            }
                            content.fg = color;
                        }
                        else if (SetsBackgroundColor(escapeCode, out color))
                        {
                            // time for new color bg
                            if (content.startIndex != -1)
                            {
                                Write(span.Slice(content.startIndex, content.length), content.bg, content.fg);
                                content.startIndex = -1;
                                content.length = 0;
                            }
                            content.bg = color;
                        }
                        // parsing only ansi color codes
                        i += 4;
                    }
                }
            }
            if (content.startIndex != -1)
            {
                Write(span.Slice(content.startIndex, content.length), content.bg, content.fg);
            }
        }

        public void Write(ReadOnlySpan<char> span, ConsoleColor? background, ConsoleColor? foreground)
        {
            var consoleContext = new ConsoleContext();
            consoleContext.Message = span.ToString();

            if (background.HasValue)
            {
                consoleContext.BackgroundColor = background.Value;
            }

            if (foreground.HasValue)
            {
                consoleContext.ForegroundColor = foreground.Value;
            }

            _sink.Write(consoleContext);

            ResetColor();
        }

        public void Write(string message, ConsoleColor? background, ConsoleColor? foreground)
        {
            var consoleContext = new ConsoleContext();
            consoleContext.Message = message;

            if (background.HasValue)
            {
                consoleContext.BackgroundColor = background.Value;
            }

            if (foreground.HasValue)
            {
                consoleContext.ForegroundColor = foreground.Value;
            }

            _sink.Write(consoleContext);

            ResetColor();
        }

        public void WriteLine(string message, ConsoleColor? background, ConsoleColor? foreground)
        {
            Write(message + Environment.NewLine, background, foreground);
        }

        public void Flush()
        {
        }

        private void ResetColor()
        {
            BackgroundColor = DefaultBackgroundColor;
            ForegroundColor = DefaultForegroundColor;
        }

        private static bool SetsForegroundColor(int number, bool isDark, out ConsoleColor? color)
        {
            switch (number)
            {
                case 30:
                    color = ConsoleColor.Black;
                    return true;
                case 31:
                    color = isDark ? ConsoleColor.DarkRed : ConsoleColor.Red;
                    return true;
                case 32:
                    color = isDark ? ConsoleColor.DarkGreen : ConsoleColor.Green;
                    return true;
                case 33:
                    color = isDark ? ConsoleColor.DarkYellow : ConsoleColor.Yellow;
                    return true;
                case 34:
                    color = isDark ? ConsoleColor.DarkBlue : ConsoleColor.Blue;
                    return true;
                case 35:
                    color = isDark ? ConsoleColor.DarkMagenta : ConsoleColor.Magenta;
                    return true;
                case 36:
                    color = isDark ? ConsoleColor.DarkCyan : ConsoleColor.Cyan;
                    return true;
                case 37:
                    color = isDark ? ConsoleColor.Gray : ConsoleColor.White;
                    return true;
                case 39:
                    color = null;
                    return true;
            }
            color = null;
            return false;
        }

        private static bool SetsBackgroundColor(int number, out ConsoleColor? color)
        {
            switch (number)
            {
                case 40:
                    color = ConsoleColor.Black;
                    return true;
                case 41:
                    color = ConsoleColor.Red;
                    return true;
                case 42:
                    color = ConsoleColor.Green;
                    return true;
                case 43:
                    color = ConsoleColor.Yellow;
                    return true;
                case 44:
                    color = ConsoleColor.Blue;
                    return true;
                case 45:
                    color = ConsoleColor.Magenta;
                    return true;
                case 46:
                    color = ConsoleColor.Cyan;
                    return true;
                case 47:
                    color = ConsoleColor.White;
                    return true;
                case 49:
                    color = null;
                    return true;
            }
            color = null;
            return false;
        }
    }
}
