// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class CopyToTests
    {
        private string _strErr = "Error";

        [Fact]
        public void Test01()
        {
            MyNameObjectCollection noc = new MyNameObjectCollection();
            Array array = null;
            ArrayList ArrayValues = new ArrayList();
            Random rand = new Random(-55);
            int n = 0;
            String key1 = "key1";
            String key2 = "key2";
            Foo val1 = new Foo();
            Foo val2 = new Foo();

            // [] Copy a collection to middle of target array.

            // Set up initial collection
            n = rand.Next(10, 1000);
            for (int i = 0; i < n; i++)
            {
                noc.Add("key_" + i.ToString(), new Foo());
            }

            // Set up initial array
            n = noc.Count + rand.Next(20, 100);
            array = Array.CreateInstance(typeof(String), n);
            ArrayValues.Clear();
            for (int i = 0; i < n; i++)
            {
                String v = "arrayvalue_" + i.ToString();
                array.SetValue(v, i);
                ArrayValues.Add(v);
            }

            // Copy the collection
            int offset = 10;
            ((ICollection)noc).CopyTo(array, offset);
            for (int i = 0; i < noc.Count; i++)
            {
                ArrayValues[i + offset] = noc.GetKey(i);
            }

            // Check array
            CheckArray(array, ArrayValues);

            // [] Verify copy is distinct from original collection.

            // Clear initial collection
            noc.Clear();

            // Verify copy is not cleared
            CheckArray(array, ArrayValues);

            // [] Fill whole array (index=0)

            // Set up initial collection
            noc = new MyNameObjectCollection();
            n = rand.Next(10, 1000);
            for (int i = 0; i < n; i++)
            {
                noc.Add("key_" + i.ToString(), new Foo());
            }

            // Set up initial array
            n = noc.Count;
            array = Array.CreateInstance(typeof(String), n);
            ArrayValues.Clear();
            for (int i = 0; i < n; i++)
            {
                String v = "arrayvalue_" + i.ToString();
                array.SetValue(v, i);
                ArrayValues.Add(v);
            }

            // Copy the collection
            ((ICollection)noc).CopyTo(array, 0);
            for (int i = 0; i < noc.Count; i++)
            {
                ArrayValues[i] = noc.GetKey(i);
            }

            // Check array
            CheckArray(array, ArrayValues);


            // [] index = max index in array

            // Set up initial collection
            noc.Clear();
            noc.Add("key1", new Foo());

            // Set up initial array
            n = noc.Count + rand.Next(1, 100);
            array = Array.CreateInstance(typeof(String), n);
            ArrayValues.Clear();
            for (int i = 0; i < n; i++)
            {
                String v = "arrayvalue_" + i.ToString();
                array.SetValue(v, i);
                ArrayValues.Add(v);
            }

            // Copy the collection
            offset = ArrayValues.Count - 1;
            ((ICollection)noc).CopyTo(array, offset);
            ArrayValues[offset] = noc.GetKey(0);

            // Retrieve values
            CheckArray(array, ArrayValues);
            // [] Target array is zero-length.

            array = Array.CreateInstance(typeof(String), 0);
            noc = new MyNameObjectCollection();
            noc.Add(key1, val1);
            noc.Add(key2, val2);
            Assert.Throws<ArgumentException>(() => { ((ICollection)noc).CopyTo(array, 0); });

            // [] Call on an empty collection (to zero-length array).

            noc = new MyNameObjectCollection();
            array = Array.CreateInstance(typeof(String), 0);

            // Copy the collection
            ((ICollection)noc).CopyTo(array, 0);
            // [] Call on an empty collection.

            noc = new MyNameObjectCollection();
            array = Array.CreateInstance(typeof(String), 16);

            // Copy the collection
            ((ICollection)noc).CopyTo(array, 0);

            // Retrieve elements
            foreach (String v in array)
            {
                if (v != null)
                {
                    Assert.False(true, _strErr + "Value is incorrect.  array should be null");
                }
            }

            // [] Call with array = null

            noc = new MyNameObjectCollection();
            Assert.Throws<ArgumentNullException>(() => { ((ICollection)noc).CopyTo(null, 0); });

            // [] Target array is multidimensional.

            array = new string[20, 2];
            noc = new MyNameObjectCollection();
            noc.Add(key1, val1);
            noc.Add(key2, val2);
            Assert.Throws<ArgumentException>(() => { ((ICollection)noc).CopyTo(array, 16); });


            // [] Target array is of incompatible type.
            array = Array.CreateInstance(typeof(Foo), 10);
            noc = new MyNameObjectCollection();
            noc.Add(key1, val1);
            noc.Add(key2, val2);
            Assert.Throws<InvalidCastException>(() => { ((ICollection)noc).CopyTo(array, 1); });

            // [] index = array length
            n = rand.Next(10, 100);
            array = Array.CreateInstance(typeof(String), n);
            noc = new MyNameObjectCollection();
            noc.Add(key1, val1);
            noc.Add(key2, val2);
            Assert.Throws<ArgumentException>(() => { ((ICollection)noc).CopyTo(array, n); });

            // [] index > array length
            n = rand.Next(10, 100);
            array = Array.CreateInstance(typeof(String), n);
            noc = new MyNameObjectCollection();
            noc.Add(key1, val1);
            noc.Add(key2, val2);
            Assert.Throws<ArgumentException>(() => { ((ICollection)noc).CopyTo(array, n + 1); });

            // [] index = Int32.MaxValue

            array = Array.CreateInstance(typeof(String), 10);
            noc = new MyNameObjectCollection();
            noc.Add(key1, val1);
            noc.Add(key2, val2);
            Assert.Throws<ArgumentException>(() => { ((ICollection)noc).CopyTo(array, Int32.MaxValue); });

            // [] index < 0
            array = Array.CreateInstance(typeof(String), 10);
            noc = new MyNameObjectCollection();
            noc.Add(key1, val1);
            noc.Add(key2, val2);
            Assert.Throws<ArgumentOutOfRangeException>(() => { ((ICollection)noc).CopyTo(array, -1); });

            // [] index is valid but collection doesn't fit in available space
            noc = new MyNameObjectCollection();
            // Set up initial collection
            n = rand.Next(10, 1000);
            for (int i = 0; i < n; i++)
            {
                noc.Add("key_" + i.ToString(), new Foo());
            }

            // Set up initial array
            n = noc.Count + 20;
            array = Array.CreateInstance(typeof(Foo), n);

            Assert.Throws<ArgumentException>(() => { ((ICollection)noc).CopyTo(array, 30); });  // array is only 20 bigger than collection

            // [] index is negative
            noc = new MyNameObjectCollection();
            // Set up initial collection
            n = rand.Next(10, 1000);
            for (int i = 0; i < n; i++)
            {
                noc.Add("key_" + i.ToString(), new Foo());
            }

            // Set up initial array
            n = noc.Count + 20;
            array = Array.CreateInstance(typeof(Foo), n);

            Assert.Throws<ArgumentOutOfRangeException>(() => { ((ICollection)noc).CopyTo(array, -1); });
        }

        void CheckArray(Array array, ArrayList expected)
        {
            if (array.Length != expected.Count)
            {
                Assert.False(true, string.Format(_strErr + "Array length != {0}.  Length == {1}", expected.Count, array.Length));
            }
            for (int i = 0; i < expected.Count; i++)
            {
                if (((String[])array)[i] != (String)expected[i])
                {
                    Assert.False(true, string.Format("Value {0} is incorrect.  array[{0}]={1}, should be {2}", i, ((String[])array)[i], (String)expected[i]));
                }
            }
        }
    }
}


