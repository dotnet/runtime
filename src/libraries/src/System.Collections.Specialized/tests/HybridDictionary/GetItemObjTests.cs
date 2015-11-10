// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class GetItemObjTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        private string[] TestValues(int length)
        {
            string[] values = new string[length];
            for (int i = 0; i < length; i++)
                values[i] = "Item" + i;
            return values;
        }

        private string[] TestKeys(int length)
        {
            string[] keys = new string[length];
            for (int i = 0; i < length; i++)
                keys[i] = "keY" + i;
            return keys;
        }

        private string[] Test_EdgeCases()
        {
            return new string[]
            {
                "",
                " ",
                "$%^#",
                System.DateTime.Today.ToString(),
                Int32.MaxValue.ToString()
            };
        }

        [Theory]
        [InlineData(50)]
        [InlineData(100)]
        public void Add(int size)
        {
            string[] values = TestValues(size);
            string[] keys = TestKeys(size);

            HybridDictionary hd = new HybridDictionary(true);
            for (int i = 0; i < size; i++)
            {
                hd.Add(keys[i], values[i]);
                Assert.Equal(hd[keys[i].ToLower()], values[i]);
                Assert.Equal(hd[keys[i].ToUpper()], values[i]);
            }
        }

        [Fact]
        public void Add_EdgeCases()
        {
            string[] values = Test_EdgeCases();
            string[] keys = Test_EdgeCases();

            HybridDictionary hd = new HybridDictionary(true);
            for (int i = 0; i < values.Length; i++)
            {
                hd.Add(keys[i], values[i]);
                Assert.Equal(hd[keys[i].ToLower()], values[i]);
                Assert.Equal(hd[keys[i].ToUpper()], values[i]);
            }
        }


        [Fact]
        public void Test01()
        {
            IntlStrings intl;

            HybridDictionary hd;


            // simple string values
            string[] valuesShort = Test_EdgeCases();

            // keys for simple string values
            string[] keysShort = Test_EdgeCases();

            string[] valuesLong = TestValues(100);
            string[] keysLong = TestKeys(100);

            int cnt = 0;            // Count
            Object itm;         // Item

            // initialize IntStrings
            intl = new IntlStrings();

            // [] HybridDictionary is constructed as expected
            //-----------------------------------------------------------------

            hd = new HybridDictionary();

            // [] get Item() on empty dictionary
            //
            cnt = hd.Count;
            Assert.Throws<ArgumentNullException>(() => { itm = hd[null]; });

            cnt = hd.Count;
            itm = hd["some_string"];
            if (itm != null)
            {
                Assert.False(true, string.Format("Error, returned non-null for Item(some_string)"));
            }

            cnt = hd.Count;
            itm = hd[new Hashtable()];
            if (itm != null)
            {
                Assert.False(true, string.Format("Error, returned non-null for Item(some_object)"));
            }

            // [] get Item() on short dictionary with simple strings
            //

            cnt = hd.Count;
            int len = valuesShort.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, valuesShort.Length));
            }
            //

            for (int i = 0; i < len; i++)
            {
                cnt = hd.Count;
                itm = hd[keysShort[i]];
                if (String.Compare(itm.ToString(), valuesShort[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong item", i));
                }
            }

            // [] get Item() on long dictionary with simple strings
            //
            hd.Clear();
            cnt = hd.Count;
            len = valuesLong.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }
            //

            for (int i = 0; i < len; i++)
            {
                cnt = hd.Count;
                itm = hd[keysLong[i]];
                if (String.Compare(itm.ToString(), valuesLong[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong item", i));
                }
            }


            // [] get Item() on long dictionary with Intl strings
            // Intl strings
            //

            len = valuesLong.Length;
            string[] intlValues = new string[len * 2];

            // fill array with unique strings
            //
            for (int i = 0; i < len * 2; i++)
            {
                string val = intl.GetRandomString(MAX_LEN);
                while (Array.IndexOf(intlValues, val) != -1)
                    val = intl.GetRandomString(MAX_LEN);
                intlValues[i] = val;
            }


            cnt = hd.Count;
            for (int i = 0; i < len; i++)
            {
                hd.Add(intlValues[i + len], intlValues[i]);
            }
            if (hd.Count != (cnt + len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, cnt + len));
            }

            for (int i = 0; i < len; i++)
            {
                //
                cnt = hd.Count;
                itm = hd[intlValues[i + len]];
                if (string.Compare(itm.ToString(), intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong item", i));
                }
            }

            // [] get Item() on short dictionary with Intl strings
            //

            len = valuesShort.Length;

            hd.Clear();
            cnt = hd.Count;
            for (int i = 0; i < len; i++)
            {
                hd.Add(intlValues[i + len], intlValues[i]);
            }
            if (hd.Count != (cnt + len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, cnt + len));
            }

            for (int i = 0; i < len; i++)
            {
                //
                cnt = hd.Count;
                itm = hd[intlValues[i + len]];
                if (String.Compare(itm.ToString(), intlValues[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong item", i));
                }
            }


            //
            // [] Case sensitivity - hashtable
            //

            len = valuesLong.Length;

            hd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToUpper(), valuesLong[i].ToUpper());     // adding uppercase strings
            }

            //
            for (int i = 0; i < len; i++)
            {
                // uppercase key
                itm = hd[keysLong[i].ToUpper()];
                if (String.Compare(itm.ToString(), valuesLong[i].ToUpper()) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong item", i));
                }
                if (String.Compare(itm.ToString(), valuesLong[i].ToLower()) == 0)
                {
                    Assert.False(true, string.Format("Error, returned lowercase when added uppercase", i));
                }
            }

            hd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToUpper(), valuesLong[i].ToUpper());     // adding uppercase strings
            }

            //  LD is case-sensitive by default
            for (int i = 0; i < len; i++)
            {
                // lowercase key
                itm = hd[keysLong[i].ToLower()];
                if (itm != null)
                {
                    Assert.False(true, string.Format("Error, returned non-null for lowercase key", i));
                }
            }

            //
            // [] Case sensitivity - list
            //


            len = valuesShort.Length;

            hd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToUpper(), valuesLong[i].ToUpper());     // adding uppercase strings
            }

            //
            for (int i = 0; i < len; i++)
            {
                // uppercase key
                itm = hd[keysLong[i].ToUpper()];
                if (String.Compare(itm.ToString(), valuesLong[i].ToUpper()) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong item", i));
                }
                if (String.Compare(itm.ToString(), valuesLong[i].ToLower()) == 0)
                {
                    Assert.False(true, string.Format("Error, returned lowercase when added uppercase", i));
                }
            }

            hd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToUpper(), valuesLong[i].ToUpper());     // adding uppercase strings
            }

            //  LD is case-sensitive by default
            for (int i = 0; i < len; i++)
            {
                // lowercase key
                itm = hd[keysLong[i].ToLower()];
                if (itm != null)
                {
                    Assert.False(true, string.Format("Error, returned non-null for lowercase key", i));
                }
            }


            //
            // [] get Item() in case-insensitive HD - list
            //
            hd = new HybridDictionary(true);

            len = valuesShort.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToLower(), valuesLong[i].ToLower());
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                itm = hd[keysLong[i].ToUpper()];
                if (String.Compare(itm.ToString(), valuesLong[i].ToLower()) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong item for uppercase key", i));
                }
                itm = hd[keysLong[i].ToLower()];
                if (String.Compare(itm.ToString(), valuesLong[i].ToLower()) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong item - for lowercase key", i));
                }
            }

            // [] get Item() in case-insensitive HD - hashtable
            //
            hd = new HybridDictionary(true);

            len = valuesLong.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i].ToLower(), valuesLong[i].ToLower());
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                itm = hd[keysLong[i].ToUpper()];
                if (String.Compare(itm.ToString(), valuesLong[i].ToLower()) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong item for uppercase key", i));
                }
                itm = hd[keysLong[i].ToLower()];
                if (String.Compare(itm.ToString(), valuesLong[i].ToLower()) != 0)
                {
                    Assert.False(true, string.Format("Error, returned wrong item for lowercase key", i));
                }
            }


            //
            //   [] get Item(null) on filled HD - list
            //
            hd = new HybridDictionary();
            len = valuesShort.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }

            Assert.Throws<ArgumentNullException>(() => { itm = hd[null]; });

            //   [] get Item(null) on filled HD - hashtable
            //
            hd = new HybridDictionary();
            len = valuesLong.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }

            Assert.Throws<ArgumentNullException>(() => { itm = hd[null]; });

            //
            //   [] get Item(special_object) on filled HD - list
            //
            hd = new HybridDictionary();

            hd.Clear();
            len = 2;
            ArrayList[] b = new ArrayList[len];
            Hashtable[] lbl = new Hashtable[len];
            for (int i = 0; i < len; i++)
            {
                lbl[i] = new Hashtable();
                b[i] = new ArrayList();
                hd.Add(lbl[i], b[i]);
            }

            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                itm = hd[lbl[i]];
                if (!itm.Equals(b[i]))
                {
                    Assert.False(true, string.Format("Error, returned wrong special object"));
                }
            }

            //   [] get Item(special_object) on filled HD - hashtable
            //
            hd = new HybridDictionary();

            hd.Clear();
            len = 40;
            b = new ArrayList[len];
            lbl = new Hashtable[len];
            for (int i = 0; i < len; i++)
            {
                lbl[i] = new Hashtable();
                b[i] = new ArrayList();
                hd.Add(lbl[i], b[i]);
            }

            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                itm = hd[lbl[i]];
                if (!itm.Equals(b[i]))
                {
                    Assert.False(true, string.Format("Error, returned wrong special object"));
                }
            }
        }
    }
}
