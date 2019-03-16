// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            console.Write(message, background: null, foreground: null);
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
            console.WriteLine(message, background: null, foreground: null);

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
            var expectedMessage = message + Environment.NewLine;

            // Act
            console.WriteLine(message, background: null, foreground: null);
            console.Flush();
            console.WriteLine(message, background: null, foreground: null);
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
            var expectedMessage = message + Environment.NewLine;

            // Act
            console.WriteLine(message, background: null, foreground: null);
            console.Flush();

            // Assert
            Assert.Equal(expectedMessage, systemConsole.Message);
        }

        [Fact]
        public void WritesMessage_WithForegroundEscapeCode_AndNoBackgroundColorSpecified()
        {
            // Arrange
            var systemConsole = new TestAnsiSystemConsole();
            var console = new AnsiLogConsole(systemConsole);
            var message = "Request received";
            var expectedMessage = GetForegroundColorEscapeCode(ConsoleColor.DarkGreen)
                + message
                + "\x1B[39m\x1B[22m"; //resets foreground color

            // Act
            console.WriteLine(message, background: null, foreground: ConsoleColor.DarkGreen);
            console.Flush();

            // Assert
            Assert.Equal(expectedMessage + Environment.NewLine, systemConsole.Message);
        }

        [Fact]
        public void WritesMessage_WithBackgroundEscapeCode_AndNoForegroundColorSpecified()
        {
            // Arrange
            var systemConsole = new TestAnsiSystemConsole();
            var console = new AnsiLogConsole(systemConsole);
            var message = "Request received";
            var expectedMessage = GetBackgroundColorEscapeCode(ConsoleColor.Red)
                + message
                + "\x1B[49m"; //resets background color

            // Act
            console.WriteLine(message, background: ConsoleColor.Red, foreground: null);
            console.Flush();

            // Assert
            Assert.Equal(expectedMessage + Environment.NewLine, systemConsole.Message);
        }

        [Fact]
        public void WriteMessage_InOrder_WhenBothForegroundOrBackgroundColorsSpecified()
        {
            // Arrange
            var systemConsole = new TestAnsiSystemConsole();
            var console = new AnsiLogConsole(systemConsole);
            var message = "Request received";
            var expectedMessage = GetBackgroundColorEscapeCode(ConsoleColor.Red)
                + GetForegroundColorEscapeCode(ConsoleColor.DarkGreen)
                + "Request received"
                + "\x1B[39m\x1B[22m" //resets foreground color
                + "\x1B[49m" //resets background color
                + Environment.NewLine;

            // Act
            console.WriteLine(message, background: ConsoleColor.Red, foreground: ConsoleColor.DarkGreen);
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
