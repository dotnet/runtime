// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class CopyToArrayIntListDictionaryTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            ListDictionary ld;

            // simple string values
            string[] values =
            {
                "",
                " ",
                "a",
                "aA",
                "text",
                "     SPaces",
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
                "oNe",
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

            // string [] destination;
            Array destination;

            int cnt = 0;            // Count

            // initialize IntStrings
            intl = new IntlStrings();


            // [] ListDictionary is constructed as expected
            //-----------------------------------------------------------------

            ld = new ListDictionary();

            // [] CopyTo empty dictionary into empty array
            //
            destination = new Object[0];
            Assert.Throws<ArgumentOutOfRangeException>(() => { ld.CopyTo(destination, -1); });

            ld.CopyTo(destination, 0);

            // exception even when copying empty dictionary
            Assert.Throws<ArgumentException>(() => { ld.CopyTo(destination, 1); });

            // [] CopyTo empty dictionary into filled array
            //
            destination = new Object[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                destination.SetValue(values[i], i);
            }
            ld.CopyTo(destination, 0);
            if (destination.Length != values.Length)
            {
                Assert.False(true, string.Format("Error, altered array after copying empty dictionary"));
            }
            if (destination.Length == values.Length)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (String.Compare(destination.GetValue(i).ToString(), values[i]) != 0)
                    {
                        Assert.False(true, string.Format("Error, altered item {0} after copying empty dictionary", i));
                    }
                }
            }

            //
            // [] CopyTo(Array, 0) for dictionary filled with simple strings


            cnt = ld.Count;
            int len = values.Length;
            for (int i = 0; i < len; i++)
            {
                ld.Add(keys[i], values[i]);
            }
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, values.Length));
            }

            destination = new Object[len];
            ld.CopyTo(destination, 0);
            //
            //
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //

                if (String.Compare(ld[keys[i]].ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Value.ToString(), ld[keys[i]]));
                }
                if (String.Compare(keys[i], ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Key.ToString(), keys[i]));
                }
            }


            //
            // [] CopyTo(Array, middle_index) for dictionary filled with simple strings
            //

            ld.Clear();

            for (int i = 0; i < len; i++)
            {
                ld.Add(keys[i], values[i]);
            }
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, values.Length));
            }

            destination = new Object[len * 2];
            ld.CopyTo(destination, len);

            //
            //
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                if (String.Compare(ld[keys[i]].ToString(), ((DictionaryEntry)destination.GetValue(i + len)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Value, ld[keys[i]]));
                }
                // verify keys
                if (String.Compare(keys[i], ((DictionaryEntry)destination.GetValue(i + len)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Key, keys[i]));
                }
            }

            //
            // Intl strings
            //
            // [] CopyTo(Array, 0) for dictionary filled with Intl strings
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
                if (intlValues[i].Length != 0 && intlValues[i].ToLower() == intlValues[i].ToUpper())
                    caseInsensitive = true;
            }

            ld.Clear();
            for (int i = 0; i < len; i++)
            {
                ld.Add(intlValues[i + len], intlValues[i]);
            }
            if (ld.Count != (len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, len));
            }

            destination = new Object[len];
            ld.CopyTo(destination, 0);
            //
            //
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                if (String.Compare(ld[intlValues[i + len]].ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Value, ld[intlValues[i + len]]));
                }
                // verify keys
                if (String.Compare(intlValues[i + len], ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Key, intlValues[i + len]));
                }
            }

            //
            // Intl strings
            //
            // [] CopyTo(Array, middle_index) for dictionary filled with Intl strings
            //


            destination = new Object[len * 2];
            ld.CopyTo(destination, len);

            //
            // order of items is the same as they were in dictionary
            //
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                if (String.Compare(ld[intlValues[i + len]].ToString(), ((DictionaryEntry)destination.GetValue(i + len)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Value, ld[intlValues[i + len]]));
                }
                // verify keys
                if (String.Compare(intlValues[i + len], ((DictionaryEntry)destination.GetValue(i + len)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Key, intlValues[i + len]));
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
                intlValues[i] = intlValues[i].ToUpper();
            }

            for (int i = 0; i < len * 2; i++)
            {
                intlValuesLower[i] = intlValues[i].ToLower();
            }

            ld.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                ld.Add(intlValues[i + len], intlValues[i]);     // adding uppercase strings
            }

            destination = new Object[len];
            ld.CopyTo(destination, 0);

            //
            //
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                if (String.Compare(ld[intlValues[i + len]].ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Value, ld[intlValues[i + len]]));
                }

                if (!caseInsensitive && Array.IndexOf(intlValuesLower, ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) > -1)
                {
                    Assert.False(true, string.Format("Error, copied lowercase string"));
                }
                // verify keys
                if (String.Compare(intlValues[i + len], ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Key, intlValues[i + len]));
                }

                if (!caseInsensitive && Array.IndexOf(intlValuesLower, ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) > -1)
                {
                    Assert.False(true, string.Format("Error, copied lowercase key"));
                }
            }


            //
            //   [] CopyTo(null, int)
            //
            destination = null;
            Assert.Throws<ArgumentNullException>(() => { ld.CopyTo(destination, 0); });

            //
            //   [] CopyTo(Array, -1)
            //
            cnt = ld.Count;

            destination = new Object[2];
            Assert.Throws<ArgumentOutOfRangeException>(() => { ld.CopyTo(destination, -1); });

            //
            //  [] CopyTo(Array, upperBound+1)
            //
            if (ld.Count < 1)
            {
                ld.Clear();
                for (int i = 0; i < len; i++)
                {
                    ld.Add(keys[i], values[i]);
                }
            }

            destination = new Object[len];
            Assert.Throws<ArgumentException>(() => { ld.CopyTo(destination, len); });

            //
            //  [] CopyTo(Array, upperBound+2)
            //
            Assert.Throws<ArgumentException>(() => { ld.CopyTo(destination, len + 1); });

            //
            //  [] CopyTo(Array, not_enough_space)
            //
            Assert.Throws<ArgumentException>(() => { ld.CopyTo(destination, len / 2); });

            //
            //  [] CopyTo(multidim_Array, 0)
            //

            Array dest = new string[len, len];
            Assert.Throws<ArgumentException>(() => { ld.CopyTo(dest, 0); });

            //
            //  [] CopyTo(wrong_type, 0)
            //

            dest = new ArrayList[len];
            Assert.Throws<InvalidCastException>(() => { ld.CopyTo(dest, 0); });

            //
            //   [] CopyTo(Array, upperBound+1) - copy empty dictionary - no exception
            //
            ld.Clear();
            destination = new Object[len];
            ld.CopyTo(destination, len);
        }
    }
}
