// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;
using GenStrings;

namespace System.Collections.Specialized.Tests
{
    internal class InsensitiveComparer : IComparer
    {
        public Int32 Compare(Object obj1, Object obj2)
        {
            if (!(obj1 is String) || !(obj2 is String))
                throw new Exception("Err_insensitiveComparer! object needs to be String");
            return String.Compare((String)obj1, (String)obj2, StringComparison.CurrentCultureIgnoreCase);
        }
    }


    public class SpecialStruct
    {
        public Int32 Num;
        public String Wrd;
    }

    internal class SpecialComparer : IComparer
    {
        public Int32 Compare(Object obj1, Object obj2)
        {
            if (!(obj1 is Hashtable) || !(obj2 is Hashtable))
                throw new Exception("Err_specialHashtable! object needs to be Hashtable");
            ((Hashtable)obj1).Equals((Hashtable)obj2);
            return String.Compare((string)(((Hashtable)obj1)["foo"]), (string)(((Hashtable)obj2)["foo"]));
        }
    }


    public class ContainsObjListDictionaryTests
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

            int cnt = 0;            // Count

            // initialize IntStrings
            intl = new IntlStrings();


            // [] ListDictionary is constructed as expected
            //-----------------------------------------------------------------

            ld = new ListDictionary();

            // [] Contains() on empty dictionary
            //
            Assert.Throws<ArgumentNullException>(() => { ld.Contains(null); });

            if (ld.Contains("some_string"))
            {
                Assert.False(true, string.Format("Error, empty dictionary contains some_object"));
            }
            if (ld.Contains(new Hashtable()))
            {
                Assert.False(true, string.Format("Error, empty dictionary contains some_object"));
            }

            //  [] simple strings and Contains()
            //

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
            //
            for (int i = 0; i < len; i++)
            {
                if (!ld.Contains(keys[i]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain \"{1}\"", i, keys[i]));
                }
            }


            //
            // Intl strings
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

            for (int i = 0; i < len; i++)
            {
                //
                if (!ld.Contains(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain \"{1}\"", i, intlValues[i + len]));
                }
            }


            //
            // [] Case sensitivity
            // by default ListDictionary is case-sensitive
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

            //
            for (int i = 0; i < len; i++)
            {
                // uppercase key
                if (!ld.Contains(intlValues[i + len]))
                {
                    Assert.False(true, string.Format("Error, doesn't contain added uppercase \"{1}\"", i, intlValues[i + len]));
                }

                // lowercase key
                if (!caseInsensitive && ld.Contains(intlValuesLower[i + len]))
                {
                    Assert.False(true, string.Format("Error, contains lowercase \"{1}\" - should not", i, intlValuesLower[i + len]));
                }
            }

            //  [] different_in_casing_only keys and Contains()
            //

            ld.Clear();
            string[] ks = { "Key", "kEy", "keY" };
            len = ks.Length;
            for (int i = 0; i < len; i++)
            {
                ld.Add(ks[i], "Value" + i);
            }
            if (ld.Count != len)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, len));
            }

            if (ld.Contains("Value0"))
            {
                Assert.False(true, string.Format("Error, returned true when should not"));
            }
            for (int i = 0; i < len; i++)
            {
                if (!ld.Contains(ks[i]))
                {
                    Assert.False(true, string.Format("Error, returned false when true expected", i));
                }
            }


            //
            //   [] Contains(null) - for filled dictionary
            //
            ld.Clear();
            for (int i = 0; i < len; i++)
            {
                ld.Add(keys[i], values[i]);
            }
            Assert.Throws<ArgumentNullException>(() => { ld.Contains(null); });

            // [] Contains() for case-insensitive comparer
            //

            ld = new ListDictionary(new InsensitiveComparer());
            ld.Clear();
            len = ks.Length;
            ld.Add(ks[0], "Value0");

            for (int i = 1; i < len; i++)
            {
                Assert.Throws<ArgumentException>(() => { ld.Add(ks[i], "Value" + i); });
            }
            if (ld.Count != 1)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, 1));
            }

            if (ld.Contains("Value0"))
            {
                Assert.False(true, string.Format("Error, returned true when should not"));
            }
            for (int i = 0; i < len; i++)
            {
                if (!ld.Contains(ks[i]))
                {
                    Assert.False(true, string.Format("Error, returned false when true expected", i));
                }
            }
            if (!ld.Contains("KEY"))
            {
                Assert.False(true, string.Format("Error, returned false non-existing-cased key"));
            }

            // [] Contains() and objects_not_overriding_Equals
            //

            ld = new ListDictionary();
            ld.Clear();
            Hashtable lbl = new Hashtable();
            Hashtable lbl1 = new Hashtable();
            ArrayList b = new ArrayList();
            ArrayList b1 = new ArrayList();
            ld.Add(lbl, b);
            ld.Add(lbl1, b1);

            Assert.Throws<ArgumentException>(() => { ld.Add(lbl, "Hello"); });

            if (ld.Count != 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, 2));
            }

            if (!ld.Contains(lbl))
            {
                Assert.False(true, string.Format("Error, returned false when true expected"));
            }
            if (!ld.Contains(lbl1))
            {
                Assert.False(true, string.Format("Error, returned false when true expected"));
            }
            if (ld.Contains(new Hashtable()))
            {
                Assert.False(true, string.Format("Error, returned true when false expected"));
            }

            //  [] Contains and Special_Comparer for objects
            //

            ld = new ListDictionary(new SpecialComparer());
            lbl["foo"] = "Hello";
            lbl1["foo"] = "Hello";
            ld.Add(lbl, b);

            Assert.Throws<ArgumentException>(() => { ld.Add(lbl1, b1); });

            if (ld.Count != 1)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, 1));
            }

            if (!ld.Contains(lbl))
            {
                Assert.False(true, string.Format("Error, returned false when true expected"));
            }
            if (!ld.Contains(lbl1))
            {
                Assert.False(true, string.Format("Error, returned false when true expected"));
            }
            lbl1["foo"] = "HELLO";
            if (ld.Contains(lbl1))
            {
                Assert.False(true, string.Format("Error, returned true when false expected"));
            }

            //  [] Contains() and special_structs_not_overriding_Equals
            ld = new ListDictionary();
            SpecialStruct s = new SpecialStruct();
            s.Num = 1;
            s.Wrd = "one";
            SpecialStruct s1 = new SpecialStruct();
            s.Num = 1;
            s.Wrd = "one";
            ld.Add(s, "first");
            ld.Add(s1, "second");
            if (ld.Count != 2)
            {
                Assert.False(true, string.Format("Error, count is {0} instead of {1}", ld.Count, 2));
            }
            if (!ld.Contains(s))
            {
                Assert.False(true, string.Format("Error, returned false when true expected"));
            }
            if (!ld.Contains(s1))
            {
                Assert.False(true, string.Format("Error, returned false when true expected"));
            }
        }
    }
}
