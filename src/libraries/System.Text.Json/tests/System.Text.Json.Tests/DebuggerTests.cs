// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using Xunit;

namespace System.Text.Json.Tests
{
    public class DebuggerTests
    {
        [Fact]
        public void DefaultJsonElement()
        {
            // Validating that we don't throw on default
            JsonElement element = default;
            GetDebuggerDisplayProperty(element);
        }

        [Fact]
        public void DefaultJsonProperty()
        {
            // Validating that we don't throw on default
            JsonProperty property = default;
            GetDebuggerDisplayProperty(property);
        }

        [Fact]
        public void DefaultUtf8JsonWriter()
        {
            // Validating that we don't throw on new object
            using var writer = new Utf8JsonWriter(new MemoryStream());
            GetDebuggerDisplayProperty(writer);
        }

        private static string GetDebuggerDisplayProperty<T>(T value)
        {
            return (string)typeof(T).GetProperty("DebuggerDisplay", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
        }
    }
}
