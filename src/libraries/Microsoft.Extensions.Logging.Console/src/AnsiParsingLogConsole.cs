// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.Versioning;

namespace Microsoft.Extensions.Logging.Console
{
    [UnsupportedOSPlatform("android")]
    [UnsupportedOSPlatform("browser")]
    [UnsupportedOSPlatform("ios")]
    [UnsupportedOSPlatform("maccatalyst")]
    [UnsupportedOSPlatform("tvos")]
    internal sealed class AnsiParsingLogConsole : IConsole
    {
        private readonly TextWriter _textWriter;
        private readonly AnsiParser _parser;

        public AnsiParsingLogConsole(bool stdErr = false)
        {
            _textWriter = stdErr ? System.Console.Error : System.Console.Out;
            _parser = new AnsiParser(WriteToConsole);
        }

        public void Write(string message)
        {
            _parser.Parse(message);
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

        private void WriteToConsole(string message, int startIndex, int length, ConsoleColor? background, ConsoleColor? foreground)
        {
            ReadOnlySpan<char> span = message.AsSpan(startIndex, length);
            var colorChanged = SetColor(background, foreground);
#if NETCOREAPP
            _textWriter.Write(span);
#else
            _textWriter.Write(span.ToString());
#endif
            if (colorChanged)
            {
                ResetColor();
            }
        }
    }
}
