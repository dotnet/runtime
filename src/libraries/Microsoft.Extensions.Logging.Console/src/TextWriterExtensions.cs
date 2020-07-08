// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Collections.Concurrent;

namespace Microsoft.Extensions.Logging.Console
{
    internal static class TextWriterExtensions
    {
        public static bool WriteColoredMessage(this TextWriter textWriter, string message, ConsoleColor? background, ConsoleColor? foreground, bool disableColors)
        {
            if (disableColors)
            {
                return false;
            }
            // Order: backgroundcolor, foregroundcolor, Message, reset foregroundcolor, reset backgroundcolor
            if (background.HasValue)
            {
                textWriter.Write(GetBackgroundColorEscapeCode(background.Value));
            }
            if (foreground.HasValue)
            {
                textWriter.Write(GetForegroundColorEscapeCode(foreground.Value));
            }
            textWriter.Write(message);
            if (foreground.HasValue)
            {
                textWriter.Write(DefaultForegroundColor); // reset to default foreground color
            }
            if (background.HasValue)
            {
                textWriter.Write(DefaultBackgroundColor); // reset to the background color
            }
            return background.HasValue || foreground.HasValue;
        }

        private const string DefaultForegroundColor = "\x1B[39m\x1B[22m"; // reset to default foreground color
        private const string DefaultBackgroundColor = "\x1B[49m"; // reset to the background color

        private static string GetForegroundColorEscapeCode(ConsoleColor color)
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

        private static string GetBackgroundColorEscapeCode(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Black => "\x1B[40m",
                ConsoleColor.Red => "\x1B[41m",
                ConsoleColor.Green => "\x1B[42m",
                ConsoleColor.Yellow => "\x1B[43m",
                ConsoleColor.Blue => "\x1B[44m",
                ConsoleColor.Magenta => "\x1B[45m",
                ConsoleColor.Cyan => "\x1B[46m",
                ConsoleColor.Gray => "\x1B[47m",
                _ => DefaultBackgroundColor // Use default background color
            };
        }
    }
}
