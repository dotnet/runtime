// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.EventLog;
using Xunit;

namespace Microsoft.Extensions.Logging
{
    public class EventLogLoggerTest
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public static void IsEnabledReturnsCorrectValue()
        {
            // Arrange
            var logger = new EventLogLogger("Test", new EventLogSettings()
            {
                Filter = (s, level) => level >= LogLevel.Warning
            },
            new LoggerExternalScopeProvider());

            // Assert
            Assert.False(logger.IsEnabled(LogLevel.None));
            Assert.True(logger.IsEnabled(LogLevel.Critical));
            Assert.True(logger.IsEnabled(LogLevel.Error));
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.False(logger.IsEnabled(LogLevel.Information));
            Assert.False(logger.IsEnabled(LogLevel.Debug));
            Assert.False(logger.IsEnabled(LogLevel.Trace));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void CallingBeginScopeOnLogger_ReturnsNonNullableInstance()
        {
            // Arrange
            var logger = new EventLogLogger("Test", new EventLogSettings(), new LoggerExternalScopeProvider());

            // Act
            var disposable = logger.BeginScope("Scope1");

            // Assert
            Assert.NotNull(disposable);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void WindowsEventLog_Constructor_CreatesWithExpectedInformation()
        {
            // Arrange
            var logName = "foo";
            var machineName = "bar";
            var sourceName = "blah";

            // Act
            var windowsEventLog = new WindowsEventLog(logName, machineName, sourceName);

            // Assert
            Assert.NotNull(windowsEventLog.DiagnosticsEventLog);
            Assert.Equal(logName, windowsEventLog.DiagnosticsEventLog.Log);
            Assert.Equal(machineName, windowsEventLog.DiagnosticsEventLog.MachineName);
            Assert.Equal(sourceName, windowsEventLog.DiagnosticsEventLog.Source);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Constructor_CreatesWindowsEventLog_WithExpectedInformation()
        {
            // Arrange & Act
            var eventLogLogger = new EventLogLogger("Test", new EventLogSettings(), new LoggerExternalScopeProvider());

            // Assert
            var windowsEventLog = Assert.IsType<WindowsEventLog>(eventLogLogger.EventLog);
            Assert.Equal("Application", windowsEventLog.DiagnosticsEventLog.Log);
            Assert.Equal(".NET Runtime", windowsEventLog.DiagnosticsEventLog.Source);
            Assert.Equal(".", windowsEventLog.DiagnosticsEventLog.MachineName);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Constructor_CreatesWindowsEventLog_WithSuppliedEventLogSettings()
        {
            // Arrange
            var settings = new EventLogSettings()
            {
                SourceName = "foo",
                LogName = "bar",
                MachineName = "blah",
                EventLog = null
            };

            // Act
            var eventLogLogger = new EventLogLogger("Test", settings, new LoggerExternalScopeProvider());

            // Assert
            var windowsEventLog = Assert.IsType<WindowsEventLog>(eventLogLogger.EventLog);
            Assert.Equal(settings.LogName, windowsEventLog.DiagnosticsEventLog.Log);
            Assert.Equal(settings.SourceName, windowsEventLog.DiagnosticsEventLog.Source);
            Assert.Equal(settings.MachineName, windowsEventLog.DiagnosticsEventLog.MachineName);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void IOptions_CreatesWindowsEventLog_WithSuppliedEventLogSettings()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder => builder.AddEventLog());
            serviceCollection.Configure<EventLogSettings>(options =>
            {
                options.SourceName = "foo";
                options.LogName = "bar";
                options.MachineName = "blah";
                options.EventLog = null;
            });

            var services = serviceCollection.BuildServiceProvider();
            var provider = (EventLogLoggerProvider)(services.GetRequiredService<IEnumerable<ILoggerProvider>>().First());
            var settings = provider._settings;
            Assert.Equal("bar", settings.LogName);
            Assert.Equal("foo", settings.SourceName);
            Assert.Equal("blah", settings.MachineName);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(50)]
        [InlineData(49)]
        [InlineData(36)]
        public void MessageWithinMaxSize_WritesFullMessage(int messageSize)
        {
            var headerLength = "EventId: 0".Length + "Category: ".Length;
            // Arrange
            var loggerName = "Test";
            var maxMessageSize = 50 + headerLength + loggerName.Length + Environment.NewLine.Length * 4;
            var message = new string('a', messageSize);
            var expectedMessage = "Category: " + loggerName + Environment.NewLine +
                                  "EventId: 0" + Environment.NewLine + Environment.NewLine +
                                  message + Environment.NewLine;
            var testEventLog = new TestEventLog(maxMessageSize);
            var logger = new EventLogLogger(loggerName, new EventLogSettings() { EventLog = testEventLog }, new LoggerExternalScopeProvider());

            // Act
            logger.LogInformation(message);

            // Assert
            Assert.Single(testEventLog.Messages);
            Assert.Equal(expectedMessage, testEventLog.Messages[0]);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Message_WritesFullMessageWithScopes()
        {
            // Arrange
            var loggerName = "Test";
            var expectedMessage = "Category: " + loggerName + Environment.NewLine +
                                  "EventId: 0" + Environment.NewLine +
                                  "Outer Scope" + Environment.NewLine +
                                  "Inner Scope" + Environment.NewLine +
                                  "K1: V1" + Environment.NewLine +
                                  "K2: " + Environment.NewLine +
                                  "K3: " + Environment.NewLine +
                                  Environment.NewLine +
                                  "Message" + Environment.NewLine;
            var testEventLog = new TestEventLog(expectedMessage.Length);
            var logger = new EventLogLogger(loggerName, new EventLogSettings() { EventLog = testEventLog }, new LoggerExternalScopeProvider());
            var scopeWithValues = new Dictionary<string, object>
            {
                { "K1", "V1" },
                { "K2", "" },
                { "K3", null },
            };

            // Act
            using (logger.BeginScope("Outer Scope"))
            using (logger.BeginScope("Inner Scope"))
            using (logger.BeginScope(scopeWithValues))
            using (logger.BeginScope((object)null))
            {
                logger.LogInformation("Message");
            }

            // Assert
            Assert.Single(testEventLog.Messages);
            Assert.Equal(expectedMessage, testEventLog.Messages[0]);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void MessageWrittenToEventLogContainsEventId()
        {
            // Arrange
            var loggerName = "Test";
            var expectedMessage = "Category: " + loggerName + Environment.NewLine +
                                  "EventId: 1" + Environment.NewLine + Environment.NewLine +
                                  "Message" + Environment.NewLine;

            var testEventLog = new TestEventLog(expectedMessage.Length);
            var logger = new EventLogLogger(loggerName, new EventLogSettings() { EventLog = testEventLog }, new LoggerExternalScopeProvider());

            // Act
            logger.LogInformation(new EventId(1, "FooEvent"), "Message");

            // Assert
            Assert.Single(testEventLog.Messages);
            Assert.Equal(expectedMessage, testEventLog.Messages[0]);
            Assert.Equal(1, testEventLog.Entries[0].EventId);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void EventIdWrittenToEventLogUsesDefaultIfSpecified()
        {
            // Arrange
            var loggerName = "Test";
            var expectedMessage = "Category: " + loggerName + Environment.NewLine +
                                  "EventId: 1" + Environment.NewLine + Environment.NewLine +
                                  "Message" + Environment.NewLine;

            var testEventLog = new TestEventLog(expectedMessage.Length)
            {
                DefaultEventId = 1034
            };
            var logger = new EventLogLogger(loggerName, new EventLogSettings() { EventLog = testEventLog }, new LoggerExternalScopeProvider());

            // Act
            logger.LogInformation(new EventId(1, "FooEvent"), "Message");

            // Assert
            Assert.Single(testEventLog.Messages);
            Assert.Equal(expectedMessage, testEventLog.Messages[0]);
            Assert.Equal(1034, testEventLog.Entries[0].EventId);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void NullCategoryNameThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new EventLogLogger(null, new EventLogSettings() { }, new LoggerExternalScopeProvider()));
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void NullEventSettingsThrows()
        {
            Assert.Throws<ArgumentNullException>(() => new EventLogLogger("Something", settings: null, new LoggerExternalScopeProvider()));
        }

        public static TheoryData<int, string[]> WritesSplitMessagesData
        {
            get
            {
                return new TheoryData<int, string[]>
                {
                    {
                        10,
                        new []
                        {
                            "Category: Test\r\nEventId: 0\r...",
                            "...\n\r\naaaaaaaaaa\r\n",
                        }
                    },
                    {
                        20,
                        new []
                        {
                            "Category: Test\r\nEventId: 0\r...",
                            "...\n\r\naaaaaaaaaaaaaaaaaaaa\r\n",
                        }
                    },
                    {
                        30, // equaling the max message size
                        new []
                        {
                            "Category: Test\r\nEventId: 0\r...",
                            "...\n\r\naaaaaaaaaaaaaaaaaaaaa...",
                            "...aaaaaaaaa\r\n",
                        }

                    },
                    {
                        40,
                        new []
                        {
                            "Category: Test\r\nEventId: 0\r...",
                            "...\n\r\naaaaaaaaaaaaaaaaaaaaa...",
                            "...aaaaaaaaaaaaaaaaaaa\r\n",
                        }
                    }
                };
            }
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Windows)]
        [MemberData(nameof(WritesSplitMessagesData))]
        public void MessageExceedingMaxSize_WritesSplitMessages(int messageSize, string[] expectedMessages)
        {
            // Arrange
            var headerLength = "EventId: 0".Length + "Category: ".Length;
            var loggerName = "Test";
            var maxMessageSize = headerLength + loggerName.Length + Environment.NewLine.Length * 3;
            var message = new string('a', messageSize);
            var testEventLog = new TestEventLog(maxMessageSize);
            var logger = new EventLogLogger(loggerName, new EventLogSettings() { EventLog = testEventLog }, new LoggerExternalScopeProvider());

            // Act
            logger.LogInformation(message);

            // Assert
            Assert.Equal(expectedMessages.Length, testEventLog.Messages.Count);
            Assert.Equal(expectedMessages, testEventLog.Messages);
        }

        private class TestEventLog : IEventLog
        {
            public TestEventLog(int maxMessageSize)
            {
                MaxMessageSize = maxMessageSize;
                Messages = new List<string>();
                Entries = new List<(string Message, EventLogEntryType Type, int EventId, short Category)>();
            }

            public int MaxMessageSize { get; }

            public List<string> Messages { get; }

            public List<(string Message, EventLogEntryType Type, int EventId, short Category)> Entries { get; }

            public int? DefaultEventId { get; set; }

            public void WriteEntry(string message, EventLogEntryType type, int eventID, short category)
            {
                Messages.Add(message);
                Entries.Add((message, type, eventID, category));
            }
        }
    }
}
