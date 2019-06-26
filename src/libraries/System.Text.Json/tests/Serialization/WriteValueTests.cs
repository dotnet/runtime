// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class WriteValueTests
    {
        [Fact]
        public static void CanWriteValueToJsonArray()
        {
            using MemoryStream memoryStream = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(memoryStream);

            writer.WriteStartObject();
            writer.WriteStartArray("test");
            JsonSerializer.Serialize<int>(writer, 1);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();

            string json = Encoding.UTF8.GetString(memoryStream.ToArray());
            Assert.Equal("{\"test\":[1]}", json);
        }
    }
}
