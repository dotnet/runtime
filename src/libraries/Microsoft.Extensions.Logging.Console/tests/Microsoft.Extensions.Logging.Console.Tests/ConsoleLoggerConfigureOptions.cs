// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class ConsoleLoggerConfigureOptions
    {
        [Fact]
        public void EnsureConsoleLoggerOptions_ConfigureOptions_SupportsAllProperties()
        {
            // NOTE: if this test fails, it is because a property was added to one of the following types.
            // When adding a new property to one of these types, ensure the corresponding
            // IConfigureOptions class is updated for the new property.

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            Assert.Equal(9, typeof(ConsoleLoggerOptions).GetProperties(flags).Length);
            Assert.Equal(3, typeof(ConsoleFormatterOptions).GetProperties(flags).Length);
            Assert.Equal(5, typeof(SimpleConsoleFormatterOptions).GetProperties(flags).Length);
            Assert.Equal(4, typeof(JsonConsoleFormatterOptions).GetProperties(flags).Length);
            Assert.Equal(6, typeof(JsonWriterOptions).GetProperties(flags).Length);
        }

        [Theory]
        [InlineData("Console:LogToStandardErrorThreshold", "invalid")]
        [InlineData("Console:MaxQueueLength", "notANumber")]
        [InlineData("Console:QueueFullMode", "invalid")]
        [InlineData("Console:FormatterOptions:IncludeScopes", "not a bool")]
        [InlineData("Console:FormatterOptions:UseUtcTimestamp", "not a bool")]
        [InlineData("Console:FormatterOptions:ColorBehavior", "not a behavior")]
        [InlineData("Console:FormatterOptions:SingleLine", "not a bool")]
        [InlineData("Console:FormatterOptions:JsonWriterOptions:Indented", "not a bool")]
        [InlineData("Console:FormatterOptions:JsonWriterOptions:MaxDepth", "not an int")]
        [InlineData("Console:FormatterOptions:JsonWriterOptions:SkipValidation", "not a bool")]
        public void ConsoleLoggerConfigureOptions_InvalidConfigurationData(string key, string value)
        {
            var configuration = new ConfigurationManager();
            configuration.AddInMemoryCollection(new[] { new KeyValuePair<string, string>(key, value) });

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsole())
                .BuildServiceProvider();

            InvalidOperationException e = Assert.Throws<InvalidOperationException>(() => serviceProvider.GetRequiredService<ILoggerProvider>());

            // "Console:" is stripped off from the config path since that config section is read by the ConsoleLogger, and not part of the Options path.
            string configPath = key.Substring("Console:".Length);
            Assert.Contains(configPath, e.Message);
        }
    }
}
