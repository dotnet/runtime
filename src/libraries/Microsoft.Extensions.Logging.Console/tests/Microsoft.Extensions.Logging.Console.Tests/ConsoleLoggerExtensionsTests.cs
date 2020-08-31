
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class ConsoleLoggerExtensionsTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddConsole_NullConfigure_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new ServiceCollection()
                    .AddLogging(builder => 
                    {
                        builder.AddConsole(null);
                    }));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddSimpleConsole_NullConfigure_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new ServiceCollection()
                    .AddLogging(builder => 
                    {
                        builder.AddSimpleConsole(null);
                    }));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddSystemdConsole_NullConfigure_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new ServiceCollection()
                    .AddLogging(builder => 
                    {
                        builder.AddSystemdConsole(null);
                    }));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddJsonConsole_NullConfigure_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new ServiceCollection()
                    .AddLogging(builder => 
                    {
                        builder.AddJsonConsole(null);
                    }));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddConsoleFormatter_NullConfigure_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => 
                new ServiceCollection()
                    .AddLogging(builder => 
                    {
                        builder.AddConsoleFormatter<CustomFormatter, CustomOptions>(null);
                    }));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [MemberData(nameof(FormatterNames))]
        public void AddConsole_ConsoleLoggerOptionsFromConfigFile_IsReadFromLoggingConfiguration(string formatterName)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:FormatterName", formatterName)
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsole())
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal(formatterName, logger.Options.FormatterName);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddConsoleFormatter_CustomFormatter_IsReadFromLoggingConfiguration()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:FormatterName", "custom"),
                new KeyValuePair<string, string>("Console:FormatterOptions:CustomLabel", "random"),
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsoleFormatter<CustomFormatter, CustomOptions>(fOptions => { fOptions.CustomLabel = "random"; })
                    .AddConsole(o => { o.FormatterName = "custom"; })
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal("custom", logger.Options.FormatterName);
            var formatter = Assert.IsType<CustomFormatter>(logger.Formatter);
            Assert.Equal("random", formatter.FormatterOptions.CustomLabel);
        }

        private class CustomFormatter : ConsoleFormatter, IDisposable
        {
            private IDisposable _optionsReloadToken;

            public CustomFormatter(IOptionsMonitor<CustomOptions> options)
                : base("custom")
            {
                ReloadLoggerOptions(options.CurrentValue);
                _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
            }

            private void ReloadLoggerOptions(CustomOptions options)
            {
                FormatterOptions = options;
            }

            public CustomOptions FormatterOptions { get; set; }
            public string CustomLog { get; set; }

            public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
            {
                CustomLog = logEntry.Formatter(logEntry.State, logEntry.Exception);
            }

            public void Dispose()
            {
                _optionsReloadToken?.Dispose();
            }
        }

        private class CustomOptions : ConsoleFormatterOptions
        {
            public string CustomLabel { get; set; }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddSimpleConsole_ChangeProperties_IsReadFromLoggingConfiguration()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:FormatterOptions:ColorBehavior", "Disabled"),
                new KeyValuePair<string, string>("Console:FormatterOptions:SingleLine", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:TimestampFormat", "HH:mm "),
                new KeyValuePair<string, string>("Console:FormatterOptions:UseUtcTimestamp", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:IncludeScopes", "true"),
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddSimpleConsole()
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal(ConsoleFormatterNames.Simple, logger.Options.FormatterName);
            var formatter = Assert.IsType<SimpleConsoleFormatter>(logger.Formatter);
            Assert.Equal(LoggerColorBehavior.Disabled, formatter.FormatterOptions.ColorBehavior);
            Assert.True(formatter.FormatterOptions.SingleLine);
            Assert.Equal("HH:mm ", formatter.FormatterOptions.TimestampFormat);
            Assert.True(formatter.FormatterOptions.UseUtcTimestamp);
            Assert.True(formatter.FormatterOptions.IncludeScopes);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddSimpleConsole_OutsideConfig_TakesProperty()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:FormatterOptions:TimestampFormat", "HH:mm "),
                new KeyValuePair<string, string>("Console:FormatterOptions:UseUtcTimestamp", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:IncludeScopes", "false"),
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddSimpleConsole(o => {
                        o.TimestampFormat = "HH:mm:ss ";
                        o.IncludeScopes = false;
                        o.UseUtcTimestamp = true;
                    })
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal(ConsoleFormatterNames.Simple, logger.Options.FormatterName);
            var formatter = Assert.IsType<SimpleConsoleFormatter>(logger.Formatter);
            Assert.Equal("HH:mm:ss ", formatter.FormatterOptions.TimestampFormat);
            Assert.False(formatter.FormatterOptions.IncludeScopes);
            Assert.True(formatter.FormatterOptions.UseUtcTimestamp);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddSystemdConsole_ChangeProperties_IsReadFromLoggingConfiguration()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:FormatterOptions:TimestampFormat", "HH:mm "),
                new KeyValuePair<string, string>("Console:FormatterOptions:UseUtcTimestamp", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:IncludeScopes", "true"),
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddSystemdConsole()
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal(ConsoleFormatterNames.Systemd, logger.Options.FormatterName);
            var formatter = Assert.IsType<SystemdConsoleFormatter>(logger.Formatter);
            Assert.Equal("HH:mm ", formatter.FormatterOptions.TimestampFormat);
            Assert.True(formatter.FormatterOptions.UseUtcTimestamp);
            Assert.True(formatter.FormatterOptions.IncludeScopes);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddSystemdConsole_OutsideConfig_TakesProperty()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:FormatterOptions:TimestampFormat", "HH:mm "),
                new KeyValuePair<string, string>("Console:FormatterOptions:UseUtcTimestamp", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:IncludeScopes", "true"),
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddSystemdConsole(o => {
                        o.TimestampFormat = "HH:mm:ss ";
                        o.IncludeScopes = false;
                        o.UseUtcTimestamp = false;
                    })
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal(ConsoleFormatterNames.Systemd, logger.Options.FormatterName);
            var formatter = Assert.IsType<SystemdConsoleFormatter>(logger.Formatter);
            Assert.Equal("HH:mm:ss ", formatter.FormatterOptions.TimestampFormat);
            Assert.False(formatter.FormatterOptions.UseUtcTimestamp);
            Assert.False(formatter.FormatterOptions.IncludeScopes);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddJsonConsole_ChangeProperties_IsReadFromLoggingConfiguration()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:FormatterOptions:TimestampFormat", "HH:mm "),
                new KeyValuePair<string, string>("Console:FormatterOptions:UseUtcTimestamp", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:IncludeScopes", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:JsonWriterOptions:Indented", "true"),
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddJsonConsole()
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal(ConsoleFormatterNames.Json, logger.Options.FormatterName);
            var formatter = Assert.IsType<JsonConsoleFormatter>(logger.Formatter);
            Assert.Equal("HH:mm ", formatter.FormatterOptions.TimestampFormat);
            Assert.True(formatter.FormatterOptions.UseUtcTimestamp);
            Assert.True(formatter.FormatterOptions.IncludeScopes);
            Assert.True(formatter.FormatterOptions.JsonWriterOptions.Indented);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddJsonConsole_OutsideConfig_TakesProperty()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:FormatterOptions:TimestampFormat", "HH:mm "),
                new KeyValuePair<string, string>("Console:FormatterOptions:UseUtcTimestamp", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:IncludeScopes", "true"),
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddJsonConsole(o => {
                        o.JsonWriterOptions = new JsonWriterOptions()
                        {
                            Indented = false,
                            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                        };
                    })
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal(ConsoleFormatterNames.Json, logger.Options.FormatterName);
            var formatter = Assert.IsType<JsonConsoleFormatter>(logger.Formatter);
            Assert.Equal("HH:mm ", formatter.FormatterOptions.TimestampFormat);
            Assert.True(formatter.FormatterOptions.UseUtcTimestamp);
            Assert.True(formatter.FormatterOptions.IncludeScopes);
            Assert.False(formatter.FormatterOptions.JsonWriterOptions.Indented);
            Assert.Equal(JavaScriptEncoder.UnsafeRelaxedJsonEscaping, formatter.FormatterOptions.JsonWriterOptions.Encoder);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddConsole_NullFormatterNameUsingSystemdFormat_AnyDeprecatedPropertiesOverwriteFormatterOptions()
        {
            var configs = new[] {
                new KeyValuePair<string, string>("Console:Format", "Systemd"),
                new KeyValuePair<string, string>("Console:TimestampFormat", "HH:mm:ss "),
                new KeyValuePair<string, string>("Console:FormatterOptions:TimestampFormat", "HH:mm "),
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(configs).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsole(o => { })
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Null(logger.Options.FormatterName);
            var formatter = Assert.IsType<SystemdConsoleFormatter>(logger.Formatter);
            Assert.Equal("HH:mm:ss ", formatter.FormatterOptions.TimestampFormat);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddConsole_NullFormatterName_UsingSystemdFormat_IgnoreFormatterOptionsAndUseDeprecatedInstead()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:Format", "Systemd"),
                new KeyValuePair<string, string>("Console:IncludeScopes", "true"),
                new KeyValuePair<string, string>("Console:TimestampFormat", "HH:mm:ss "),
                new KeyValuePair<string, string>("Console:FormatterOptions:TimestampFormat", "HH:mm "),
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsole(o => { 
#pragma warning disable CS0618
                        o.IncludeScopes = false;
#pragma warning restore CS0618
                    })
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Null(logger.Options.FormatterName);
            var formatter = Assert.IsType<SystemdConsoleFormatter>(logger.Formatter);
            
            Assert.Equal("HH:mm:ss ", formatter.FormatterOptions.TimestampFormat);  // ignore FormatterOptions, using deprecated one
            Assert.False(formatter.FormatterOptions.UseUtcTimestamp);               // not set anywhere, defaulted to false
            Assert.False(formatter.FormatterOptions.IncludeScopes);                 // setup using lambda wins over config
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void AddConsole_NullFormatterName_UsingDefaultFormat_IgnoreFormatterOptionsAndUseDeprecatedInstead()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:Format", "Default"),
                new KeyValuePair<string, string>("Console:IncludeScopes", "true"),
                new KeyValuePair<string, string>("Console:TimestampFormat", "HH:mm:ss "),
                new KeyValuePair<string, string>("Console:FormatterOptions:SingleLine", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:TimestampFormat", "HH:mm "),
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsole(o => { 
#pragma warning disable CS0618
                        o.DisableColors = true;
                        o.IncludeScopes = false;
#pragma warning restore CS0618
                    })
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Null(logger.Options.FormatterName);
#pragma warning disable CS0618
            Assert.True(logger.Options.DisableColors);
#pragma warning restore CS0618
            var formatter = Assert.IsType<SimpleConsoleFormatter>(logger.Formatter);
            
            Assert.False(formatter.FormatterOptions.SingleLine);                    // ignored
            Assert.Equal("HH:mm:ss ", formatter.FormatterOptions.TimestampFormat);  // ignore FormatterOptions, using deprecated one
            Assert.False(formatter.FormatterOptions.UseUtcTimestamp);               // not set anywhere, defaulted to false
            Assert.False(formatter.FormatterOptions.IncludeScopes);                 // setup using lambda wins over config
            Assert.Equal(LoggerColorBehavior.Disabled, formatter.FormatterOptions.ColorBehavior);                  // setup using lambda
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData("missingFormatter")]
        [InlineData("simple")]
        [InlineData("Simple")]
        public void AddConsole_FormatterNameIsSet_UsingDefaultFormat_IgnoreDeprecatedAndUseFormatterOptionsInstead(string formatterName)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:Format", "Default"),
                new KeyValuePair<string, string>("Console:FormatterName", formatterName),
                new KeyValuePair<string, string>("Console:TimestampFormat", "HH:mm:ss "),
                new KeyValuePair<string, string>("Console:FormatterOptions:IncludeScopes", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:SingleLine", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:TimestampFormat", "HH:mm "),
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsole(o => { 
#pragma warning disable CS0618
                        o.DisableColors = true;
                        o.IncludeScopes = false;
#pragma warning restore CS0618
                    })
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal(formatterName, logger.Options.FormatterName);
#pragma warning disable CS0618
            Assert.True(logger.Options.DisableColors);
#pragma warning restore CS0618
            var formatter = Assert.IsType<SimpleConsoleFormatter>(logger.Formatter);

            Assert.True(formatter.FormatterOptions.SingleLine);                    // picked from FormatterOptions
            Assert.Equal("HH:mm ", formatter.FormatterOptions.TimestampFormat);     // ignore deprecated, using FormatterOptions instead
            Assert.False(formatter.FormatterOptions.UseUtcTimestamp);               // not set anywhere, defaulted to false
            Assert.True(formatter.FormatterOptions.IncludeScopes);                  // ignore deprecated set in lambda use FormatterOptions instead
            Assert.Equal(LoggerColorBehavior.Default, formatter.FormatterOptions.ColorBehavior);                 // ignore deprecated set in lambda, defaulted to false
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData("missingFormatter")]
        [InlineData("systemd")]
        [InlineData("Systemd")]
        public void AddConsole_FormatterNameIsSet_UsingSystemdFormat_IgnoreDeprecatedAndUseFormatterOptionsInstead(string formatterName)
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new[] {
                new KeyValuePair<string, string>("Console:Format", "Systemd"),
                new KeyValuePair<string, string>("Console:FormatterName", formatterName),
                new KeyValuePair<string, string>("Console:TimestampFormat", "HH:mm:ss "),
                new KeyValuePair<string, string>("Console:FormatterOptions:IncludeScopes", "true"),
                new KeyValuePair<string, string>("Console:FormatterOptions:TimestampFormat", "HH:mm "),
            }).Build();

            var loggerProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConfiguration(configuration)
                    .AddConsole(o => { 
#pragma warning disable CS0618
                        o.UseUtcTimestamp = true;
                        o.IncludeScopes = false;
#pragma warning restore CS0618
                    })
                )
                .BuildServiceProvider()
                .GetRequiredService<ILoggerProvider>();

            var consoleLoggerProvider = Assert.IsType<ConsoleLoggerProvider>(loggerProvider);
            var logger = (ConsoleLogger)consoleLoggerProvider.CreateLogger("Category");
            Assert.Equal(formatterName, logger.Options.FormatterName);
#pragma warning disable CS0618
            Assert.True(logger.Options.UseUtcTimestamp);
#pragma warning restore CS0618
            var formatter = Assert.IsType<SystemdConsoleFormatter>(logger.Formatter);

            Assert.Equal("HH:mm ", formatter.FormatterOptions.TimestampFormat);     // ignore deprecated, using FormatterOptions instead
            Assert.True(formatter.FormatterOptions.IncludeScopes);                  // ignore deprecated set in lambda use FormatterOptions instead
            Assert.False(formatter.FormatterOptions.UseUtcTimestamp);               // ignore deprecated set in lambda, defaulted to false
        }

        public static TheoryData<string> FormatterNames
        {
            get
            {
                var data = new TheoryData<string>();
                data.Add(ConsoleFormatterNames.Simple);
                data.Add(ConsoleFormatterNames.Systemd);
                data.Add(ConsoleFormatterNames.Json);
                return data;
            }
        }
    }
}
