// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetEnumeratorListDictionaryTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            ListDictionary ld;
            IDictionaryEnumerator en;

            DictionaryEntry curr;        // Enumerator.Current value
            DictionaryEntry de;        // Enumerator.Entry value
            Object k;        // Enumerator.Key value
            Object v;        // Enumerator.Value

            // simple string values
            string[] values =
            {
                "a",
                "aa",
                "",
                " ",
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


            // [] ListDictionary GetEnumerator()
            //-----------------------------------------------------------------

            ld = new ListDictionary();

            // [] for empty dictionary
            //
            en = ld.GetEnumerator();
            string type = en.GetType().ToString();
            if (type.IndexOf("Enumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not Enumerator"));
            }

            //
            //  MoveNext should return false
            //
            bool res = en.MoveNext();
            if (res)
            {
                Assert.False(true, string.Format("Error, MoveNext returned true"));
            }

            //
            //  Attempt to get Current should result in exception
            //
            Assert.Throws<InvalidOperationException>(() => { curr = (DictionaryEntry)en.Current; });
            //
            //   []  for Filled dictionary
            //
            for (int i = 0; i < values.Length; i++)
            {
                ld.Add(keys[i], values[i]);
            }

            en = ld.GetEnumerator();
            type = en.GetType().ToString();
            if (type.IndexOf("Enumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not Enumerator"));
            }

            //
            //  MoveNext should return true
            //

            for (int i = 0; i < ld.Count; i++)
            {
                res = en.MoveNext();
                if (!res)
                {
                    Assert.False(true, string.Format("Error, MoveNext returned false", i));
                }

                curr = (DictionaryEntry)en.Current;
                de = en.Entry;
                //
                //enumerator enumerates in different than added order
                // so we'll check Contains
                //
                if (!ld.Contains(curr.Key.ToString()))
                {
                    Assert.False(true, string.Format("Error, Current dictionary doesn't contain key from enumerator", i));
                }
                if (!ld.Contains(en.Key.ToString()))
                {
                    Assert.False(true, string.Format("Error, Current dictionary doesn't contain key from enumerator", i));
                }
                if (!ld.Contains(de.Key.ToString()))
                {
                    Assert.False(true, string.Format("Error, Current dictionary doesn't contain Entry.Key from enumerator", i));
                }
                if (String.Compare(ld[curr.Key.ToString()].ToString(), curr.Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, Value for current Key is different in dictionary", i));
                }
                if (String.Compare(ld[de.Key.ToString()].ToString(), de.Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, Entry.Value for current Entry.Key is different in dictionary", i));
                }
                if (String.Compare(ld[en.Key.ToString()].ToString(), en.Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, En-tor.Value for current En-tor.Key is different in dictionary", i));
                }

                // while we didn't MoveNext, Current should return the same value
                DictionaryEntry curr1 = (DictionaryEntry)en.Current;
                if (!curr.Equals(curr1))
                {
                    Assert.False(true, string.Format("Error, second call of Current returned different result", i));
                }
                DictionaryEntry de1 = en.Entry;
                if (!de.Equals(de1))
                {
                    Assert.False(true, string.Format("Error, second call of Entry returned different result", i));
                }
            }

            // next MoveNext should bring us outside of the collection
            //
            res = en.MoveNext();
            res = en.MoveNext();
            if (res)
            {
                Assert.False(true, string.Format("Error, MoveNext returned true"));
            }

            //
            //  Attempt to get Current should result in exception
            //
            Assert.Throws<InvalidOperationException>(() => { curr = (DictionaryEntry)en.Current; });

            //
            //  Attempt to get Entry should result in exception
            //
            Assert.Throws<InvalidOperationException>(() => { de = en.Entry; });

            //
            //  Attempt to get Key should result in exception
            //
            Assert.Throws<InvalidOperationException>(() => { k = en.Key; });

            //
            //  Attempt to get Value should result in exception
            //
            Assert.Throws<InvalidOperationException>(() => { v = en.Value; });

            en.Reset();

            //
            //  Attempt to get Current should result in exception
            //
            Assert.Throws<InvalidOperationException>(() => { curr = (DictionaryEntry)en.Current; });

            //
            //  Attempt to get Entry should result in exception
            //
            Assert.Throws<InvalidOperationException>(() => { de = en.Entry; });

            //
            //   [] Modify dictionary when enumerating
            //
            if (ld.Count < 1)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    ld.Add(keys[i], values[i]);
                }
            }

            en = ld.GetEnumerator();
            res = en.MoveNext();
            if (!res)
            {
                Assert.False(true, string.Format("Error, MoveNext returned false"));
            }
            curr = (DictionaryEntry)en.Current;
            de = en.Entry;
            k = en.Key;
            v = en.Value;
            int cnt = ld.Count;
            ld.Remove(keys[0]);
            if (ld.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove item with 0th key"));
            }

            // will return just removed item
            DictionaryEntry curr2 = (DictionaryEntry)en.Current;
            if (!curr.Equals(curr2))
            {
                Assert.False(true, string.Format("Error, current returned different value after modification"));
            }

            // will return just removed item
            DictionaryEntry de2 = en.Entry;
            if (!de.Equals(de2))
            {
                Assert.False(true, string.Format("Error, Entry returned different value after modification"));
            }

            // will return just removed item
            Object k2 = en.Key;
            if (!k.Equals(k2))
            {
                Assert.False(true, string.Format("Error, Key returned different value after modification"));
            }

            // will return just removed item
            Object v2 = en.Value;
            if (!v.Equals(v2))
            {
                Assert.False(true, string.Format("Error, Value returned different value after modification"));
            }

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { res = en.MoveNext(); });

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { en.Reset(); });


            //
            //   [] Modify dictionary after enumerated beyond the end
            //
            ld.Clear();
            for (int i = 0; i < values.Length; i++)
            {
                ld.Add(keys[i], values[i]);
            }

            en = ld.GetEnumerator();
            for (int i = 0; i < ld.Count; i++)
            {
                en.MoveNext();
            }
            curr = (DictionaryEntry)en.Current;
            de = en.Entry;
            k = en.Key;
            v = en.Value;

            cnt = ld.Count;
            ld.Remove(keys[0]);
            if (ld.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove item with 0th key"));
            }

            // will return just removed item
            DictionaryEntry curr3 = (DictionaryEntry)en.Current;
            if (!curr.Equals(curr3))
            {
                Assert.False(true, string.Format("Error, current returned different value after modification"));
            }

            // will return just removed item
            de2 = en.Entry;
            if (!de.Equals(de2))
            {
                Assert.False(true, string.Format("Error, Entry returned different value after modification"));
            }

            // will return just removed item
            k2 = en.Key;
            if (!k.Equals(k2))
            {
                Assert.False(true, string.Format("Error, Key returned different value after modification"));
            }

            // will return just removed item
            v2 = en.Value;
            if (!v.Equals(v2))
            {
                Assert.False(true, string.Format("Error, Value returned different value after modification"));
            }

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { res = en.MoveNext(); });

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { en.Reset(); });

            /////////////////////////////////////
            //// Code coverage
            //
            // [] Call IEnumerable.GetEnumerator()
            //
            en = ld.GetEnumerator();
            IEnumerator en2 = ((IEnumerable)ld).GetEnumerator();
            if (en2 == null)
            {
                Assert.False(true, string.Format("Error, enumerator was null"));
            }
        }
    }
}
