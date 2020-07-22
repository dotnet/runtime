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
        private AnsiParser _parser;

        public TestConsole(ConsoleSink sink)
        {
            _sink = sink;
            _parser = new AnsiParser(OnParseWrite);
            BackgroundColor = DefaultBackgroundColor;
            ForegroundColor = DefaultForegroundColor;
        }

        public ConsoleColor? BackgroundColor { get; private set; }

        public ConsoleColor? ForegroundColor { get; private set; }

        public void Write(string message)
        {
            _parser.Parse(message);
        }

        public void OnParseWrite(string message, int startIndex, int length, ConsoleColor? background, ConsoleColor? foreground)
        {
            var consoleContext = new ConsoleContext();
            consoleContext.Message = message.AsSpan().Slice(startIndex, length).ToString();

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

        private void ResetColor()
        {
            BackgroundColor = DefaultBackgroundColor;
            ForegroundColor = DefaultForegroundColor;
        }
    }
}
