// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonArrayTests
    {

        private static void TestArray<T>(T value1, T value2, Func<JsonArray, T> getter, Func<T, JsonNode> nodeCtor)
        {
            var value1Casted = value1 as dynamic;
            var value2Casted = value2 as dynamic;

            JsonNode value1Node = nodeCtor(value1);
            JsonNode value2Node = nodeCtor(value2);

            var list = new List<T>() { value1 };
            JsonArray jsonArray = new JsonArray(list as dynamic);
            Assert.Equal(1, jsonArray.Count);

            Assert.True(jsonArray.Contains(value1Node));
            Assert.Equal(value1, getter(jsonArray));

            jsonArray.Insert(0, value2 as dynamic);
            Assert.Equal(2, jsonArray.Count);
            Assert.True(jsonArray.Contains(value2Node));
            Assert.Equal(value2, getter(jsonArray));

            Assert.Equal(1, jsonArray.IndexOf(value1Node));
            Assert.Equal(1, jsonArray.LastIndexOf(value1Node));

            jsonArray.RemoveAt(0);
            Assert.False(jsonArray.Contains(value2Node));

            jsonArray.Remove(value1Node);
            Assert.False(jsonArray.Contains(value1Node));

            Assert.Equal(0, jsonArray.Count);

            jsonArray.Add(value2Casted);
            Assert.Equal(1, jsonArray.Count);
            Assert.True(jsonArray.Contains(value2Node));
            Assert.Equal(value2, getter(jsonArray));

            jsonArray[0] = value1Node;
            Assert.Equal(1, jsonArray.Count);
            Assert.True(jsonArray.Contains(value1Node));
            Assert.Equal(value1, getter(jsonArray));
        }

        [Fact]
        public static void TestStringArray()
        {
            TestArray(
                "value1", "value2",
                jsonArray => ((JsonString)jsonArray[0]).Value,
                v => new JsonString(v)
            );
        }

        [Fact]
        public static void TestBooleanArray()
        {
            TestArray(
                true, false,
                jsonArray => ((JsonBoolean)jsonArray[0]).Value,
                v => new JsonBoolean(v)
            );
        }

        [Fact]
        public static void TestByteArray()
        {
            TestArray<byte>(
                byte.MaxValue, byte.MaxValue - 1,
                jsonArray => ((JsonNumber)jsonArray[0]).GetByte(),
                v => new JsonNumber(v)
            );
        }

        [Fact]
        public static void TestInt16Array()
        {
            TestArray<short>(
                short.MaxValue, short.MaxValue - 1,
                jsonArray => ((JsonNumber)jsonArray[0]).GetInt16(),
                v => new JsonNumber(v)
            );
        }

        [Fact]
        public static void TestInt32rray()
        {
            TestArray(
                int.MaxValue, int.MaxValue - 1,
                jsonArray => ((JsonNumber)jsonArray[0]).GetInt32(),
                v => new JsonNumber(v)
            );
        }

        [Fact]
        public static void TestInt64rray()
        {
            TestArray(
                long.MaxValue, long.MaxValue - 1,
                jsonArray => ((JsonNumber)jsonArray[0]).GetInt64(),
                v => new JsonNumber(v)
            );
        }

        [Fact]
        public static void TestSingleArray()
        {
            TestArray(
                3.14f, 1.41f,
                jsonArray => ((JsonNumber)jsonArray[0]).GetSingle(),
                v => new JsonNumber(v)
            );
        }

        [Fact]
        public static void TestDoubleArray()
        {
            TestArray(
                3.14, 1.41,
                jsonArray => ((JsonNumber)jsonArray[0]).GetDouble(),
                v => new JsonNumber(v)
            );
        }

        [Fact]
        public static void TestSByteArray()
        {
            TestArray<sbyte>(
                sbyte.MaxValue, sbyte.MaxValue - 1,
                jsonArray => ((JsonNumber)jsonArray[0]).GetSByte(),
                v => new JsonNumber(v)
            );
        }

        [Fact]
        public static void TestUInt16Array()
        {
            TestArray<ushort>(
                ushort.MaxValue, ushort.MaxValue - 1,
                jsonArray => ((JsonNumber)jsonArray[0]).GetUInt16(),
                v => new JsonNumber(v)
            );
        }

        [Fact]
        public static void TestUInt32Array()
        {
            TestArray(
                uint.MaxValue, uint.MaxValue - 1,
                jsonArray => ((JsonNumber)jsonArray[0]).GetUInt32(),
                v => new JsonNumber(v)
            );
        }

        [Fact]
        public static void TestUInt64Array()
        {
            TestArray(
                ulong.MaxValue, ulong.MaxValue - 1,
                jsonArray => ((JsonNumber)jsonArray[0]).GetUInt64(),
                v => new JsonNumber(v)
            );
        }

        [Fact]
        public static void TestDecimalArray()
        {
            TestArray(
                decimal.One, decimal.Zero,
                jsonArray => ((JsonNumber)jsonArray[0]).GetDecimal(),
                v => new JsonNumber(v)
            );
        }

        [Fact]
        public static void TestCreatingJsonArrayFromStringArray()
        {
            string[] expected = { "sushi", "pasta", "cucumber soup" };

            var dishesJsonArray = new JsonArray(expected);
            Assert.Equal(3, dishesJsonArray.Count);

            for (int i = 0; i < dishesJsonArray.Count; i++)
            {
                Assert.IsType<JsonString>(dishesJsonArray[i]);
                Assert.Equal(expected[i], dishesJsonArray[i] as JsonString);
            }
        }

        [Fact]
        public static void TestCreatingNestedJsonArray()
        {
            var vertices = new JsonArray()
            {
                new JsonArray
                {
                    new JsonArray
                    {
                        new JsonArray { 0, 0, 0 },
                        new JsonArray { 0, 0, 1 }
                    },
                    new JsonArray
                    {
                        new JsonArray { 0, 1, 0 },
                        new JsonArray { 0, 1, 1 }
                    }
                },
                new JsonArray
                {
                    new JsonArray
                    {
                        new JsonArray { 1, 0, 0 },
                        new JsonArray { 1, 0, 1 }
                    },
                    new JsonArray
                    {
                        new JsonArray { 1, 1, 0 },
                        new JsonArray { 1, 1, 1 }
                    }
                },
            };

            var innerJsonArray = (JsonArray)vertices[0];
            innerJsonArray = (JsonArray)innerJsonArray[0];
            Assert.IsType<JsonArray>(innerJsonArray[0]);
        }

        [Fact]
        public static void TestCreatingJsonArrayFromCollection()
        {
            var employeesIds = new JsonArray(EmployeesDatabase.GetTenBestEmployees().Select(employee => new JsonString(employee.Key)));

            JsonString prevId = new JsonString();
            foreach (JsonNode employeeId in employeesIds)
            {
                var employeeIdString = (JsonString)employeeId;
                Assert.NotEqual(prevId, employeeIdString);
                prevId = employeeIdString;
            }
        }

        [Fact]
        public static void TestCreatingJsonArrayFromCollectionOfString()
        {
            var employeesIds = new JsonArray(EmployeesDatabase.GetTenBestEmployees().Select(employee => employee.Key));

            JsonString prevId = new JsonString();
            foreach (JsonNode employeeId in employeesIds)
            {
                var employeeIdString = (JsonString)employeeId;
                Assert.NotEqual(prevId, employeeIdString);
                prevId = employeeIdString;
            }
        }

        [Fact]
        public static void TestAddingToJsonArray()
        {
            var employeesIds = new JsonArray();

            foreach (KeyValuePair<string, JsonNode> employee in EmployeesDatabase.GetTenBestEmployees())
            {
                employeesIds.Add(employee.Key);
            }

            JsonString prevId = new JsonString();
            foreach (JsonNode employeeId in employeesIds)
            {
                var employeeIdString = (JsonString)employeeId;
                Assert.NotEqual(prevId, employeeIdString);
                prevId = employeeIdString;
            }
        }

        [Fact]
        public static void TestAccesingNestedJsonArrayGetPropertyMethod()
        {
            var issues = new JsonObject()
            {
                { "features", new JsonString [] { "new functionality 1", "new functionality 2" } },
                { "bugs", new JsonString [] { "bug 123", "bug 4566", "bug 821" } },
                { "tests", new JsonString [] { "code coverage" } },
            };

            issues.GetJsonArrayPropertyValue("bugs").Add("bug 12356");
            ((JsonString)issues.GetJsonArrayPropertyValue("features")[0]).Value = "feature 1569";
            ((JsonString)issues.GetJsonArrayPropertyValue("features")[1]).Value = "feature 56134";

            Assert.Equal((JsonString)"bug 12356", ((JsonArray)issues["bugs"])[3]);
            Assert.Equal("feature 1569", (JsonString)((JsonArray)issues["features"])[0]);
            Assert.Equal("feature 56134", (JsonString)((JsonArray)issues["features"])[1]);
        }

        [Fact]
        public static void TestAccesingNestedJsonArrayTryGetPropertyMethod()
        {
            var issues = new JsonObject()
            {
                { "features", new JsonString [] { "new functionality 1", "new functionality 2" } },
            };

            Assert.True(issues.TryGetJsonArrayPropertyValue("features", out JsonArray featuresArray));
            Assert.Equal("new functionality 1", (JsonString)featuresArray[0]);
            Assert.Equal("new functionality 2", (JsonString)featuresArray[1]);
        }

        [Fact]
        public static void TestOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonArray()[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonArray()[-1] = new JsonString());
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonArray()[0]);
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonArray()[0] = new JsonString());
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonArray()[1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonArray()[1] = new JsonString());
        }

        [Fact]
        public static void TestIsReadOnly()
        {
            Assert.False(new JsonArray().IsReadOnly);
        }

        [Fact]
        public static void TestClean()
        {
            var jsonArray = new JsonArray { 1, 2, 3 };
            
            Assert.Equal(3, jsonArray.Count);
            Assert.Equal((JsonNumber)1, jsonArray[0]);
            Assert.Equal((JsonNumber)2, jsonArray[1]);
            Assert.Equal((JsonNumber)3, jsonArray[2]);

            jsonArray.Clear();
            
            Assert.Equal(0, jsonArray.Count);
        }

        [Fact]
        public static void TestRemoveAll()
        {
            var jsonArray = new JsonArray { 1, 2, 3 };

            Assert.Equal(3, jsonArray.Count);
            Assert.Equal((JsonNumber)1, jsonArray[0]);
            Assert.Equal((JsonNumber)2, jsonArray[1]);
            Assert.Equal((JsonNumber)3, jsonArray[2]);

            jsonArray.RemoveAll(v => ((JsonNumber)v).GetInt32() <= 2);

            Assert.Equal(1, jsonArray.Count);
            Assert.Equal((JsonNumber)3, jsonArray[0]);
        }       
    }
}
