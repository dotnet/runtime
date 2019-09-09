// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonArrayTests
    {

        private static void TestArray<T>(T value1, T value2, Func<JsonArray, T> getter, Func<T, JsonNode> nodeCtor)
        {
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

            jsonArray.Add(value2 as dynamic);
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
                Assert.Equal(expected[i], ((JsonString)dishesJsonArray[i]).Value);
            }
        }

        [Fact]
        public static void TestCreatingJsonArrayFromIEnumerableOfStrings()
        {
            var sportsExperienceYears = new JsonObject()
            {
                { "skiing", 5 },
                { "cycling", 8 },
                { "hiking", 6 },
                { "chess", 2 },
                { "skating", 1 },
            };

            // choose only sports with > 2 experience years
            IEnumerable<string> sports = sportsExperienceYears.Where(sport => ((JsonNumber)sport.Value).GetInt32() > 2).Select(sport => sport.Key);

            var sportsJsonArray = new JsonArray(sports);
            Assert.Equal(3, sportsJsonArray.Count);

            for (int i = 0; i < sportsJsonArray.Count; i++)
            {
                Assert.Equal(sports.ElementAt(i), ((JsonString)sportsJsonArray[i]).Value);
            }
        }

        [Fact]
        public static void TestCreatingJsonArrayFromIEnumerableOfJsonNodes()
        {
            var strangeWords = new JsonArray()
            {
                "supercalifragilisticexpialidocious",
                "gladiolus",
                "albumen",
                "smaragdine"
            };

            var strangeWordsJsonArray = new JsonArray(strangeWords.Where(word => ((JsonString)word).Value.Length < 10));
            Assert.Equal(2, strangeWordsJsonArray.Count);

            string[] expected = { "gladiolus", "albumen" };

            for (int i = 0; i < strangeWordsJsonArray.Count; i++)
            {
                Assert.Equal(expected[i], ((JsonString)strangeWordsJsonArray[i]).Value);
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

            var jsonArray = (JsonArray)vertices[0];
            Assert.Equal(2, jsonArray.Count());
            jsonArray = (JsonArray)jsonArray[1];
            Assert.Equal(2, jsonArray.Count());
            jsonArray = (JsonArray)jsonArray[0];
            Assert.Equal(3, jsonArray.Count());

            Assert.Equal(0, jsonArray[0]);
            Assert.Equal(1, jsonArray[1]);
            Assert.Equal(0, jsonArray[2]);
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
        public static void TestHandlingNulls()
        {
            var jsonArray = new JsonArray() { "to be replaced" };

            jsonArray[0] = null;
            Assert.Equal(1, jsonArray.Count());
            Assert.IsType<JsonNull>(jsonArray[0]);

            jsonArray.Add(null);
            Assert.Equal(2, jsonArray.Count());
            Assert.IsType<JsonNull>(jsonArray[1]);

            jsonArray.Add(new JsonNull());
            Assert.Equal(3, jsonArray.Count());
            Assert.IsType<JsonNull>(jsonArray[2]);

            jsonArray.Insert(3, null);
            Assert.Equal(4, jsonArray.Count());
            Assert.IsType<JsonNull>(jsonArray[3]);

            jsonArray.Insert(4, new JsonNull());
            Assert.Equal(5, jsonArray.Count());
            Assert.IsType<JsonNull>(jsonArray[4]);

            Assert.True(jsonArray.Contains(null));

            Assert.Equal(0, jsonArray.IndexOf(null));
            Assert.Equal(4, jsonArray.LastIndexOf(null));

            jsonArray.Remove(null);
            Assert.Equal(4, jsonArray.Count());
        }

        [Fact]
        public static void TestAccesingNestedJsonArrayGetPropertyMethod()
        {
            var issues = new JsonObject()
            {
                { "features", new JsonArray { "new functionality 1", "new functionality 2" } },
                { "bugs", new JsonArray { "bug 123", "bug 4566", "bug 821" } },
                { "tests", new JsonArray { "code coverage" } },
            };

            issues.GetJsonArrayPropertyValue("bugs").Add("bug 12356");
            ((JsonString)issues.GetJsonArrayPropertyValue("features")[0]).Value = "feature 1569";
            ((JsonString)issues.GetJsonArrayPropertyValue("features")[1]).Value = "feature 56134";

            Assert.Equal("bug 12356", ((JsonString)((JsonArray)issues["bugs"])[3]).Value);
            Assert.Equal("feature 1569", ((JsonString)((JsonArray)issues["features"])[0]).Value);
            Assert.Equal("feature 56134", ((JsonString)((JsonArray)issues["features"])[1]).Value);
        }

        [Fact]
        public static void TestAccesingNestedJsonArrayTryGetPropertyMethod()
        {
            var issues = new JsonObject()
            {
                { "features", new JsonArray { "new functionality 1", "new functionality 2" } },
            };

            Assert.True(issues.TryGetJsonArrayPropertyValue("features", out JsonArray featuresArray));
            Assert.Equal("new functionality 1", ((JsonString)featuresArray[0]).Value);
            Assert.Equal("new functionality 2", ((JsonString)featuresArray[1]).Value);
        }

        [Fact]
        public static void TestInsert()
        {
            var jsonArray = new JsonArray() { 1 };
            Assert.Equal(1, jsonArray.Count);

            jsonArray.Insert(0, 0);

            Assert.Equal(2, jsonArray.Count);
            Assert.Equal(0, jsonArray[0]);
            Assert.Equal(1, jsonArray[1]);

            jsonArray.Insert(2, 3);

            Assert.Equal(3, jsonArray.Count);
            Assert.Equal(0, jsonArray[0]);
            Assert.Equal(1, jsonArray[1]);
            Assert.Equal(3, jsonArray[2]);

            jsonArray.Insert(2, 2);

            Assert.Equal(4, jsonArray.Count);
            Assert.Equal(0, jsonArray[0]);
            Assert.Equal(1, jsonArray[1]);
            Assert.Equal(2, jsonArray[2]);
            Assert.Equal(3, jsonArray[3]);
        }

        [Fact]
        public static void TestHeterogeneousArray()
        {
            var mixedTypesArray = new JsonArray { 1, "value", true };

            Assert.Equal(1, mixedTypesArray[0]);
            Assert.Equal("value", mixedTypesArray[1]);
            Assert.Equal(true, mixedTypesArray[2]);

            mixedTypesArray.Add(17);
            mixedTypesArray.Insert(4, "another");
            mixedTypesArray.Add(new JsonNull());

            Assert.Equal(17, mixedTypesArray[3]);
            Assert.Equal("another", mixedTypesArray[4]);
            Assert.IsType<JsonNull>(mixedTypesArray[5]);

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
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var jsonArray = new JsonArray { 1, 2, 3 };
                jsonArray.Insert(4, 17);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var jsonArray = new JsonArray { 1, 2, 3 };
                jsonArray.Insert(-1, 17);
            });
        }

        [Fact]
        public static void TestIsReadOnly()
        {
            Assert.False(new JsonArray().IsReadOnly);
        }

        [Fact]
        public static void TestClear()
        {
            var jsonArray = new JsonArray { 1, 2, 3 };

            Assert.Equal(3, jsonArray.Count);
            Assert.Equal(1, ((JsonNumber)jsonArray[0]).GetInt32());
            Assert.Equal(2, ((JsonNumber)jsonArray[1]).GetInt32());
            Assert.Equal(3, ((JsonNumber)jsonArray[2]).GetInt32());

            jsonArray.Clear();

            Assert.Equal(0, jsonArray.Count);
        }

        [Fact]
        public static void TestRemoveAll()
        {
            var jsonArray = new JsonArray { 1, 2, 3 };

            Assert.Equal(3, jsonArray.Count);
            Assert.Equal(1, ((JsonNumber)jsonArray[0]).GetInt32());
            Assert.Equal(2, ((JsonNumber)jsonArray[1]).GetInt32());
            Assert.Equal(3, ((JsonNumber)jsonArray[2]).GetInt32());

            jsonArray.RemoveAll(v => ((JsonNumber)v).GetInt32() <= 2);

            Assert.Equal(1, jsonArray.Count);
            Assert.Equal(3, ((JsonNumber)jsonArray[0]).GetInt32());
        }

        [Fact]
        public static void TestValueKind()
        {
            Assert.Equal(JsonValueKind.Array, new JsonArray().ValueKind);
        }

        [Fact]
        public static void TestJsonArrayIEnumerator()
        {
            var jsonArray = new JsonArray() { 1, "value" };

            // Test generic IEnumerator:
            IEnumerator<JsonNode> jsonArrayEnumerator = new JsonArrayEnumerator(jsonArray);

            Assert.Null(jsonArrayEnumerator.Current);

            jsonArrayEnumerator.MoveNext();
            Assert.Equal(1, jsonArrayEnumerator.Current);
            jsonArrayEnumerator.MoveNext();
            Assert.Equal("value", jsonArrayEnumerator.Current);

            jsonArrayEnumerator.Reset();

            jsonArrayEnumerator.MoveNext();
            Assert.Equal(1, jsonArrayEnumerator.Current);
            jsonArrayEnumerator.MoveNext();
            Assert.Equal("value", jsonArrayEnumerator.Current);

            // Test non-generic IEnumerator:
            IEnumerator jsonArrayEnumerator2 = new JsonArrayEnumerator(jsonArray);

            Assert.Null(jsonArrayEnumerator2.Current);

            jsonArrayEnumerator2.MoveNext();
            Assert.Equal((JsonNumber)1, jsonArrayEnumerator2.Current);
            jsonArrayEnumerator2.MoveNext();
            Assert.Equal((JsonString)"value", jsonArrayEnumerator2.Current);

            jsonArrayEnumerator2.Reset();

            jsonArrayEnumerator2.MoveNext();
            Assert.Equal((JsonNumber)1, jsonArrayEnumerator2.Current);
            jsonArrayEnumerator2.MoveNext();
            Assert.Equal((JsonString)"value", jsonArrayEnumerator2.Current);
        }

        [Fact]
        public static void TestGetJsonArrayIEnumerable()
        {
            IEnumerable jsonArray = new JsonArray() { 1, "value" };
            IEnumerator jsonArrayEnumerator = jsonArray.GetEnumerator();
            
            Assert.Null(jsonArrayEnumerator.Current);

            jsonArrayEnumerator.MoveNext();
            Assert.Equal((JsonNumber)1, jsonArrayEnumerator.Current);
            jsonArrayEnumerator.MoveNext();
            Assert.Equal((JsonString)"value", jsonArrayEnumerator.Current);
        }

        [Fact]
        public static void TestJsonArrayEmptyArrayEnumerator()
        {
            var jsonArray = new JsonArray();
            var jsonArrayEnumerator = new JsonArrayEnumerator(jsonArray);

            Assert.Null(jsonArrayEnumerator.Current);
            Assert.False(jsonArrayEnumerator.MoveNext());
        }
    }
}
