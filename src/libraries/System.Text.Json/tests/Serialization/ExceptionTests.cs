// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ExceptionTests
    {
        [Fact]
        public static void RootThrownFromReader()
        {
            try
            {
                int i2 = JsonSerializer.Parse<int>("12bad");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal(0, e.LineNumber);
                Assert.Equal(2, e.BytePositionInLine);
                Assert.Equal("[System.Int32]", e.Path);
                Assert.Contains("Path: [System.Int32] | LineNumber: 0 | BytePositionInLine: 2.", e.Message);

                // Verify Path is not repeated.
                Assert.True(e.Message.IndexOf("Path:") == e.Message.LastIndexOf("Path:"));
            }
        }

        [Fact]
        public static void ThrownFromSerializer()
        {
            try
            {
                JsonSerializer.Parse<IDictionary<string, int>>(@"{""Key"":1, ""Key"":2}");
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal(0, e.LineNumber);
                Assert.Equal(17, e.BytePositionInLine);
                Assert.Contains("LineNumber: 0 | BytePositionInLine: 17.", e.Message);
                Assert.Contains("[System.Collections.Generic.IDictionary`2", e.Path);

                // Verify Path is not repeated.
                Assert.True(e.Message.IndexOf("Path:") == e.Message.LastIndexOf("Path:"));
            }
        }

        [Fact]
        public static void ThrownFromReader()
        {
            string json = Encoding.UTF8.GetString(BasicCompany.s_data);

            json = json.Replace(@"""zip"" : 98052", @"""zip"" : bad");

            try
            {
                JsonSerializer.Parse<BasicCompany>(json);
                Assert.True(false, "Expected JsonException was not thrown.");
            }
            catch (JsonException e)
            {
                Assert.Equal(18, e.LineNumber);
                Assert.Equal(8, e.BytePositionInLine);
                Assert.Equal("[System.Text.Json.Serialization.Tests.BasicCompany].mainSite.zip", e.Path);
                Assert.Contains("Path: [System.Text.Json.Serialization.Tests.BasicCompany].mainSite.zip | LineNumber: 18 | BytePositionInLine: 8.",
                    e.Message);

                // Verify Path is not repeated.
                Assert.True(e.Message.IndexOf("Path:") == e.Message.LastIndexOf("Path:"));

                Assert.NotNull(e.InnerException);
                JsonException inner = (JsonException)e.InnerException;
                Assert.Equal(18, inner.LineNumber);
                Assert.Equal(8, inner.BytePositionInLine);
            }
        }
    }
}
