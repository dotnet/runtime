// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LoggerFilterTest
    {
        [Fact]
        public void ChangingConfigReloadsDefaultFilter()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""LogLevel"": {
      ""Microsoft"": ""Information""
    }
  }
}";
            var config = TestConfiguration.Create(() => json);
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);

            var factory = TestLoggerBuilder.Create(builder => builder
                .AddConfiguration(config.GetSection("Logging"))
                .AddProvider(loggerProvider));

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Empty(writes);

            json =
@"{
  ""Logging"": {
    ""LogLevel"": {
      ""Microsoft"": ""Trace""
    }
  }
}";
            config.Reload();

            // Act
            logger.LogTrace("Message");

            // Assert
            writes = loggerProvider.Sink.Writes;
            Assert.Single(writes);
        }

        [Fact]
        public void ChangingConfigFromUseConfigurationReloadsDefaultFilter()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""LogLevel"": {
      ""Microsoft"": ""Information""
    }
  }
}";
            var config = TestConfiguration.Create(() => json);
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var factory = TestLoggerBuilder.Create(builder => builder
                .AddConfiguration(config.GetSection("Logging"))
                .AddProvider(loggerProvider));

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Empty(writes);

            json =
@"{
  ""Logging"": {
    ""LogLevel"": {
      ""Microsoft"": ""Trace""
    }
  }
}";
            config.Reload();

            // Act
            logger.LogTrace("Message");

            // Assert
            writes = loggerProvider.Sink.Writes;
            Assert.Single(writes);
        }

        [Fact]
        public void CanFilterOnNamedProviders()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""Microsoft.Extensions.Logging.Test.TestLoggerProvider"": {
      ""LogLevel"": {
        ""Microsoft"": ""Information""
      }
    }
  }
}";
            var config = TestConfiguration.Create(() => json);

            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var factory = TestLoggerBuilder.Create(builder => builder
                .AddConfiguration(config.GetSection("Logging"))
                .AddProvider(loggerProvider));

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Empty(writes);
        }

        [Fact]
        public void PreferFullNameOverDefaultForFiltering()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""LogLevel"": {
      ""Microsoft"": ""Critical""
    },
    ""TestLogger"": {
      ""LogLevel"": {
        ""Microsoft"": ""Trace""
      }
    }
  }
}";
            var config = TestConfiguration.Create(() => json);

            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var factory = TestLoggerBuilder.Create(builder => builder
                .AddConfiguration(config.GetSection("Logging"))
                .AddProvider(loggerProvider));

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Single(writes);
        }

        [Fact]
        public void DefaultCategoryNameIsUsedIfNoneMatch()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""Microsoft.Extensions.Logging.Test.TestLoggerProvider"": {
      ""LogLevel"": {
        ""Default"": ""Information"",
        ""Microsoft"": ""Warning""
      }
    }
  }
}";
            var config = TestConfiguration.Create(() => json);

            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var factory = TestLoggerBuilder.Create(builder => builder
                .AddConfiguration(config.GetSection("Logging"))
                .AddProvider(loggerProvider));

            var logger = factory.CreateLogger("Microsoft");

            // Act
            logger.LogTrace("Message");

            // Assert
            var writes = loggerProvider.Sink.Writes;
            Assert.Empty(writes);

            // No config value for 'None' so should use 'Default'
            logger = factory.CreateLogger("None");

            // Act
            logger.LogTrace("Message");

            // Assert
            Assert.Empty(writes);

            // Act
            logger.LogInformation("Message");

            // Assert
            Assert.Single(writes);
        }

        [Fact]
        public void AddFilterForMatchingProviderFilters()
        {
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var factory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(provider)
                .AddFilter((name, cat, level) =>
                {
                    if (string.Equals("Microsoft.Extensions.Logging.Test.TestLoggerProvider", name))
                    {
                        if (string.Equals("Test", cat))
                        {
                            return level >= LogLevel.Information;
                        }
                    }

                    return true;
                }));

            var logger = factory.CreateLogger("Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Single(writes);

            logger.LogTrace("Message");

            Assert.Single(writes);
        }

        [Fact]
        public void AddFilterForNonMatchingProviderDoesNotFilter()
        {
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var factory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(provider)
                .AddFilter((name, cat, level) =>
                {
                    if (string.Equals("None", name))
                    {
                        return level >= LogLevel.Error;
                    }

                    return true;
                }));

            var logger = factory.CreateLogger("Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Single(writes);
        }

        [Fact]
        public void AddFilterLastWins()
        {
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var factory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(provider)
                .AddFilter((name, cat, level) => level >= LogLevel.Warning)
                .AddFilter((name, cat, level) => string.Equals(cat, "NotTest")));

            var logger = factory.CreateLogger("Test");

            logger.LogWarning("Message");

            var writes = provider.Sink.Writes;
            Assert.Empty(writes);

            logger = factory.CreateLogger("NotTest");

            logger.LogInformation("Message");

            Assert.Single(writes);

            logger.LogError("Message");

            Assert.Equal(2, writes.Count);
        }

        [Fact]
        public void ProviderLevelIsPreferredOverGlobalFilter()
        {
            // Arrange
            var json =
@"{
  ""Logging"": {
    ""TestLogger"": {
      ""LogLevel"": {
        ""Test"": ""Debug""
      }
    }
  }
}";
            var config = TestConfiguration.Create(() => json);
            var loggerProvider = new TestLoggerProvider(new TestSink(), isEnabled: true);

            var factory = TestLoggerBuilder.Create(builder => builder
                .AddConfiguration(config.GetSection("Logging"))
                .AddProvider(loggerProvider)
                .AddFilter((name, cat, level) => level < LogLevel.Critical));

            var logger = factory.CreateLogger("Test");

            var writes = loggerProvider.Sink.Writes;

            logger.LogTrace("Message");

            Assert.Empty(writes);

            logger.LogInformation("Message");

            Assert.Single(writes);

            logger.LogCritical("Message");

            Assert.Equal(2, writes.Count);
        }

        [Fact]
        public void AddFilterWithProviderNameCategoryNameAndFilterFuncFiltersCorrectly()
        {
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var factory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(provider)
                .AddFilter<TestLoggerProvider>((cat, level) => level >= LogLevel.Warning));

            var logger = factory.CreateLogger("Sample.Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Empty(writes);

            logger.LogWarning("Message");

            Assert.Single(writes);
        }

        [Fact]
        public void AddFilterWithProviderNameCategoryNameAndMinLevelFiltersCorrectly()
        {
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var factory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(provider)
                .AddFilter<TestLoggerProvider>("Sample", LogLevel.Warning));

            var logger = factory.CreateLogger("Sample.Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Empty(writes);

            logger.LogWarning("Message");

            Assert.Single(writes);
        }

        [Fact]
        public void AddFilterWithProviderNameAndCategoryFilterFuncFiltersCorrectly()
        {
            var provider = new TestLoggerProvider(new TestSink(), isEnabled: true);
            var factory = TestLoggerBuilder.Create(builder => builder
                .AddProvider(provider)
                .AddFilter<TestLoggerProvider>((c, l) => l >= LogLevel.Warning));

            var logger = factory.CreateLogger("Sample.Test");

            logger.LogInformation("Message");

            var writes = provider.Sink.Writes;
            Assert.Empty(writes);

            logger.LogWarning("Message");

            Assert.Single(writes);
        }

        [Fact]
        public void LogLevelKeyIsCaseInsensitive()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(
                    builder => builder.AddConfiguration(new ConfigurationBuilder()
                                                            .AddInMemoryCollection(new[] {new KeyValuePair<string, string>("logLevel:Default", "Error")})
                                                            .Build())
                )
                .BuildServiceProvider();

            var options = serviceProvider.GetRequiredService<IOptions<LoggerFilterOptions>>();

            Assert.Equal(LogLevel.Error, options.Value.Rules.Single().LogLevel);
        }

        [Fact]
        public void DefaultCategoryIsCaseInsensitive()
        {
            var serviceProvider = new ServiceCollection()
                .AddLogging(
                    builder => builder.AddConfiguration(new ConfigurationBuilder()
                                                            .AddInMemoryCollection(new[] {new KeyValuePair<string, string>("LogLevel:default", "Error")})
                                                            .Build())
                )
                .BuildServiceProvider();

            var options = serviceProvider.GetRequiredService<IOptions<LoggerFilterOptions>>();

            Assert.Null(options.Value.Rules.Single().CategoryName);
        }

        [Fact]
        public void MultipleWildcardsAreNotAllowed()
        {
            var options = new LoggerFilterOptions()
            {
                Rules = { new LoggerFilterRule(providerName: null, categoryName: "*A*", logLevel: null, filter: null)}
            };
            var testSink1 = new TestSink();
            var loggerFactory = new LoggerFactory(new[]
            {
                new TestLoggerProvider2(testSink1)
            }, options);

            var exception = Assert.Throws<InvalidOperationException>(() => loggerFactory.CreateLogger("Category"));
            Assert.Equal("Only one wildcard character is allowed in category name.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(FilterTestData))]
        public void FilterTest(LoggerFilterOptions options, (string category, LogLevel level, bool expectInProvider1, bool expectInProvider2) message)
        {
            var testSink1 = new TestSink();
            var testSink2 = new TestSink();

            var loggerFactory = new LoggerFactory(new[]
            {
                new TestLoggerProvider(testSink1, true),
                new TestLoggerProvider2(testSink2)
            }, options);

            var logger = loggerFactory.CreateLogger(message.category);
            Assert.Equal(message.expectInProvider1 || message.expectInProvider2, logger.IsEnabled(message.Item2));
            logger.Log(message.level, 0, "hello", null, (s, exception) => s);

            Assert.Equal(message.expectInProvider1 ? 1 : 0, testSink1.Writes.Count);
            Assert.Equal(message.expectInProvider2 ? 1 : 0, testSink2.Writes.Count);
        }

        public static TheoryData<LoggerFilterOptions, (string, LogLevel, bool, bool)> FilterTestData =
            new TheoryData<LoggerFilterOptions, (string, LogLevel, bool, bool)>()
            {
                {
                    new LoggerFilterOptions()
                    {
                        Rules =
                        {
                            new LoggerFilterRule(typeof(TestLoggerProvider).FullName, "System", LogLevel.Information, null),
                            new LoggerFilterRule(null, "Microsoft", LogLevel.Trace, null)
                        }
                    },
                    ("Microsoft", LogLevel.Debug, true, true)
                },
                {  // Provider specific rule if preferred
                    new LoggerFilterOptions()
                    {
                        Rules =
                        {
                            new LoggerFilterRule(typeof(TestLoggerProvider).FullName, null, LogLevel.Information, null),
                            new LoggerFilterRule(null, null, LogLevel.Critical, null)
                        }
                    },
                    ("Category", LogLevel.Information, true, false)
                },
                { // Category specific rule if preferred
                    new LoggerFilterOptions()
                    {
                        Rules =
                        {
                            new LoggerFilterRule(null, "Category", LogLevel.Information, null),
                            new LoggerFilterRule(null, null, LogLevel.Critical, null)
                        }
                    },
                    ("Category", LogLevel.Information, true, true)
                },
                { // Longest category specific rule if preferred
                    new LoggerFilterOptions()
                    {
                        Rules =
                        {
                            new LoggerFilterRule(null, "Category.Sub", LogLevel.Trace, null),
                            new LoggerFilterRule(null, "Category", LogLevel.Information, null),
                            new LoggerFilterRule(null, null, LogLevel.Critical, null)
                        }
                    },
                    ("Category.Sub", LogLevel.Trace, true, true)
                },
                { // Provider is selected first, then category
                    new LoggerFilterOptions()
                    {
                        Rules =
                        {
                            new LoggerFilterRule(null, "Category.Sub", LogLevel.Trace, null),
                            new LoggerFilterRule(typeof(TestLoggerProvider).FullName, "Category", LogLevel.Information, null),
                            new LoggerFilterRule(null, null, LogLevel.Critical, null)
                        }
                    },
                    ("Category.Sub", LogLevel.Trace, false, true)
                },
                { // Last most specific is selected
                    new LoggerFilterOptions()
                    {
                        Rules =
                        {
                            new LoggerFilterRule(null, "Category.Sub", LogLevel.Trace, null),
                            new LoggerFilterRule(typeof(TestLoggerProvider).FullName, "Category", LogLevel.Information, null),
                            new LoggerFilterRule(typeof(TestLoggerProvider).FullName, "Category", LogLevel.Trace, null),
                            new LoggerFilterRule(null, null, LogLevel.Critical, null)
                        }
                    },
                    ("Category.Sub", LogLevel.Trace, true, true)
                },
                { // Filter is used if matches level
                    new LoggerFilterOptions()
                    {
                        Rules =
                        {
                            new LoggerFilterRule(null, null, LogLevel.Critical, (logger, category, level) => true)
                        }
                    },
                    ("Category.Sub", LogLevel.Error, false, false)
                },
                { // Last filter is used is used
                    new LoggerFilterOptions()
                    {
                        Rules =
                        {
                            new LoggerFilterRule(null, null, LogLevel.Critical, (logger, category, level) => false),
                            new LoggerFilterRule(null, null, LogLevel.Critical, (logger, category, level) => true)
                        }
                    },
                    ("Category.Sub", LogLevel.Critical, true, true)
                },
                { // MinLevel is used when no match
                    new LoggerFilterOptions()
                    {
                        Rules =
                        {
                            new LoggerFilterRule(typeof(TestLoggerProvider).FullName, null, LogLevel.Trace, null),
                        },
                        MinLevel = LogLevel.Debug
                    },
                    ("Category.Sub", LogLevel.Trace, true, false)
                },
                { // Provider aliases work
                    new LoggerFilterOptions()
                    {
                        Rules =
                        {
                            new LoggerFilterRule(typeof(TestLoggerProvider).FullName, "Category", LogLevel.Information, null),
                            new LoggerFilterRule("TestLogger", "Category", LogLevel.Trace, null),
                            new LoggerFilterRule(null, null, LogLevel.Critical, null)
                        }
                    },
                    ("Category.Sub", LogLevel.Trace, true, false)
                },
                { // Aliases equivalent to full names
                    new LoggerFilterOptions()
                    {
                        Rules =
                        {
                            new LoggerFilterRule("TestLogger", "Category", LogLevel.Information, null),
                            new LoggerFilterRule(typeof(TestLoggerProvider).FullName, "Category", LogLevel.Trace, null),
                            new LoggerFilterRule(null, null, LogLevel.Critical, null)
                        }
                    },
                    ("Category.Sub", LogLevel.Trace, true, false)
                },
                { // Wildcards allowed in category names
                    new LoggerFilterOptions()
                    {
                        MinLevel = LogLevel.Critical,
                        Rules =
                        {
                            new LoggerFilterRule(typeof(TestLoggerProvider).FullName, "Category.*.Sub", LogLevel.Trace, null),
                            new LoggerFilterRule(null, null, LogLevel.Critical, null)
                        }
                    },
                    ("Category.B.Sub", LogLevel.Trace, true, false)
                },
                { // Wildcards allowed in the beginning of category names
                    new LoggerFilterOptions()
                    {
                        MinLevel = LogLevel.Critical,
                        Rules =
                        {
                            new LoggerFilterRule(typeof(TestLoggerProvider).FullName, "*.Sub", LogLevel.Trace, null),
                        }
                    },
                    ("Category.B.Sub", LogLevel.Trace, true, false)
                },
                { // Wildcards allowed in the end of category names
                    new LoggerFilterOptions()
                    {
                        MinLevel = LogLevel.Critical,
                        Rules =
                        {
                            new LoggerFilterRule(typeof(TestLoggerProvider).FullName, "Cat*", LogLevel.Trace, null),
                        }
                    },
                    ("Category.B.Sub", LogLevel.Trace, true, false)
                }
            };
    }
}
