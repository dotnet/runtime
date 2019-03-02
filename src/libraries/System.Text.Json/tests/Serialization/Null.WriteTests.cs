// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class NullTests
    {
        [Fact]
        public static void DefaultWriteOptions()
        {
            var input = new TestClassWithNull();
            string json = JsonSerializer.ToString(input);
            Assert.Equal(@"{""MyString"":null}", json);
        }

        [Fact]
        public static void OverrideWriteOnOption()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreNullPropertyValueOnWrite = true;

            var input = new TestClassWithNull();
            string json = JsonSerializer.ToString(input, options);
            Assert.Equal(@"{}", json);
        }
    }
}
