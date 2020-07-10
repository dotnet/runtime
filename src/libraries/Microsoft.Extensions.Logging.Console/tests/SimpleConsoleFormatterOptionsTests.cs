// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class SimpleConsoleFormatterOptionsTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void SimpleConsoleFormatterOptions_ChangeProperties_ChangesReflected()
        {
            // Arrange
            var formatterMonitor = new TestSimpleFormatterOptionsMonitor(new SimpleConsoleFormatterOptions()
            { 
                DisableColors = false,
                SingleLine = true,
                TimestampFormat = "HH:mm ",
                UseUtcTimestamp = true,
                IncludeScopes = false
            });
            var simpleFormatter = new SimpleConsoleFormatter(formatterMonitor);

            // Act & Assert
            Assert.False(simpleFormatter.FormatterOptions.DisableColors);
            Assert.True(simpleFormatter.FormatterOptions.SingleLine);
            Assert.Equal("HH:mm ", simpleFormatter.FormatterOptions.TimestampFormat);
            Assert.True(simpleFormatter.FormatterOptions.UseUtcTimestamp);
            Assert.False(simpleFormatter.FormatterOptions.IncludeScopes);
            formatterMonitor.Set(new SimpleConsoleFormatterOptions()
            {
                DisableColors = true,
                SingleLine = false,
                TimestampFormat = "HH:mm:ss ",
                UseUtcTimestamp = false,
                IncludeScopes = true
            });
            Assert.True(simpleFormatter.FormatterOptions.DisableColors);
            Assert.False(simpleFormatter.FormatterOptions.SingleLine);
            Assert.Equal("HH:mm:ss ", simpleFormatter.FormatterOptions.TimestampFormat);
            Assert.False(simpleFormatter.FormatterOptions.UseUtcTimestamp);
            Assert.True(simpleFormatter.FormatterOptions.IncludeScopes);
        }

        public class TestSimpleFormatterOptionsMonitor : IOptionsMonitor<SimpleConsoleFormatterOptions>
        {
            private SimpleConsoleFormatterOptions _options;
            private event Action<SimpleConsoleFormatterOptions, string> _onChange;

            public TestSimpleFormatterOptionsMonitor(SimpleConsoleFormatterOptions options)
            {
                _options = options;
            }

            public SimpleConsoleFormatterOptions Get(string name) => _options;

            public IDisposable OnChange(Action<SimpleConsoleFormatterOptions, string> listener)
            {
                _onChange += listener;
                return null;
            }

            public SimpleConsoleFormatterOptions CurrentValue => _options;

            public void Set(SimpleConsoleFormatterOptions options)
            {
                _options = options;
                _onChange?.Invoke(options, "");
            }
        }
    }
}
