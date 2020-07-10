// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class MapTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var map = new Map();
            Assert.Equal(0, map.Count);
            Assert.Equal(0, map.Length);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(100)]
        public static void Map_Add(int numberOfElements)
        {
            var map = new Map();
            for (int i = 0; i < numberOfElements; i++)
            {
                map.Add(i, $"value{i}");
            }
            Assert.Equal(numberOfElements, map.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(1000)]
        public static void Map_AddContains(int numberOfElements)
        {
            var map = new Map();
            for (int i = 0; i < numberOfElements; i++)
            {
                map.Add(i, $"value{i}");
            }
            Assert.Equal(numberOfElements, map.Count);

            for (int i = 0; i < numberOfElements; i++)
            {
                Assert.True(map.Contains(i));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(1000)]
        public static void Map_Iterator(int numberOfElements)
        {
            var map = new Map();
            for (int i = 0; i < numberOfElements; i++)
            {
                map.Add(i, $"value{i}");
            }
            Assert.Equal(numberOfElements, map.Count);

            int d = 0;
            foreach (var value in map)
            {
                d++;
            }
            Assert.Equal(numberOfElements, d);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(1000)]
        public static void Map_IteratorKeyValue(int numberOfElements)
        {
            var map = new Map();
            for (int i = 0; i < numberOfElements; i++)
            {
                map.Add(i, $"value{i}");
            }
            Assert.Equal(numberOfElements, map.Count);

            int d = 0;
            foreach (DictionaryEntry value in map)
            {
                Assert.Equal(d, value.Key);
                Assert.Equal($"value{d++}", value.Value);
            }
            Assert.Equal(numberOfElements, d);
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        public static void Map_Contains(int numberOfElements)
        {
            var map = new Map();
            for (int i = 0; i < numberOfElements; i++)
            {
                map.Add(i, $"value{i}");
            }
            Assert.Equal(numberOfElements, map.Count);
            Assert.True(map.Contains(numberOfElements - 1));
        }

        [Fact]
        public static void Add_ClearRepeatedly()
        {
            const int Iterations = 2;
            const int Count = 2;

            var hash = new Map();
            for (int i = 0; i < Iterations; i++)
            {
                for (int j = 0; j < Count; j++)
                {
                    string key = $"Key: i={i}, j={j}";
                    string value = $"Value: i={i}, j={j}";
                    hash.Add(key, value);
                }

                Assert.Equal(Count, hash.Count);
                hash.Clear();
            }
        }

        [Fact]
        public static void ContainsKey()
        {
            var map1 = Helpers.CreateStringMap(100);
            Helpers.PerformActionOnAllMapWrappers(map1, map2 =>
            {
                for (int i = 0; i < map2.Count; i++)
                {
                    string key = $"Key_{i}";
                    Assert.True(map2.Contains(key));
                }

                Assert.False(map2.Contains("Non Existent Key"));
                Assert.False(map2.Contains(101));

                string removedKey = "Key_1";
                map2.Remove(removedKey);
                Assert.False(map2.Contains(removedKey));
            });
        }

        [Fact]
        public static void RemoveKey()
        {
            var map1 = Helpers.CreateStringMap(100);
            Helpers.PerformActionOnAllMapWrappers(map1, map2 =>
            {
                for (int i = 0; i < map2.Count; i++)
                {
                    string key = $"Key_{i}";
                    Assert.True(map2.Contains(key));
                }
                Assert.Equal(map2.Count, map2.Keys.Count);

                foreach (var key in map2.Keys)
                {
                    map2.Remove(key);
                }
                Assert.Equal(0, map2.Keys.Count);
            });
        }

        [Fact]
        public static void ContainsValue()
        {
            Map map1 = Helpers.CreateStringMap(100);
            Helpers.PerformActionOnAllMapWrappers(map1, map2 =>
            {
                for (int i = 0; i < map2.Count; i++)
                {
                    string value = $"Value_{i}";
                    Assert.True(map2[$"Key_{i}"].ToString() == value);
                }
                Assert.True(map2["Non Existent Value"] == null);
                Assert.True(map2[101] == null);
            });
        }

        private static class Helpers
        {
            public static void PerformActionOnAllMapWrappers(Map map, Action<Map> action)
            {
                action(map);
            }

            public static Map CreateStringMap(int count, int start = 0)
            {
                var map = new Map();

                for (int i = start; i < start + count; i++)
                {
                    string key = $"Key_{i}";
                    string value = $"Value_{i}";

                    map.Add(key, value);
                }

                return map;
            }
        }
    }
}
