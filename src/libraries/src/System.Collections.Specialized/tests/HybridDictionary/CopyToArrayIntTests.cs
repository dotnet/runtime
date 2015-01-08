// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class CopyToArrayIntTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            IntlStrings intl;
            HybridDictionary hd;
            const int BIG_LENGTH = 100;

            // simple string values
            string[] valuesShort =
            {
                "",
                " ",
                "$%^#",
                System.DateTime.Today.ToString(),
                Int32.MaxValue.ToString()
            };

            // keys for simple string values
            string[] keysShort =
            {
                Int32.MaxValue.ToString(),
                " ",
                System.DateTime.Today.ToString(),
                "",
                "$%^#"
            };

            string[] valuesLong = new string[BIG_LENGTH];
            string[] keysLong = new string[BIG_LENGTH];

            // string [] destination;
            Array destination;

            int cnt = 0;            // Count

            // initialize IntStrings
            intl = new IntlStrings();

            for (int i = 0; i < BIG_LENGTH; i++)
            {
                valuesLong[i] = "Item" + i;
                keysLong[i] = "keY" + i;
            }

            // [] HybridDictionary is constructed as expected
            //-----------------------------------------------------------------

            hd = new HybridDictionary();

            // []  Copy empty dictionary into empty array
            //
            destination = Array.CreateInstance(typeof(Object), 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => { hd.CopyTo(destination, -1); });

            hd.CopyTo(destination, 0);

            // exception even when copying empty dictionary
            Assert.Throws<ArgumentException>(() => { hd.CopyTo(destination, 1); });

            //  [] Copy empty dictionary into filled array
            //
            destination = Array.CreateInstance(typeof(Object), valuesShort.Length);
            for (int i = 0; i < valuesShort.Length; i++)
            {
                destination.SetValue(valuesShort[i], i);
            }
            hd.CopyTo(destination, 0);
            if (destination.Length != valuesShort.Length)
            {
                Assert.False(true, string.Format("Error, altered array after copying empty dictionary"));
            }
            if (destination.Length == valuesShort.Length)
            {
                for (int i = 0; i < valuesShort.Length; i++)
                {
                    if (String.Compare(destination.GetValue(i).ToString(), valuesShort[i]) != 0)
                    {
                        Assert.False(true, string.Format("Error, altered item {0} after copying empty dictionary", i));
                    }
                }
            }


            //  [] few simple strings and CopyTo(Array, 0)
            //

            hd.Clear();
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

            destination = Array.CreateInstance(typeof(Object), len);
            hd.CopyTo(destination, 0);
            //
            //
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //

                if (String.Compare(hd[keysShort[i]].ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Value.ToString(), hd[keysShort[i]]));
                }
                if (String.Compare(keysShort[i], ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Key.ToString(), keysShort[i]));
                }
            }

            //  [] few simple strings and CopyTo(Array, middle_index)
            //


            hd.Clear();

            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, valuesShort.Length));
            }

            destination = Array.CreateInstance(typeof(Object), len * 2);
            hd.CopyTo(destination, len);

            //
            //
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                if (String.Compare(hd[keysShort[i]].ToString(), ((DictionaryEntry)destination.GetValue(i + len)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Value, hd[keysShort[i]]));
                }
                // verify keysShort
                if (String.Compare(keysShort[i], ((DictionaryEntry)destination.GetValue(i + len)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Key, keysShort[i]));
                }
            }

            //  [] many simple strings and CopyTo(Array, 0)
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

            destination = Array.CreateInstance(typeof(Object), len);
            hd.CopyTo(destination, 0);
            //
            //
            IDictionaryEnumerator en = hd.GetEnumerator();
            en.MoveNext();
            // items are copied in the same order they are enumerated
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                Object k = en.Key;

                if (String.Compare(hd[k].ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Value.ToString(), hd[k]));
                }
                if (String.Compare(k.ToString(), ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Key.ToString(), k.ToString()));
                }
                en.MoveNext();
            }

            //  [] many simple strings and CopyTo(Array, middle_index)
            //


            hd.Clear();

            hd.Clear();
            len = valuesLong.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            destination = Array.CreateInstance(typeof(Object), len * 2);
            hd.CopyTo(destination, len);

            //
            //
            en = hd.GetEnumerator();
            en.MoveNext();
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                Object k = en.Key;
                if (String.Compare(hd[k].ToString(), ((DictionaryEntry)destination.GetValue(i + len)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Value, hd[k]));
                }
                // verify keysShort
                if (String.Compare(k.ToString(), ((DictionaryEntry)destination.GetValue(i + len)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Key, k));
                }
                en.MoveNext();
            }

            //
            // [] many Intl strings and CopyTo(Array, 0)
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

            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(intlValues[i + len], intlValues[i]);
            }
            if (hd.Count != (len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            destination = Array.CreateInstance(typeof(Object), len);
            hd.CopyTo(destination, 0);
            //
            //
            en = hd.GetEnumerator();
            en.MoveNext();
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                Object k = en.Key;
                if (String.Compare(hd[k].ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Value, hd[k]));
                }
                // verify keysShort
                if (String.Compare(k.ToString(), ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Key, k));
                }
                en.MoveNext();
            }


            //
            // [] many Intl strings and CopyTo(Array, middle_index)
            //


            destination = Array.CreateInstance(typeof(Object), len * 2);
            hd.CopyTo(destination, len);

            //
            // order of items is the same as they were in dictionary
            //
            en = hd.GetEnumerator();
            en.MoveNext();
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                Object k = en.Key;
                if (String.Compare(hd[k].ToString(), ((DictionaryEntry)destination.GetValue(i + len)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Value, hd[k]));
                }
                // verify keysShort
                if (String.Compare(k.ToString(), ((DictionaryEntry)destination.GetValue(i + len)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len)).Key, k));
                }
                en.MoveNext();
            }

            // [] few Intl strings and CopyTo(Array, 0)
            //

            int len1 = valuesShort.Length;

            hd.Clear();
            for (int i = 0; i < len1; i++)
            {
                hd.Add(intlValues[i + len1], intlValues[i]);
            }
            if (hd.Count != (len1))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len1));
            }

            destination = Array.CreateInstance(typeof(Object), len1);
            hd.CopyTo(destination, 0);
            //
            //
            en = hd.GetEnumerator();
            en.MoveNext();
            for (int i = 0; i < len1; i++)
            {
                // verify that dictionary is copied correctly
                //
                Object k = en.Key;
                if (String.Compare(hd[k].ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Value, hd[k]));
                }
                // verify keysShort
                if (String.Compare(k.ToString(), ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Key, k));
                }
                en.MoveNext();
            }

            //
            // [] few Intl strings and CopyTo(Array, middle_index)
            //


            destination = Array.CreateInstance(typeof(Object), len1 * 2);
            hd.CopyTo(destination, len1);

            //
            // order of items is the same as they were in dictionary
            //
            en = hd.GetEnumerator();
            en.MoveNext();
            for (int i = 0; i < len1; i++)
            {
                // verify that dictionary is copied correctly
                //
                Object k = en.Key;
                if (String.Compare(hd[k].ToString(), ((DictionaryEntry)destination.GetValue(i + len1)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len1)).Value, hd[k]));
                }
                // verify keysShort
                if (String.Compare(k.ToString(), ((DictionaryEntry)destination.GetValue(i + len1)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i + len1)).Key, k));
                }
                en.MoveNext();
            }


            //
            // [] Case sensitivity
            //

            string[] intlValuesUpper = new string[len * 2];
            string[] intlValuesLower = new string[len * 2];

            // fill array with unique upper-case strings
            //
            for (int i = 0; i < len * 2; i++)
            {
                string val = intlValues[i].ToUpper();
                while (Array.IndexOf(intlValuesUpper, val) != -1)
                    val = intl.GetRandomString(MAX_LEN).ToUpper();
                intlValuesUpper[i] = val;
            }

            caseInsensitive = false;
            for (int i = 0; i < len * 2; i++)
            {
                intlValuesLower[i] = intlValuesUpper[i].ToLower();
                if (intlValuesLower[i].Length != 0 &&
            intlValuesLower[i] == intlValuesUpper[i])
                    caseInsensitive = true;
            }

            hd.Clear();
            //
            // will use first half of array as values and second half as keys
            //
            for (int i = 0; i < len; i++)
            {
                hd.Add(intlValuesUpper[i + len], intlValuesUpper[i]);     // adding uppercase strings
            }

            destination = Array.CreateInstance(typeof(Object), len);
            hd.CopyTo(destination, 0);

            //
            //
            en = hd.GetEnumerator();
            en.MoveNext();
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                Object k = en.Key;
                if (String.Compare(hd[k].ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Value, hd[k]));
                }

                if (!caseInsensitive && Array.IndexOf(intlValuesLower, ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) > -1)
                {
                    Assert.False(true, string.Format("Error, copied lowercase string"));
                }
                // verify keysShort
                if (String.Compare(k.ToString(), ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, copied \"{1}\" instead of \"{2}\"", i, ((DictionaryEntry)destination.GetValue(i)).Key, k));
                }

                if (!caseInsensitive && Array.IndexOf(intlValuesLower, ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) > -1)
                {
                    Assert.False(true, string.Format("Error, copied lowercase key"));
                }
                en.MoveNext();
            }

            //  ----------------------------------------------------------------
            //   [] Parameter validation for short HybridDictionary (list)
            //  ----------------------------------------------------------------

            hd = new HybridDictionary();
            for (int i = 0; i < len1; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }
            //
            //   CopyTo(null, int)
            //
            destination = null;
            Assert.Throws<ArgumentNullException>(() => { hd.CopyTo(destination, 0); });

            //
            //   CopyTo(Array, -1)
            //

            destination = Array.CreateInstance(typeof(Object), 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => { hd.CopyTo(destination, -1); });

            //
            //   CopyTo(Array, upperBound+1)
            //
            cnt = hd.Count;

            destination = Array.CreateInstance(typeof(Object), cnt);
            Assert.Throws<ArgumentException>(() => { hd.CopyTo(destination, cnt); });

            //
            //   CopyTo(Array, upperBound+2)
            //
            Assert.Throws<ArgumentException>(() => { hd.CopyTo(destination, cnt + 1); });

            //
            //   CopyTo(Array, not_enough_space)
            //
            Assert.Throws<ArgumentException>(() => { hd.CopyTo(destination, cnt / 2); });

            //
            //   CopyTo(multidim_Array, 0)
            //

            Array dest = new String[cnt, cnt];
            Assert.Throws<ArgumentException>(() => { hd.CopyTo(dest, 0); });

            //
            //   CopyTo(wrong_type, 0)
            //

            dest = Array.CreateInstance(typeof(ArrayList), cnt);
            Assert.Throws<System.InvalidCastException>(() => { hd.CopyTo(dest, 0); });
            //
            //   CopyTo(Array, upperBound+1) - copy empty dictionary - no exception
            //
            hd.Clear();

            destination = Array.CreateInstance(typeof(Object), len);
            hd.CopyTo(destination, len);


            //  ----------------------------------------------------------------
            //   [] Parameter validation for long HybridDictionary (hashtable)
            //  ----------------------------------------------------------------

            hd = new HybridDictionary();
            len = valuesLong.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            //
            //   CopyTo(null, int)
            //
            destination = null;
            Assert.Throws<ArgumentNullException>(() => { hd.CopyTo(destination, 0); });
            //
            //   CopyTo(Array, -1)
            //

            destination = Array.CreateInstance(typeof(Object), 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => { hd.CopyTo(destination, -1); });

            //
            //   CopyTo(Array, upperBound+1)
            //
            cnt = hd.Count;

            destination = Array.CreateInstance(typeof(Object), cnt);
            Assert.Throws<ArgumentException>(() => { hd.CopyTo(destination, cnt); });

            //
            //   CopyTo(Array, upperBound+2)
            //
            Assert.Throws<ArgumentException>(() => { hd.CopyTo(destination, cnt + 1); });

            //
            //   CopyTo(Array, not_enough_space)
            //
            Assert.Throws<ArgumentException>(() => { hd.CopyTo(destination, cnt / 2); });

            //
            //   CopyTo(multidim_Array, 0)
            //

            dest = Array.CreateInstance(typeof(string), cnt, cnt);
            Assert.Throws<ArgumentException>(() => { hd.CopyTo(dest, 0); });

            //
            //   CopyTo(wrong_type, 0)
            //

            dest = Array.CreateInstance(typeof(ArrayList), cnt);
            Assert.Throws<System.InvalidCastException>(() => { hd.CopyTo(dest, 0); });

            //
            //  [] CopyTo() for few not_overriding_Equals objects
            //

            hd.Clear();
            int num = 2;
            Hashtable[] lbl = new Hashtable[num];
            ArrayList[] b = new ArrayList[num];
            for (int i = 0; i < num; i++)
            {
                lbl[i] = new Hashtable();
                b[i] = new ArrayList();
                hd.Add(lbl[i], b[i]);
            }

            destination = Array.CreateInstance(typeof(Object), num);
            hd.CopyTo(destination, 0);
            //
            //
            en = hd.GetEnumerator();
            en.MoveNext();
            for (int i = 0; i < num; i++)
            {
                // verify that dictionary is copied correctly
                //
                Object k = en.Key;
                if (!hd[k].Equals(((DictionaryEntry)destination.GetValue(i)).Value))
                {
                    Assert.False(true, string.Format("Error, failed to copy {0}th entry", i));
                }
                // verify keysShort
                if (!k.Equals(((DictionaryEntry)destination.GetValue(i)).Key))
                {
                    Assert.False(true, string.Format("Error, failed to copy {0} entry", i));
                }
                en.MoveNext();
            }

            //  [] CopyTo() for many not_overriding_Equals objects
            //
            hd.Clear();
            num = 40;
            lbl = new Hashtable[num];
            b = new ArrayList[num];
            for (int i = 0; i < num; i++)
            {
                lbl[i] = new Hashtable();
                b[i] = new ArrayList();
                hd.Add(lbl[i], b[i]);
            }

            destination = Array.CreateInstance(typeof(Object), num);
            hd.CopyTo(destination, 0);
            //
            //
            en = hd.GetEnumerator();
            en.MoveNext();
            for (int i = 0; i < num; i++)
            {
                // verify that dictionary is copied correctly
                //
                Object k = en.Key;
                if (!hd[k].Equals(((DictionaryEntry)destination.GetValue(i)).Value))
                {
                    Assert.False(true, string.Format("Error, failed to copy {0}th entry", i));
                }
                // verify keysShort
                if (!k.Equals(((DictionaryEntry)destination.GetValue(i)).Key))
                {
                    Assert.False(true, string.Format("Error, failed to copy {0} entry", i));
                }
                en.MoveNext();
            }

            //
            //  [] CopyTo() - for short case-insensitive HybridDictionary
            //
            hd = new HybridDictionary(true);
            len = 3;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }

            destination = Array.CreateInstance(typeof(Object), len);
            hd.CopyTo(destination, 0);
            //
            //
            en = hd.GetEnumerator();
            en.MoveNext();
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                Object k = en.Key;
                if (String.Compare(hd[k].ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) < 0)
                {
                    Assert.False(true, string.Format("Error, failed to copy {0}th entry when case-insensitive", i));
                }
                // verify keysShort
                if (String.Compare(k.ToString(), ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) < 0)
                {
                    Assert.False(true, string.Format("Error, failed to copy {0} entry when case-insensitive", i));
                }
                en.MoveNext();
            }

            //  [] CopyTo() - for long case-insensitive HybridDictionary
            //
            hd = new HybridDictionary(true);
            len = valuesLong.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }

            destination = Array.CreateInstance(typeof(Object), len);
            hd.CopyTo(destination, 0);
            //
            //
            en = hd.GetEnumerator();
            en.MoveNext();
            for (int i = 0; i < len; i++)
            {
                // verify that dictionary is copied correctly
                //
                Object k = en.Key;
                if (String.Compare(hd[k].ToString(), ((DictionaryEntry)destination.GetValue(i)).Value.ToString()) < 0)
                {
                    Assert.False(true, string.Format("Error, failed to copy {0}th entry when case-insensitive", i));
                }
                // verify keysShort
                if (String.Compare(k.ToString(), ((DictionaryEntry)destination.GetValue(i)).Key.ToString()) < 0)
                {
                    Assert.False(true, string.Format("Error, failed to copy {0} entry when case-insensitive", i));
                }
                en.MoveNext();
            }
        }
    }
}