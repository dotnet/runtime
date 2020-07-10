// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Test.Console;
using Microsoft.Extensions.Logging.Console;
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

        [Theory]
        [MemberData(nameof(Colors))]
        public void SetForeground_Parse_Success(ConsoleColor background, ConsoleColor foreground)
        {
            // Arrange
            var message = AnsiParser.GetBackgroundColorEscapeCode(background)
                + AnsiParser.GetForegroundColorEscapeCode(foreground)
                + "Request received"
                + "\x1B[39m\x1B[22m" //resets foreground color
                + "\x1B[49m"; //resets background color
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
            if (IsForegroundColorNotSupported(foreground))
            {
                Assert.Null(segments[0].ForegroundColor);
            }
            else
            {
                Assert.Equal(foreground, segments[0].ForegroundColor);
            }
            if (IsBackgroundColorNotSupported(background))
            {
                Assert.Null(segments[0].BackgroundColor);
            }
            else
            {
                Assert.Equal(background, segments[0].BackgroundColor);
            }
        }

        [Fact]
        public void NullDelegate_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new AnsiParser(null));
        }

        private class TestAnsiSystemConsole : IAnsiSystemConsole
        {
            public string Message { get; private set; }

            public void Write(string message)
            {
                Message = message;
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