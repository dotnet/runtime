// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    public class ContainsObjTests
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

            // [] Contains on empty dictionary

            Assert.Throws<ArgumentNullException>(() => { hd.Contains(null); });


            if (hd.Contains("some_string"))
            {
                Assert.False(true, string.Format("Error, empty dictionary contains some_object"));
            }
            if (hd.Contains(new Hashtable()))
            {
                Assert.False(true, string.Format("Error, empty dictionary contains some_object"));
            }

            // [] simple strings and Contains()
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
                if (!hd.Contains(keysShort[i]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain \"{1}\"", i, keysShort[i]));
                }
            }

            cnt = hd.Count;
            len = valuesLong.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len + cnt)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len + cnt));
            }
            // verify new keys
            for (int i = 0; i < len; i++)
            {
                if (!hd.Contains(keysLong[i]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain \"{1}\"", i, keysLong[i]));
                }
            }
            // verify old keys
            for (int i = 0; i < valuesShort.Length; i++)
            {
                if (!hd.Contains(keysShort[i]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain \"{1}\"", i, keysShort[i]));
                }
            }


            //
            // [] Intl strings and Contains()
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


            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(intlValues[i + len], intlValues[i]);
            }
            if (hd.Count != (len))
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            for (int i = 0; i < len; i++)
            {
                //
                if (!hd.Contains(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain \"{1}\"", i, intlValues[i + len]));
                }
            }


            //
            // [] Case sensitivity
            // by default HybridDictionary is case-sensitive
            //


            hd.Clear();
            len = valuesLong.Length;
            //
            // will use first half of array as valuesShort and second half as keysShort
            //
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);     // adding uppercase strings
            }

            //
            for (int i = 0; i < len; i++)
            {
                // uppercase key
                if (!hd.Contains(keysLong[i]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain added uppercase \"{1}\"", i, keysLong[i]));
                }

                // lowercase key
                if (hd.Contains(keysLong[i].ToUpper()))
                {
                    Assert.False(true, string.Format("Error, contains uppercase \"{1}\" - should not", i, keysLong[i].ToUpper()));
                }
            }

            //  [] different_in_casing_only keys and Contains()
            //

            hd.Clear();
            string[] ks = { "Key", "kEy", "keY" };
            len = ks.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(ks[i], "Value" + i);
            }
            if (hd.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len));
            }

            if (hd.Contains("Value0"))
            {
                Assert.False(true, string.Format("Error, returned true when should not"));
            }
            for (int i = 0; i < len; i++)
            {
                if (!hd.Contains(ks[i]))
                {
                    Assert.False(true, string.Format("Error, returned false when true expected", i));
                }
            }

            cnt = hd.Count;
            len = valuesLong.Length;
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }
            if (hd.Count != len + cnt)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, len + cnt));
            }
            // verify new keys
            for (int i = 0; i < len; i++)
            {
                if (!hd.Contains(keysLong[i]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain \"{1}\"", i, keysLong[i]));
                }
            }
            // verify old keys
            for (int i = 0; i < ks.Length; i++)
            {
                if (!hd.Contains(ks[i]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain \"{1}\"", i, ks[i]));
                }
            }


            //
            //   [] Contains(null) - for filled dictionary
            //
            len = valuesShort.Length;
            hd.Clear();
            for (int i = 0; i < len; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }

            Assert.Throws<ArgumentNullException>(() => { hd.Contains(null); });

            // [] Contains() for case-insensitive comparer dictionary
            //

            hd = new HybridDictionary(true);
            hd.Clear();
            len = ks.Length;
            hd.Add(ks[0], "Value0");

            for (int i = 1; i < len; i++)
            {
                Assert.Throws<ArgumentException>(() => { hd.Add(ks[i], "Value" + i); });
            }
            if (hd.Count != 1)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, 1));
            }

            if (hd.Contains("Value0"))
            {
                Assert.False(true, string.Format("Error, returned true when should not"));
            }
            for (int i = 0; i < len; i++)
            {
                if (!hd.Contains(ks[i]))
                {
                    Assert.False(true, string.Format("Error, returned false when true expected", i));
                }
            }
            if (!hd.Contains("KEY"))
            {
                Assert.False(true, string.Format("Error, returned false non-existing-cased key"));
            }

            // [] few not_overriding_Equals objects and Contains()
            //

            hd = new HybridDictionary();
            hd.Clear();
            Hashtable[] lbl = new Hashtable[2];
            lbl[0] = new Hashtable();
            lbl[1] = new Hashtable();
            ArrayList[] b = new ArrayList[2];
            b[0] = new ArrayList();
            b[1] = new ArrayList();
            hd.Add(lbl[0], b[0]);
            hd.Add(lbl[1], b[1]);

            Assert.Throws<ArgumentException>(() => { hd.Add(lbl[0], "Hello"); });

            if (hd.Count != 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, 2));
            }

            if (!hd.Contains(lbl[0]))
            {
                Assert.False(true, string.Format("Error, returned false when true expected"));
            }
            if (!hd.Contains(lbl[1]))
            {
                Assert.False(true, string.Format("Error, returned false when true expected"));
            }
            if (hd.Contains(new Hashtable()))
            {
                Assert.False(true, string.Format("Error, returned true when false expected"));
            }

            // [] many not_overriding_Equals objects and Contains()
            //

            hd = new HybridDictionary();
            hd.Clear();
            int num = 40;
            lbl = new Hashtable[num];
            b = new ArrayList[num];
            for (int i = 0; i < num; i++)
            {
                lbl[i] = new Hashtable();
                b[i] = new ArrayList();
                hd.Add(lbl[i], b[i]);
            }

            Assert.Throws<ArgumentException>(() => { hd.Add(lbl[0], "Hello"); });


            if (hd.Count != num)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, num));
            }

            for (int i = 0; i < num; i++)
            {
                if (!hd.Contains(lbl[i]))
                {
                    Assert.False(true, string.Format("Error, returned false when true expected", i));
                }
            }
            if (hd.Contains(new Hashtable()))
            {
                Assert.False(true, string.Format("Error, returned true when false expected"));
            }

            // [] few not_overriding_Equals structs and Contains()

            hd = new HybridDictionary();
            SpecialStruct s = new SpecialStruct();
            s.Num = 1;
            s.Wrd = "one";
            SpecialStruct s1 = new SpecialStruct();
            s1.Num = 1;
            s1.Wrd = "one";
            hd.Add(s, "first");
            hd.Add(s1, "second");
            if (hd.Count != 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, 2));
            }
            if (!hd.Contains(s))
            {
                Assert.False(true, string.Format("Error, returned false when true expected"));
            }
            if (!hd.Contains(s1))
            {
                Assert.False(true, string.Format("Error, returned false when true expected"));
            }

            // [] many not_overriding_Equals structs and Contains()

            hd = new HybridDictionary();
            SpecialStruct[] ss = new SpecialStruct[num];
            for (int i = 0; i < num; i++)
            {
                ss[i] = new SpecialStruct();
                ss[i].Num = i;
                ss[i].Wrd = "value" + i;
                hd.Add(ss[i], "item" + i);
            }
            if (hd.Count != num)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", hd.Count, num));
            }
            for (int i = 0; i < num; i++)
            {
                if (!hd.Contains(ss[i]))
                {
                    Assert.False(true, string.Format("Error, returned false when true expected", i));
                }
            }
            s = new SpecialStruct();
            s.Num = 1;
            s.Wrd = "value1";
            if (hd.Contains(s))
            {
                Assert.False(true, string.Format("Error, returned true when false expected"));
            }
        }
    }
}