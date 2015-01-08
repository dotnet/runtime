// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class CopyToArrayIntStringDictionaryTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            StringDictionary sd;
            // simple string values
            string[] values =
            {
                "",
                " ",
                "a",
                "aa",
                "text",
                "     spaces",
                "1",
                "$%^#",
                "2222222222222222222222222",
                System.DateTime.Today.ToString(),
                Int32.MaxValue.ToString()
            };

            // keys for simple string values
            string[] keys =
            {
                "zero",
                "one",
                " ",
                "",
                "aa",
                "1",
                System.DateTime.Today.ToString(),
                "$%^#",
                Int32.MaxValue.ToString(),
                "     spaces",
                "2222222222222222222222222"
            };

            Array destination;
            int cnt = 0;            // Count
            // initialize IntStrings
            intl = new IntlStrings();


            // [] StringDictionary is constructed as expected
            //-----------------------------------------------------------------

            sd = new StringDictionary();

            // [] Copy empty dictionary into empty array
            //
            destination = Array.CreateInstance(typeof(Object), sd.Count);
            Assert.Throws<ArgumentOutOfRangeException>(() => { sd.CopyTo(destination, -1); });
            sd.CopyTo(destination, 0);
            Assert.Throws<ArgumentException>(() => { sd.CopyTo(destination, 1); });

            // [] Copy empty dictionary into non-empty array
            //
            destination = Array.CreateInstance(typeof(Object), values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                destination.SetValue(values[i], i);
            }
            sd.CopyTo(destination, 0);
            if (destination.Length != values.Length)
            {
                Assert.False(true, string.Format("Error, altered array after copying empty collection"));
            }
            if (destination.Length == values.Length)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (String.Compare(destination.GetValue(i).ToString(), values[i]) != 0)
                    {
                        Assert.False(true, string.Format("Error, altered item {0} after copying empty collection", i));
                    }
                }
            }


            // [] add simple strings and CopyTo(Array, 0)
            //

            cnt = sd.Count;
            int len = values.Length;
            for (int i = 0; i < len; i++)
            {
                sd.Add(keys[i], values[i]);
            }
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, values.Length));
            }

            destination = Array.CreateInstance(typeof(Object), len);
            sd.CopyTo(destination, 0);

            IEnumerator en = sd.GetEnumerator();
            //
            // order of items is the same as order of enumerator
            //
            for (int i = 0; i < len; i++)
            {
                en.MoveNext();
                // verify that collection is copied correctly
                //
                DictionaryEntry curr = (DictionaryEntry)en.Current;

                if (String.Compare(curr.Value.ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Value, curr.Value));
                }

                if (String.Compare(curr.Key.ToString(), ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Key, curr.Key));
                }
            }

            //
            // [] add simple strings and CopyTo(Array, middle_index)


            sd.Clear();

            for (int i = 0; i < len; i++)
            {
                sd.Add(keys[i], values[i]);
            }
            if (sd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, values.Length));
            }

            destination = Array.CreateInstance(typeof(Object), len * 2);
            sd.CopyTo(destination, len);

            en = sd.GetEnumerator();
            //
            // order of items is the same as order of enumerator
            //
            for (int i = 0; i < len; i++)
            {
                en.MoveNext();
                // verify that collection is copied correctly
                //
                DictionaryEntry curr = (DictionaryEntry)en.Current;

                if (String.Compare(curr.Value.ToString(), ((DictionaryEntry)destination.GetValue(i + len)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Value, curr.Value));
                }

                if (String.Compare(curr.Key.ToString(), ((DictionaryEntry)destination.GetValue(i + len)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Key, curr.Key));
                }
            }

            //
            // Intl strings
            // [] add intl strings and CopyTo(Array, 0)
            //

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

            Boolean caseInsensitive = false;
            for (int i = 0; i < len * 2; i++)
            {
                if (intlValues[i].Length != 0 && intlValues[i].ToLowerInvariant() == intlValues[i].ToUpperInvariant())
                    caseInsensitive = true;
            }

            sd.Clear();
            for (int i = 0; i < len; i++)
            {
                sd.Add(intlValues[i + len], intlValues[i]);
            }
            if (sd.Count != (len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", sd.Count, len));
            }

            destination = Array.CreateInstance(typeof(Object), len);
            sd.CopyTo(destination, 0);

            en = sd.GetEnumerator();
            //
            // order of items is the same as order of enumerator
            //
            for (int i = 0; i < len; i++)
            {
                en.MoveNext();
                // verify that collection is copied correctly
                //
                DictionaryEntry curr = (DictionaryEntry)en.Current;

                if (String.Compare(curr.Value.ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Value, curr.Value));
                }

                if (String.Compare(curr.Key.ToString(), ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Key, curr.Key));
                }
            }

            //
            // Intl strings
            // [] add intl strings and CopyTo(Array, middle_index)
            //


            destination = Array.CreateInstance(typeof(Object), len * 2);
            sd.CopyTo(destination, len);

            en = sd.GetEnumerator();
            //
            // order of items is the same as order of enumerator
            //
            for (int i = 0; i < len; i++)
            {
                en.MoveNext();
                // verify that collection is copied correctly
                //
                DictionaryEntry curr = (DictionaryEntry)en.Current;

                if (String.Compare(curr.Value.ToString(), ((DictionaryEntry)destination.GetValue(i + len)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Value, curr.Value));
                }

                if (String.Compare(curr.Key.ToString(), ((DictionaryEntry)destination.GetValue(i + len)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Key, curr.Key));
                }
            }


            //
            // [] Case sensitivity
            //

            string[] intlValuesLower = new string[len * 2];

            // fill array with unique strings
            //
            for (int i = 0; i < len * 2; i++)
            {
                intlValues[i] = intlValues[i].ToUpperInvariant();
            }

            for (int i = 0; i < len * 2; i++)
            {
                intlValuesLower[i] = intlValues[i].ToLowerInvariant();
            }

            sd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                sd.Add(intlValues[i + len], intlValues[i]);     // adding uppercase strings
            }

            destination = Array.CreateInstance(typeof(Object), len);
            sd.CopyTo(destination, 0);

            en = sd.GetEnumerator();
            //
            // order of items is the same as order of enumerator
            //
            for (int i = 0; i < len; i++)
            {
                en.MoveNext();
                // verify that collection is copied correctly
                //
                DictionaryEntry curr = (DictionaryEntry)en.Current;

                if (String.Compare(curr.Value.ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Value, curr.Value));
                }

                if (!caseInsensitive && (Array.IndexOf(intlValuesLower, ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != -1))
                {
                    Assert.False(true, string.Format("Error, copied lowercase string"));
                }

                if (String.Compare(curr.Key.ToString(), ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Key, curr.Key));
                }

                if (Array.IndexOf(intlValuesLower, ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) == -1)
                {
                    Assert.False(true, string.Format("Error, copied uppercase key"));
                }
            }


            //
            //  [] CopyTo(null, int)
            //
            destination = null;
            Assert.Throws<ArgumentNullException>(() => { sd.CopyTo(destination, 0); });

            //
            //  [] CopyTo(string[], -1)
            //
            cnt = sd.Count;

            destination = Array.CreateInstance(typeof(Object), cnt);
            Assert.Throws<ArgumentOutOfRangeException>(() => { sd.CopyTo(destination, -1); });

            //
            // [] CopyTo(Array, upperBound+1)
            //
            if (sd.Count < 1)
            {
                for (int i = 0; i < len; i++)
                {
                    sd.Add(keys[i], values[i]);
                }
            }

            destination = Array.CreateInstance(typeof(Object), len);
            Assert.Throws<ArgumentException>(() => { sd.CopyTo(destination, len); });

            //
            // [] CopyTo(Array, upperBound+2)
            //
            Assert.Throws<ArgumentException>(() => { sd.CopyTo(destination, len + 1); });

            //
            // [] CopyTo(Array, not_enough_space)
            //
            Assert.Throws<ArgumentException>(() => { sd.CopyTo(destination, len / 2); });

            //
            // [] CopyTo(multidim_Array, 0)
            //

            destination = new object[len, len];
            Assert.Throws<ArgumentException>(() => { sd.CopyTo(destination, 0); });
        }
    }
}
