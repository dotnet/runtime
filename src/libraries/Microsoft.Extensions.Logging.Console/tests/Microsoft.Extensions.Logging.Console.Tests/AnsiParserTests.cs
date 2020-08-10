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
        private const char EscapeChar = '\x1B';

        [Theory]
        [InlineData(1, "No Color", "No Color")]
        [InlineData(2, "\x1B[41mColored\x1B[49mNo Color", "No Color")]
        [InlineData(2, "\x1B[41m\x1B[1m\x1B[31mmColored\x1B[39m\x1B[49mNo Color", "No Color")]
        public void Parse_CheckTimesWrittenToConsole(int numSegments, string message, string lastSegment)
        {
            // Arrange
            var segments = new List<ConsoleContext>();
            Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite = (message, startIndex, length, bg, fg) => {
                segments.Add(new ConsoleContext() {
                    BackgroundColor = bg,
                    ForegroundColor = fg,
                    Message = message.AsSpan().Slice(startIndex, length).ToString()
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
                    Message = message.AsSpan().Slice(startIndex, length).ToString()
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
                    Message = message.AsSpan().Slice(startIndex, length).ToString()
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
                    Message = message.AsSpan().Slice(startIndex, length).ToString()
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
        [InlineData("\x1B[77mInfo", "Info")]
        [InlineData("\x1B[77m\x1B[1m\x1B[2m\x1B[0mInfo\x1B[1m", "Info")]
        [InlineData("\x1B[7mInfo", "Info")]
        [InlineData("\x1B[40m\x1B[1m\x1B[33mwarn\x1B[39m\x1B[22m\x1B[49m:", "warn", ":")]
        // unsupported: skips
        [InlineData("Info\x1B[77m:", "Info", ":")]
        [InlineData("Info\x1B[7m:", "Info", ":")]
        // treats as content
        [InlineData("\x1B", "\x1B")]
        [InlineData("\x1B ", "\x1B ")]
        [InlineData("\x1Bm", "\x1Bm")]
        [InlineData("\x1B m", "\x1B m")]
        [InlineData("\x1Bxym", "\x1Bxym")]
        [InlineData("\x1B[", "\x1B[")]
        [InlineData("\x1B[m", "\x1B[m")]
        [InlineData("\x1B[ ", "\x1B[ ")]
        [InlineData("\x1B[ m", "\x1B[ m")]
        [InlineData("\x1B[xym", "\x1B[xym")]
        [InlineData("\x1B[7777m", "\x1B[7777m")]
        [InlineData("\x1B\x1B\x1B", "\x1B\x1B\x1B")]
        [InlineData("Message\x1B\x1B\x1B", "Message\x1B\x1B\x1B")]
        [InlineData("\x1B\x1BMessage\x1B", "\x1B\x1BMessage\x1B")]
        [InlineData("\x1B\x1B\x1BMessage", "\x1B\x1B\x1BMessage")]
        [InlineData("Message\x1B ", "Message\x1B ")]
        [InlineData("\x1BmMessage", "\x1BmMessage")]
        [InlineData("\x1B[77m\x1B m\x1B[40m", "\x1B m")]
        [InlineData("\x1B mMessage\x1Bxym", "\x1B mMessage\x1Bxym")]
        public void Parse_ValidSupportedOrUnsupportedCodesInMessage_MessageParsedSuccessfully(string messageWithUnsupportedCode, params string[] output)
        {
            // Arrange
            var message = messageWithUnsupportedCode;
            var segments = new List<ConsoleContext>();
            Action<string, int, int, ConsoleColor?, ConsoleColor?> onParseWrite = (message, startIndex, length, bg, fg) => {
                segments.Add(new ConsoleContext() {
                    BackgroundColor = bg,
                    ForegroundColor = fg,
                    Message = message.AsSpan().Slice(startIndex, length).ToString()
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
