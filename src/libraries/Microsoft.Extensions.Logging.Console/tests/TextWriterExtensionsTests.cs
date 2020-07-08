// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging
{
    public class TextWriterExtensionsTests
    {
        [Fact]
        public void WriteColoredMessage_WithForegroundEscapeCode_AndNoBackgroundColorSpecified()
        {
            // Arrange
            var message = "Request received";
            var expectedMessage = GetForegroundColorEscapeCode(ConsoleColor.DarkGreen)
                + message
                + "\x1B[39m\x1B[22m"; //resets foreground color
            var textWriter = new StringWriter();

            // Act
            textWriter.WriteColoredMessage(message, background: null, foreground: ConsoleColor.DarkGreen, disableColors: false);

            // Assert
            Assert.Equal(expectedMessage, textWriter.ToString());
        }

        [Fact]
        public void WriteColoredMessage_WithBackgroundEscapeCode_AndNoForegroundColorSpecified()
        {
            // Arrange
            var message = "Request received";
            var expectedMessage = GetBackgroundColorEscapeCode(ConsoleColor.Red)
                + message
                + "\x1B[49m"; //resets background color
            var textWriter = new StringWriter();

            // Act
            textWriter.WriteColoredMessage(message, background: ConsoleColor.Red, foreground: null, disableColors: false);

            // Assert
            Assert.Equal(expectedMessage, textWriter.ToString());
        }

        [Fact]
        public void WriteColoredMessage_InOrder_WhenBothForegroundOrBackgroundColorsSpecified()
        {
            // Arrange
            var message = "Request received";
            var expectedMessage = GetBackgroundColorEscapeCode(ConsoleColor.Red)
                + GetForegroundColorEscapeCode(ConsoleColor.DarkGreen)
                + "Request received"
                + "\x1B[39m\x1B[22m" //resets foreground color
                + "\x1B[49m"; //resets background color
            var textWriter = new StringWriter();

            // Act
            textWriter.WriteColoredMessage(message, ConsoleColor.Red, ConsoleColor.DarkGreen, disableColors: false);

            // Assert
            Assert.Equal(expectedMessage, textWriter.ToString());
        }

        private static string GetForegroundColorEscapeCode(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.DarkGreen => "\x1B[32m",
                ConsoleColor.DarkYellow => "\x1B[33m",
                ConsoleColor.Gray => "\x1B[37m",
                _ => "\x1B[39m\x1B[22m" // default foreground color
            };
        }

        private static string GetBackgroundColorEscapeCode(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Red => "\x1B[41m",
                _ => "\x1B[49m"
            };
        }
    }
}