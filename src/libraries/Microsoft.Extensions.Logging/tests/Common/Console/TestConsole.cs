// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    }
}
