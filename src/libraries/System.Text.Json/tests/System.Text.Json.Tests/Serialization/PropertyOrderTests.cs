// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class PropertyOrderTests
    {
        private class MyPoco_BeforeAndAfter
        {
            public int B { get; set; }

            [JsonPropertyOrder(1)]
            public int A { get; set; }

            [JsonPropertyOrder(-1)]
            public int C { get; set; }
        }

        [Fact]
        public static void BeforeAndAfterDefaultOrder()
        {
            string json = JsonSerializer.Serialize<MyPoco_BeforeAndAfter>(new MyPoco_BeforeAndAfter());
            Assert.Equal("{\"C\":0,\"B\":0,\"A\":0}", json);
        }

        private class MyPoco_After
        {
            [JsonPropertyOrder(2)]
            public int C { get; set; }

            public int B { get; set; }
            public int D { get; set; }

            [JsonPropertyOrder(1)]
            public int A { get; set; }
        }

        [Fact]
        public static void AfterDefaultOrder()
        {
            string json = JsonSerializer.Serialize<MyPoco_After>(new MyPoco_After());
            Assert.EndsWith("\"A\":0,\"C\":0}", json);
            // Order of B and D are not defined except they come before A and C
        }

        private class MyPoco_Before
        {
            [JsonPropertyOrder(-1)]
            public int C { get; set; }

            public int B { get; set; }
            public int D { get; set; }

            [JsonPropertyOrder(-2)]
            public int A { get; set; }
        }

        [Fact]
        public static void BeforeDefaultOrder()
        {
            string json = JsonSerializer.Serialize<MyPoco_Before>(new MyPoco_Before());
            Assert.StartsWith("{\"A\":0,\"C\":0", json);
            // Order of B and D are not defined except they come after A and C
        }
    }
}
