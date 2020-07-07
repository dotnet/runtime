// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging.Console;
using Xunit;

namespace Microsoft.Extensions.Logging
{
    public class AnsiLogConsoleTest
    {
        [Fact]
        public void DoesNotAddNewLine()
        {
            // Arrange
            var systemConsole = new TestAnsiSystemConsole();
            var console = new AnsiLogConsole(systemConsole);
            var message = "Request received";
            var expectedMessage = message;

            // Act
            console.Write(message);
            console.Flush();

            // Assert
            Assert.Equal(expectedMessage, systemConsole.Message);
        }

        [Fact]
        public void NotCallingFlush_DoesNotWriteData_ToSystemConsole()
        {
            // Arrange
            var systemConsole = new TestAnsiSystemConsole();
            var console = new AnsiLogConsole(systemConsole);
            var message = "Request received";
            var expectedMessage = message + Environment.NewLine;

            // Act
            console.Write(message);

            // Assert
            Assert.Null(systemConsole.Message);
        }

        [Fact]
        public void CallingFlush_ClearsData_FromOutputBuilder()
        {
            // Arrange
            var systemConsole = new TestAnsiSystemConsole();
            var console = new AnsiLogConsole(systemConsole);
            var message = "Request received";
            var expectedMessage = message;

            // Act
            console.Write(message);
            console.Flush();
            console.Write(message);
            console.Flush();

            // Assert
            Assert.Equal(expectedMessage, systemConsole.Message);
        }

        [Fact]
        public void WritesMessage_WithoutEscapeCodes_AndNoForegroundOrBackgroundColorsSpecified()
        {
            // Arrange
            var systemConsole = new TestAnsiSystemConsole();
            var console = new AnsiLogConsole(systemConsole);
            var message = "Request received";
            var expectedMessage = message;

            // Act
            console.Write(message);
            console.Flush();

            // Assert
            Assert.Equal(expectedMessage, systemConsole.Message);
        }

        private class TestAnsiSystemConsole : IAnsiSystemConsole
        {
            public string Message { get; private set; }

            public void Write(string message)
            {
                Message = message;
            }
        }

        private static string GetForegroundColorEscapeCode(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Red:
                    return "\x1B[31m";
                case ConsoleColor.DarkGreen:
                    return "\x1B[32m";
                case ConsoleColor.DarkYellow:
                    return "\x1B[33m";
                case ConsoleColor.Gray:
                    return "\x1B[37m";
                default:
                    return "\x1B[39m";
            }
        }

        private static string GetBackgroundColorEscapeCode(ConsoleColor color)
        {
            switch (color)
            {
                case ConsoleColor.Red:
                    return "\x1B[41m";
                default:
                    return "\x1B[49m";
            }
        }
    }
}
