// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Test.Console;
using Xunit;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class AnsiParserTests
    {
        private const char EscapeChar = '\e';

        [Theory]
        [InlineData(1, "No Color", "No Color")]
        [InlineData(2, "\e[41mColored\e[49mNo Color", "No Color")]
        [InlineData(2, "\e[41m\e[1m\e[31mmColored\e[39m\e[49mNo Color", "No Color")]
        public void Parse_CheckTimesWrittenToConsole(int numSegments, string message, string lastSegment)
        {
            // Arrange
            var segments = new List<ConsoleContext>();
            Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite = (message, startIndex, length, bg, fg) => {
                segments.Add(new ConsoleContext() {
                    BackgroundColor = bg,
                    ForegroundColor = fg,
                    Message = message.AsSpan(startIndex, length).ToString()
                });
            };
            var parser = new AnsiParser(onParseWrite);

            // Act
            parser.Parse(message);

            // Assert
            Assert.Equal(numSegments, segments.Count);
            Assert.Equal(lastSegment, segments.Last().Message);
        }

        [Theory]
        [MemberData(nameof(Colors))]
        public void Parse_SetBackgroundForegroundAndMessageThenReset_Success(ConsoleColor background, ConsoleColor foreground)
        {
            // Arrange
            var message = AnsiParser.GetBackgroundColorEscapeCode(background)
                + AnsiParser.GetForegroundColorEscapeCode(foreground)
                + "Request received"
                + AnsiParser.DefaultForegroundColor //resets foreground color
                + AnsiParser.DefaultBackgroundColor; //resets background color
            var segments = new List<ConsoleContext>();
            Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite = (message, startIndex, length, bg, fg) => {
                segments.Add(new ConsoleContext() {
                    BackgroundColor = bg,
                    ForegroundColor = fg,
                    Message = message.AsSpan(startIndex, length).ToString()
                });
            };
            var parser = new AnsiParser(onParseWrite);

            // Act
            parser.Parse(message);

            // Assert
            Assert.Equal(1, segments.Count);
            Assert.Equal("Request received", segments[0].Message);
            VerifyForeground(foreground, segments[0]);
            VerifyBackground(background, segments[0]);
        }

        [Fact]
        public void Parse_MessageWithMultipleColors_ParsedIntoMultipleSegments()
        {
            // Arrange
            var message = AnsiParser.GetBackgroundColorEscapeCode(ConsoleColor.DarkRed)
                + AnsiParser.GetForegroundColorEscapeCode(ConsoleColor.Gray)
                + "Message1"
                + AnsiParser.DefaultForegroundColor
                + AnsiParser.DefaultBackgroundColor
                + "NoColor"
                + AnsiParser.GetBackgroundColorEscapeCode(ConsoleColor.DarkGreen)
                + AnsiParser.GetForegroundColorEscapeCode(ConsoleColor.Cyan)
                + "Message2"
                + AnsiParser.GetBackgroundColorEscapeCode(ConsoleColor.DarkBlue)
                + AnsiParser.GetForegroundColorEscapeCode(ConsoleColor.Yellow)
                + "Message3"
                + AnsiParser.DefaultForegroundColor
                + AnsiParser.DefaultBackgroundColor;
            var segments = new List<ConsoleContext>();
            Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite = (message, startIndex, length, bg, fg) => {
                segments.Add(new ConsoleContext() {
                    BackgroundColor = bg,
                    ForegroundColor = fg,
                    Message = message.AsSpan(startIndex, length).ToString()
                });
            };
            var parser = new AnsiParser(onParseWrite);

            // Act
            parser.Parse(message);

            // Assert
            Assert.Equal(4, segments.Count);
            Assert.Equal("NoColor", segments[1].Message);
            Assert.Null(segments[1].ForegroundColor);
            Assert.Null(segments[1].BackgroundColor);

            Assert.Equal("Message1", segments[0].Message);
            Assert.Equal("Message2", segments[2].Message);
            Assert.Equal("Message3", segments[3].Message);
            VerifyBackground(ConsoleColor.DarkRed, segments[0]);
            VerifyBackground(ConsoleColor.DarkGreen, segments[2]);
            VerifyBackground(ConsoleColor.DarkBlue, segments[3]);
            VerifyForeground(ConsoleColor.Gray, segments[0]);
            VerifyForeground(ConsoleColor.Cyan, segments[2]);
            VerifyForeground(ConsoleColor.Yellow, segments[3]);
        }

        [Fact]
        public void Parse_RepeatedColorChange_PicksLastSet()
        {
            // Arrange
            var message = AnsiParser.GetBackgroundColorEscapeCode(ConsoleColor.DarkRed)
                + AnsiParser.GetBackgroundColorEscapeCode(ConsoleColor.DarkGreen)
                + AnsiParser.GetBackgroundColorEscapeCode(ConsoleColor.DarkBlue)
                + AnsiParser.GetForegroundColorEscapeCode(ConsoleColor.Gray)
                + AnsiParser.GetForegroundColorEscapeCode(ConsoleColor.Cyan)
                + AnsiParser.GetForegroundColorEscapeCode(ConsoleColor.Yellow)
                + "Request received"
                + AnsiParser.DefaultForegroundColor //resets foreground color
                + AnsiParser.DefaultBackgroundColor; //resets background color
            var segments = new List<ConsoleContext>();
            Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite = (message, startIndex, length, bg, fg) => {
                segments.Add(new ConsoleContext() {
                    BackgroundColor = bg,
                    ForegroundColor = fg,
                    Message = message.AsSpan(startIndex, length).ToString()
                });
            };
            var parser = new AnsiParser(onParseWrite);

            // Act
            parser.Parse(message);

            // Assert
            Assert.Equal(1, segments.Count);
            Assert.Equal("Request received", segments[0].Message);
            VerifyBackground(ConsoleColor.DarkBlue, segments[0]);
            VerifyForeground(ConsoleColor.Yellow, segments[0]);
        }

        [Theory]
        // supported
        [InlineData("\e[77mInfo", "Info")]
        [InlineData("\e[77m\e[1m\e[2m\e[0mInfo\e[1m", "Info")]
        [InlineData("\e[7mInfo", "Info")]
        [InlineData("\e[40m\e[1m\e[33mwarn\e[39m\e[22m\e[49m:", "warn", ":")]
        // unsupported: skips
        [InlineData("Info\e[77m:", "Info", ":")]
        [InlineData("Info\e[7m:", "Info", ":")]
        // treats as content
        [InlineData("\e", "\e")]
        [InlineData("\e ", "\e ")]
        [InlineData("\em", "\em")]
        [InlineData("\e m", "\e m")]
        [InlineData("\exym", "\exym")]
        [InlineData("\e[", "\e[")]
        [InlineData("\e[m", "\e[m")]
        [InlineData("\e[ ", "\e[ ")]
        [InlineData("\e[ m", "\e[ m")]
        [InlineData("\e[xym", "\e[xym")]
        [InlineData("\e[7777m", "\e[7777m")]
        [InlineData("\e\e\e", "\e\e\e")]
        [InlineData("Message\e\e\e", "Message\e\e\e")]
        [InlineData("\e\eMessage\e", "\e\eMessage\e")]
        [InlineData("\e\e\eMessage", "\e\e\eMessage")]
        [InlineData("Message\e ", "Message\e ")]
        [InlineData("\emMessage", "\emMessage")]
        [InlineData("\e[77m\e m\e[40m", "\e m")]
        [InlineData("\e mMessage\exym", "\e mMessage\exym")]
        public void Parse_ValidSupportedOrUnsupportedCodesInMessage_MessageParsedSuccessfully(string messageWithUnsupportedCode, params string[] output)
        {
            // Arrange
            var message = messageWithUnsupportedCode;
            var segments = new List<ConsoleContext>();
            Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite = (message, startIndex, length, bg, fg) => {
                segments.Add(new ConsoleContext() {
                    BackgroundColor = bg,
                    ForegroundColor = fg,
                    Message = message.AsSpan(startIndex, length).ToString()
                });
            };
            var parser = new AnsiParser(onParseWrite);

            // Act
            parser.Parse(messageWithUnsupportedCode);

            // Assert
            Assert.Equal(output.Length, segments.Count);
            for (int i = 0; i < output.Length; i++)
                Assert.Equal(output[i], segments[i].Message);
        }

        [Fact]
        public void NullDelegate_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new AnsiParser(null));
        }

        public static TheoryData<ConsoleColor, ConsoleColor> Colors
        {
            get
            {
                var data = new TheoryData<ConsoleColor, ConsoleColor>();
                foreach (ConsoleColor background in Enum.GetValues(typeof(ConsoleColor)))
                {
                    foreach (ConsoleColor foreground in Enum.GetValues(typeof(ConsoleColor)))
                    {
                        data.Add(background, foreground);
                    }
                }
                return data;
            }
        }

        private void VerifyBackground(ConsoleColor background, ConsoleContext segment)
        {
            if (IsBackgroundColorNotSupported(background))
            {
                Assert.Null(segment.BackgroundColor);
            }
            else
            {
                Assert.Equal(background, segment.BackgroundColor);
            }
        }

        private void VerifyForeground(ConsoleColor foreground, ConsoleContext segment)
        {
            if (IsForegroundColorNotSupported(foreground))
            {
                Assert.Null(segment.ForegroundColor);
            }
            else
            {
                Assert.Equal(foreground, segment.ForegroundColor);
            }
        }

        private static bool IsBackgroundColorNotSupported(ConsoleColor color)
        {
            return AnsiParser.GetBackgroundColorEscapeCode(color).Equals(
                AnsiParser.DefaultBackgroundColor, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsForegroundColorNotSupported(ConsoleColor color)
        {
            return AnsiParser.GetForegroundColorEscapeCode(color).Equals(
                AnsiParser.DefaultForegroundColor, StringComparison.OrdinalIgnoreCase);
        }
    }
}
