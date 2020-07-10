// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Logging.Console.Test
{
    public class JsonConsoleFormatterOptionsTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void JsonConsoleFormatterOptions_ChangeProperties_ChangesReflected()
        {
            // Arrange
            var formatterMonitor = new TestJsonFormatterOptionsMonitor(new JsonConsoleFormatterOptions()
            {
                JsonWriterOptions = new JsonWriterOptions() {
                    Indented = true,
                },
                TimestampFormat = "HH:mm ",
                UseUtcTimestamp = true,
                IncludeScopes = false
            });
            var jsonFormatter = new JsonConsoleFormatter(formatterMonitor);

            // Act & Assert
            Assert.True(jsonFormatter.FormatterOptions.JsonWriterOptions.Indented);
            Assert.Equal("HH:mm ", jsonFormatter.FormatterOptions.TimestampFormat);
            Assert.True(jsonFormatter.FormatterOptions.UseUtcTimestamp);
            Assert.False(jsonFormatter.FormatterOptions.IncludeScopes);
            formatterMonitor.Set(new JsonConsoleFormatterOptions()
            {
                JsonWriterOptions = new JsonWriterOptions() {
                    Indented = false,
                },
                TimestampFormat = "HH:mm:ss ",
                UseUtcTimestamp = false,
                IncludeScopes = true
            });
            Assert.False(jsonFormatter.FormatterOptions.JsonWriterOptions.Indented);
            Assert.Equal("HH:mm:ss ", jsonFormatter.FormatterOptions.TimestampFormat);
            Assert.False(jsonFormatter.FormatterOptions.UseUtcTimestamp);
            Assert.True(jsonFormatter.FormatterOptions.IncludeScopes);
        }

        public class TestJsonFormatterOptionsMonitor : IOptionsMonitor<JsonConsoleFormatterOptions>
        {
            private JsonConsoleFormatterOptions _options;
            private event Action<JsonConsoleFormatterOptions, string> _onChange;

            public TestJsonFormatterOptionsMonitor(JsonConsoleFormatterOptions options)
            {
                _options = options;
            }

            public JsonConsoleFormatterOptions Get(string name) => _options;

            public IDisposable OnChange(Action<JsonConsoleFormatterOptions, string> listener)
            {
                _onChange += listener;
                return null;
            }

            public JsonConsoleFormatterOptions CurrentValue => _options;

            public void Set(JsonConsoleFormatterOptions options)
            {
                _options = options;
                _onChange?.Invoke(options, "");
            }
        }
    }
}
