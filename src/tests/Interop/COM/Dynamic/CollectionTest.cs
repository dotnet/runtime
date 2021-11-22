// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dynamic
{
    using System;
    using System.Collections.Generic;
    using Xunit;

    internal class CollectionTest
    {
        private dynamic obj;
        private Random rand;

        public CollectionTest(int seed = 123)
        {
            Type t = Type.GetTypeFromCLSID(Guid.Parse(ServerGuids.CollectionTest));
            obj = Activator.CreateInstance(t);
            rand = new Random(seed);
        }

        public void Run()
        {
            Console.WriteLine($"Running {nameof(CollectionTest)}");
            Array();
            CustomCollection();
            IndexChain();
        }

        private void Array()
        {
            int len = 5;
            int[] array = new int[len];
            int[] expected = new int[len];
            for (int i = 0; i < len; i++)
            {
                int val = rand.Next(int.MaxValue - 1);
                array[i] = val;
                expected[i] = val + 1;
            }

            // Call method returning array
            AssertExtensions.CollectionEqual(expected, obj.Array_PlusOne_Ret(array));

            // Call method with array in/out
            int[] inout = new int[len];
            System.Array.Copy(array, inout, len);
            obj.Array_PlusOne_InOut(ref inout);
            AssertExtensions.CollectionEqual(expected, inout);

            // Call method returning array as variant
            AssertExtensions.CollectionEqual(expected, obj.ArrayVariant_PlusOne_Ret(array));

            // Call method with array as variant in/out
            inout = new int[len];
            System.Array.Copy(array, inout, len);
            obj.ArrayVariant_PlusOne_InOut(ref inout);
            AssertExtensions.CollectionEqual(expected, inout);
        }

        private void CustomCollection()
        {
            // Add to the collection
            Assert.Equal(0, obj.Count);
            string[] array = { "ONE", "TWO", "THREE" };
            foreach (string s in array)
            {
                obj.Add(s);
            }

            // Get item by index
            Assert.Equal(array[0], obj[0]);
            Assert.Equal(array[0], obj.Item(0));
            Assert.Equal(array[0], obj.Item[0]);
            Assert.Equal(array[1], obj[1]);
            Assert.Equal(array[1], obj.Item(1));
            Assert.Equal(array[2], obj[2]);
            Assert.Equal(array[2], obj.Item(2));
            Assert.Equal(array.Length, obj.Count);

            // Enumerate collection
            List<string> list = new List<string>();

            // Get and use enumerator directly
            System.Collections.IEnumerator enumerator = obj.GetEnumerator();
            while (enumerator.MoveNext())
            {
                list.Add((string)enumerator.Current);
            }
            AssertExtensions.CollectionEqual(array, list);

            list.Clear();
            enumerator.Reset();
            while (enumerator.MoveNext())
            {
                list.Add((string)enumerator.Current);
            }
            AssertExtensions.CollectionEqual(array, list);

            // Iterate over object that handles DISPID_NEWENUM
            list.Clear();
            foreach (string str in obj)
            {
                list.Add(str);
            }
            AssertExtensions.CollectionEqual(array, list);

            array = new string[] { "NEW_ONE", "NEW_TWO", "NEW_THREE" };
            // Update items by index
            obj[0] = array[0];
            Assert.Equal(array[0], obj[0]);
            obj[1] = array[1];
            Assert.Equal(array[1], obj[1]);
            obj[2] = array[2];
            Assert.Equal(array[2], obj[2]);
            Assert.Equal(array.Length, obj.Count);

            list.Clear();
            foreach (string str in obj)
            {
                list.Add(str);
            }
            AssertExtensions.CollectionEqual(array, list);

            // Remove item
            obj.Remove(1);
            Assert.Equal(2, obj.Count);
            Assert.Equal(array[0], obj[0]);
            Assert.Equal(array[2], obj[1]);

            // Clear collection
            obj.Clear();
            Assert.Equal(0, obj.Count);
        }

        private void IndexChain()
        {
            dynamic collection = obj.GetDispatchCollection();
            collection.Add(collection);

            Assert.Equal(1, collection.Item[0][0][0].Count);

            collection[0].Add(obj);

            Assert.Equal(2, collection.Count);
            Assert.Equal(2, collection[0].Item[1].GetDispatchCollection()[0].Count);
        }
    }
}
