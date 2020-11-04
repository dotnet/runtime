// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class TextWriterExtensionsTests
    {
        [Fact]
        public void WriteColoredMessage_WithForegroundEscapeCode_AndNoBackgroundColorSpecified()
        {
            // Arrange
            var message = "Request received";
            var expectedMessage = AnsiParser.GetForegroundColorEscapeCode(ConsoleColor.DarkGreen)
                + message
                + "\x1B[39m\x1B[22m"; //resets foreground color
            var textWriter = new StringWriter();

            // Act
            textWriter.WriteColoredMessage(message, background: null, foreground: ConsoleColor.DarkGreen);

            // Assert
            Assert.Equal(expectedMessage, textWriter.ToString());
        }

        [Fact]
        public void WriteColoredMessage_WithBackgroundEscapeCode_AndNoForegroundColorSpecified()
        {
            // Arrange
            var message = "Request received";
            var expectedMessage = AnsiParser.GetBackgroundColorEscapeCode(ConsoleColor.Red)
                + message
                + "\x1B[49m"; //resets background color
            var textWriter = new StringWriter();

            // Act
            textWriter.WriteColoredMessage(message, background: ConsoleColor.Red, foreground: null);

            // Assert
            Assert.Equal(expectedMessage, textWriter.ToString());
        }

        [Fact]
        public void WriteColoredMessage_InOrder_WhenBothForegroundOrBackgroundColorsSpecified()
        {
            // Arrange
            var message = "Request received";
            var expectedMessage = AnsiParser.GetBackgroundColorEscapeCode(ConsoleColor.Red)
                + AnsiParser.GetForegroundColorEscapeCode(ConsoleColor.DarkGreen)
                + "Request received"
                + "\x1B[39m\x1B[22m" //resets foreground color
                + "\x1B[49m"; //resets background color
            var textWriter = new StringWriter();

            // Act
            textWriter.WriteColoredMessage(message, ConsoleColor.Red, ConsoleColor.DarkGreen);

            // Assert
            Assert.Equal(expectedMessage, textWriter.ToString());
        }

        [Fact]
        public void WriteColoredMessage_NullColors_NoAnsiEmbedded()
        {
            // Arrange
            var message = "Request received";
            var textWriter = new StringWriter();

            // Act
            textWriter.WriteColoredMessage(message, null, null);

            // Assert
            Assert.Equal(message, textWriter.ToString());
        }
    }
}
