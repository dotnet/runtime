// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SerializerTrimmingTest
{
    /// <summary>
    /// Tests that the collection converter factory is linker-safe.
    /// </summary>
    internal class Program
    {
        static int Main(string[] args)
        {
            string json = "[1]";
            object obj = JsonSerializer.Deserialize(json, typeof(int[])); // ArrayConverter
            if (!TestHelper.AssertCollectionAndSerialize<int[]>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(ConcurrentQueue<int>)); // ConcurrentQueueOfTConverter
            if (!TestHelper.AssertCollectionAndSerialize<ConcurrentQueue<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(ConcurrentStack<int>)); // ConcurrentStackOfTConverter
            if (!TestHelper.AssertCollectionAndSerialize<ConcurrentStack<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(LinkedList<int>)); // ICollectionOfTConverter
            if (!TestHelper.AssertCollectionAndSerialize<LinkedList<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(IEnumerable)); // IEnumerableConverter
            if (!TestHelper.AssertCollectionAndSerialize<IEnumerable>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(IEnumerable<int>)); // IEnumerableOfTConverter
            if (!TestHelper.AssertCollectionAndSerialize<IEnumerable<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(Queue)); // IEnumerableWithAddMethodConverter
            if (!TestHelper.AssertCollectionAndSerialize<Queue>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(ArrayList)); // IListConverter
            if (!TestHelper.AssertCollectionAndSerialize<ArrayList>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(Collection<int>)); // IListOfTConverter
            if (!TestHelper.AssertCollectionAndSerialize<Collection<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(HashSet<int>)); // ISetOfTConverter
            if (!TestHelper.AssertCollectionAndSerialize<HashSet<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(List<int>)); // ListOfTConverter
            if (!TestHelper.AssertCollectionAndSerialize<List<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(Queue<int>)); // QueueOfTConverter
            if (!TestHelper.AssertCollectionAndSerialize<Queue<int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(Stack<int>)); // StackOfTConverter
            if (!TestHelper.AssertCollectionAndSerialize<Stack<int>>(obj, json))
            {
                return -1;
            }

            json = @"{""Key"":1}";
            obj = JsonSerializer.Deserialize(json, typeof(Dictionary<string, int>)); // DictionaryOfTKeyTValueConverter
            if (!TestHelper.AssertCollectionAndSerialize<Dictionary<string, int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(Hashtable)); // IDictionaryConverter
            if (!TestHelper.AssertCollectionAndSerialize<Hashtable>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(ConcurrentDictionary<string, int>)); // IDictionaryOfTKeyTValueConverter
            if (!TestHelper.AssertCollectionAndSerialize<ConcurrentDictionary<string, int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(IDictionary<string, int>)); // IDictionaryOfTKeyTValueConverter
            if (!TestHelper.AssertCollectionAndSerialize<IDictionary<string, int>>(obj, json))
            {
                return -1;
            }

            obj = JsonSerializer.Deserialize(json, typeof(IReadOnlyDictionary<string, int>)); // IReadOnlyDictionaryOfTKeyTValueConverter
            if (!TestHelper.AssertCollectionAndSerialize<IReadOnlyDictionary<string, int>>(obj, json))
            {
                return -1;
            }

            return 100;
        }
    }
}
