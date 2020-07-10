// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class ConsoleFormatterOptionsTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void ConsoleFormatterOptions_ChangeProperties_ChangesReflected()
        {
            // Arrange
            var formatterMonitor = new TestFormatterOptionsMonitor(new ConsoleFormatterOptions()
            { 
                TimestampFormat = "HH:mm ",
                UseUtcTimestamp = true,
                IncludeScopes = false
            });
            var systemdFormatter = new SystemdConsoleFormatter(formatterMonitor);

            // Act & Assert
            Assert.Equal("HH:mm ", systemdFormatter.FormatterOptions.TimestampFormat);
            Assert.True(systemdFormatter.FormatterOptions.UseUtcTimestamp);
            Assert.False(systemdFormatter.FormatterOptions.IncludeScopes);
            formatterMonitor.Set(new ConsoleFormatterOptions()
            {
                TimestampFormat = "HH:mm:ss ",
                UseUtcTimestamp = false,
                IncludeScopes = true
            });
            Assert.Equal("HH:mm:ss ", systemdFormatter.FormatterOptions.TimestampFormat);
            Assert.False(systemdFormatter.FormatterOptions.UseUtcTimestamp);
            Assert.True(systemdFormatter.FormatterOptions.IncludeScopes);
        }

        public class TestFormatterOptionsMonitor : IOptionsMonitor<ConsoleFormatterOptions>
        {
            private ConsoleFormatterOptions _options;
            private event Action<ConsoleFormatterOptions, string> _onChange;

            public TestFormatterOptionsMonitor(ConsoleFormatterOptions options)
            {
                _options = options;
            }

            public ConsoleFormatterOptions Get(string name) => _options;

            public IDisposable OnChange(Action<ConsoleFormatterOptions, string> listener)
            {
                _onChange += listener;
                return null;
            }

            public ConsoleFormatterOptions CurrentValue => _options;

            public void Set(ConsoleFormatterOptions options)
            {
                _options = options;
                _onChange?.Invoke(options, "");
            }
        }
    }
}
