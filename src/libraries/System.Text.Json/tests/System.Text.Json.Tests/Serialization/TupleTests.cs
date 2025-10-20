// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class TupleTests
    {
        [Fact]
        public static void ValueTupleSerializesWithoutIncludeFields()
        {
            // Test that ValueTuple serializes without requiring IncludeFields = true
            var tuple = (Label1: "string", Label2: 42, true);
            string json = JsonSerializer.Serialize(tuple);
            Assert.Equal("{\"Item1\":\"string\",\"Item2\":42,\"Item3\":true}", json);
        }

        [Fact]
        public static void ValueTupleDiscardsLabels()
        {
            // Test that C# labels are discarded and standard Item naming is used
            var tuple = (Name: "John", Age: 30, Active: true);
            string json = JsonSerializer.Serialize(tuple);
            Assert.Equal("{\"Item1\":\"John\",\"Item2\":30,\"Item3\":true}", json);
            Assert.DoesNotContain("Name", json);
            Assert.DoesNotContain("Age", json);
            Assert.DoesNotContain("Active", json);
        }

        [Fact]
        public static void ValueTupleLongTupleFlattensRest()
        {
            // Test 8-element tuple (boundary case)
            var tuple8 = (1, 2, 3, 4, 5, 6, 7, 8);
            string json8 = JsonSerializer.Serialize(tuple8);
            Assert.Equal("{\"Item1\":1,\"Item2\":2,\"Item3\":3,\"Item4\":4,\"Item5\":5,\"Item6\":6,\"Item7\":7,\"Item8\":8}", json8);

            // Test 10-element tuple
            var tuple10 = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
            string json10 = JsonSerializer.Serialize(tuple10);
            Assert.Equal("{\"Item1\":1,\"Item2\":2,\"Item3\":3,\"Item4\":4,\"Item5\":5,\"Item6\":6,\"Item7\":7,\"Item8\":8,\"Item9\":9,\"Item10\":10}", json10);

            // Test 16-element tuple (nested Rest)
            var tuple16 = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            string json16 = JsonSerializer.Serialize(tuple16);
            Assert.Equal("{\"Item1\":1,\"Item2\":2,\"Item3\":3,\"Item4\":4,\"Item5\":5,\"Item6\":6,\"Item7\":7,\"Item8\":8,\"Item9\":9,\"Item10\":10,\"Item11\":11,\"Item12\":12,\"Item13\":13,\"Item14\":14,\"Item15\":15,\"Item16\":16}", json16);
        }

        [Fact]
        public static void SystemTupleSerializesWithoutIncludeFields()
        {
            // Test that System.Tuple serializes (it already has properties, so this should work)
            var tuple = Tuple.Create("string", 42, true);
            string json = JsonSerializer.Serialize(tuple);
            Assert.Equal("{\"Item1\":\"string\",\"Item2\":42,\"Item3\":true}", json);
        }

        [Fact]
        public static void SystemTupleLongTupleFlattensRest()
        {
            // Test 8-element System.Tuple (boundary case)
            var tuple8 = Tuple.Create(1, 2, 3, 4, 5, 6, 7, 8);
            string json8 = JsonSerializer.Serialize(tuple8);
            Assert.Equal("{\"Item1\":1,\"Item2\":2,\"Item3\":3,\"Item4\":4,\"Item5\":5,\"Item6\":6,\"Item7\":7,\"Item8\":8}", json8);

            // Test 10-element System.Tuple
            var tuple10 = new Tuple<int, int, int, int, int, int, int, Tuple<int, int, int>>(
                1, 2, 3, 4, 5, 6, 7, new Tuple<int, int, int>(8, 9, 10));
            string json10 = JsonSerializer.Serialize(tuple10);
            Assert.Equal("{\"Item1\":1,\"Item2\":2,\"Item3\":3,\"Item4\":4,\"Item5\":5,\"Item6\":6,\"Item7\":7,\"Item8\":8,\"Item9\":9,\"Item10\":10}", json10);
        }

        [Fact]
        public static void MixedTypeTuple()
        {
            // Test tuple with mixed types
            var tuple = ("text", 123, true, 45.67, 'c');
            string json = JsonSerializer.Serialize(tuple);
            Assert.Equal("{\"Item1\":\"text\",\"Item2\":123,\"Item3\":true,\"Item4\":45.67,\"Item5\":\"c\"}", json);
        }

        [Fact]
        public static void NestedTuple()
        {
            // Test tuple containing another tuple
            var innerTuple = (1, 2);
            var outerTuple = ("outer", innerTuple);
            string json = JsonSerializer.Serialize(outerTuple);
            Assert.Equal("{\"Item1\":\"outer\",\"Item2\":{\"Item1\":1,\"Item2\":2}}", json);
        }

        [Fact]
        public static void TupleWithObject()
        {
            // Test tuple containing a complex object
            var obj = new { Name = "Test", Value = 42 };
            var tuple = ("prefix", obj, "suffix");
            string json = JsonSerializer.Serialize(tuple);
            Assert.Equal("{\"Item1\":\"prefix\",\"Item2\":{\"Name\":\"Test\",\"Value\":42},\"Item3\":\"suffix\"}", json);
        }
    }
}
