// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests the serializer (de)serializes collections appropriately,
    /// and that the collection converter factory is linker-safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";
            object obj = JsonSerializer.Deserialize(json, typeof(int[])); // ArrayConverter
            if (!AssertCollectionAndSerialize<int[]>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(ConcurrentQueue<int>)); // ConcurrentQueueOfTConverter
            if (!AssertCollectionAndSerialize<ConcurrentQueue<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(ConcurrentStack<int>)); // ConcurrentStackOfTConverter
            if (!AssertCollectionAndSerialize<ConcurrentStack<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(LinkedList<int>)); // ICollectionOfTConverter
            if (!AssertCollectionAndSerialize<LinkedList<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(IEnumerable)); // IEnumerableConverter
            if (!AssertCollectionAndSerialize<IEnumerable>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(IEnumerable<int>)); // IEnumerableOfTConverter
            if (!AssertCollectionAndSerialize<IEnumerable<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(Queue)); // IEnumerableWithAddMethodConverter
            if (!AssertCollectionAndSerialize<Queue>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(ArrayList)); // IListConverter
            if (!AssertCollectionAndSerialize<ArrayList>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(Collection<int>)); // IListOfTConverter
            if (!AssertCollectionAndSerialize<Collection<int>>(obj, json))
            {
                return -1;
            }

            // TODO: instantiate this with the serializer - https://github.com/dotnet/runtime/issues/38593.
            obj = ImmutableList.CreateRange(new[] { 1 });
            if (!(AssertCollectionAndSerialize<ImmutableList<int>>(obj, json))) // ImmutableEnumerableOfTConverter
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(HashSet<int>)); // ISetOfTConverter
            if (!AssertCollectionAndSerialize<HashSet<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(List<int>)); // ListOfTConverter
            if (!AssertCollectionAndSerialize<List<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(Queue<int>)); // QueueOfTConverter
            if (!AssertCollectionAndSerialize<Queue<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(Stack<int>)); // StackOfTConverter
            if (!AssertCollectionAndSerialize<Stack<int>>(obj, json))
            {
                return -1;
            }

            json = @"{""Key"":1}";
            obj = JsonSerializer.Deserialize(json, typeof(Dictionary<string, int>)); // DictionaryOfTKeyTValueConverter
            if (!AssertCollectionAndSerialize<Dictionary<string, int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(Hashtable)); // IDictionaryConverter
            if (!AssertCollectionAndSerialize<Hashtable>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(ConcurrentDictionary<string, int>)); // IDictionaryOfTKeyTValueConverter
            if (!AssertCollectionAndSerialize<ConcurrentDictionary<string, int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(IDictionary<string, int>)); // IDictionaryOfTKeyTValueConverter
            if (!AssertCollectionAndSerialize<IDictionary<string, int>>(obj, json))
            {
                return -1;
            }

            // TODO: instantiate this with the serializer - https://github.com/dotnet/runtime/issues/38593.
            obj = ImmutableDictionary.CreateRange(new Dictionary<string, int> { ["Key"] = 1 });
            if (!(AssertCollectionAndSerialize<ImmutableDictionary<string, int>>(obj, json))) // ImmutableDictionaryTKeyTValueOfTConverter
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(IReadOnlyDictionary<string, int>)); // IReadOnlyDictionaryOfTKeyTValueConverter
            if (!AssertCollectionAndSerialize<IReadOnlyDictionary<string, int>>(obj, json))
            {
                return -1;
            }

            return 100;
        }

        private static bool AssertCollectionAndSerialize<T>(object obj, string json)
        {
            return obj is T && JsonSerializer.Serialize(obj) == json;
        }
    }
}
