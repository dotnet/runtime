// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetEnumeratorTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            HybridDictionary hd;
            IDictionaryEnumerator en;

            DictionaryEntry curr;        // Enumerator.Current value
            DictionaryEntry de;        // Enumerator.Entry value
            Object k;        // Enumerator.Key value
            Object v;        // Enumerator.Value

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

            for (int i = 0; i < BIG_LENGTH; i++)
            {
                valuesLong[i] = "Item" + i;
                keysLong[i] = "keY" + i;
            }

            // [] HybridDictionary GetEnumerator()
            //-----------------------------------------------------------------

            hd = new HybridDictionary();

            //  [] Enumerator for empty dictionary
            //
            en = hd.GetEnumerator();
            IEnumerator en2;
            en2 = ((IEnumerable)hd).GetEnumerator();
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

            //  ---------------------------------------------------------
            //   [] Enumerator for Filled dictionary  - list
            //  ---------------------------------------------------------
            //
            for (int i = 0; i < valuesShort.Length; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }

            en = hd.GetEnumerator();
            type = en.GetType().ToString();
            if (type.IndexOf("Enumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not Enumerator"));
            }

            //
            //  MoveNext should return true
            //

            for (int i = 0; i < hd.Count; i++)
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
                if (!hd.Contains(curr.Key.ToString()))
                {
                    Assert.False(true, string.Format("Error, Current dictionary doesn't contain key from enumerator", i));
                }
                if (!hd.Contains(en.Key.ToString()))
                {
                    Assert.False(true, string.Format("Error, Current dictionary doesn't contain key from enumerator", i));
                }
                if (!hd.Contains(de.Key.ToString()))
                {
                    Assert.False(true, string.Format("Error, Current dictionary doesn't contain Entry.Key from enumerator", i));
                }
                if (String.Compare(hd[curr.Key.ToString()].ToString(), curr.Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, Value for current Key is different in dictionary", i));
                }
                if (String.Compare(hd[de.Key.ToString()].ToString(), de.Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, Entry.Value for current Entry.Key is different in dictionary", i));
                }
                if (String.Compare(hd[en.Key.ToString()].ToString(), en.Value.ToString()) != 0)
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

            //  ---------------------------------------------------------
            //   [] Enumerator for Filled dictionary  - hashtable
            //  ---------------------------------------------------------
            //
            for (int i = 0; i < valuesLong.Length; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }

            en = hd.GetEnumerator();
            type = en.GetType().ToString();
            if (type.IndexOf("Enumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not Enumerator"));
            }

            //
            //  MoveNext should return true
            //

            for (int i = 0; i < hd.Count; i++)
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
                if (!hd.Contains(curr.Key.ToString()))
                {
                    Assert.False(true, string.Format("Error, Current dictionary doesn't contain key from enumerator", i));
                }
                if (!hd.Contains(en.Key.ToString()))
                {
                    Assert.False(true, string.Format("Error, Current dictionary doesn't contain key from enumerator", i));
                }
                if (!hd.Contains(de.Key.ToString()))
                {
                    Assert.False(true, string.Format("Error, Current dictionary doesn't contain Entry.Key from enumerator", i));
                }
                if (String.Compare(hd[curr.Key.ToString()].ToString(), curr.Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, Value for current Key is different in dictionary", i));
                }
                if (String.Compare(hd[de.Key.ToString()].ToString(), de.Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, Entry.Value for current Entry.Key is different in dictionary", i));
                }
                if (String.Compare(hd[en.Key.ToString()].ToString(), en.Value.ToString()) != 0)
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

            //=========================================================
            // --------------------------------------------------------
            //
            //   Modify dictionary when enumerating
            //
            //  [] Enumerator and short modified HD (list)
            //
            hd.Clear();
            if (hd.Count < 1)
            {
                for (int i = 0; i < valuesShort.Length; i++)
                {
                    hd.Add(keysShort[i], valuesShort[i]);
                }
            }

            en = hd.GetEnumerator();
            res = en.MoveNext();
            if (!res)
            {
                Assert.False(true, string.Format("Error, MoveNext returned false"));
            }
            curr = (DictionaryEntry)en.Current;
            de = en.Entry;
            k = en.Key;
            v = en.Value;
            int cnt = hd.Count;
            hd.Remove(keysShort[0]);
            if (hd.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove item with 0th key"));
            }

            // will return just removed item
            DictionaryEntry curr2 = (DictionaryEntry)en.Current;
            if (!curr.Equals(curr2))
            {
                Assert.False(true, string.Format("Error, current returned different value after midification"));
            }

            // will return just removed item
            DictionaryEntry de2 = en.Entry;
            if (!de.Equals(de2))
            {
                Assert.False(true, string.Format("Error, Entry returned different value after midification"));
            }

            // will return just removed item
            Object k2 = en.Key;
            if (!k.Equals(k2))
            {
                Assert.False(true, string.Format("Error, Key returned different value after midification"));
            }


            // will return just removed item
            Object v2 = en.Value;
            if (!v.Equals(v2))
            {
                Assert.False(true, string.Format("Error, Value returned different value after midification"));
            }

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { res = en.MoveNext(); });

            // ==========================================================
            //
            //
            //  [] Enumerator and long modified HD (hashtable)
            //
            hd.Clear();
            if (hd.Count < 1)
            {
                for (int i = 0; i < valuesLong.Length; i++)
                {
                    hd.Add(keysLong[i], valuesLong[i]);
                }
            }

            en = hd.GetEnumerator();
            res = en.MoveNext();
            if (!res)
            {
                Assert.False(true, string.Format("Error, MoveNext returned false"));
            }
            curr = (DictionaryEntry)en.Current;
            de = en.Entry;
            k = en.Key;
            v = en.Value;
            cnt = hd.Count;
            hd.Remove(keysLong[0]);
            if (hd.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove item with 0th key"));
            }

            // will return just removed item
            DictionaryEntry curr3 = (DictionaryEntry)en.Current;
            if (!curr.Equals(curr3))
            {
                Assert.False(true, string.Format("Error, current returned different value after midification"));
            }

            // will return just removed item

            DictionaryEntry de3 = en.Entry;
            if (!de.Equals(de3))
            {
                Assert.False(true, string.Format("Error, Entry returned different value after midification"));
            }


            // will return just removed item
            Object k3 = en.Key;
            if (!k.Equals(k3))
            {
                Assert.False(true, string.Format("Error, Key returned different value after midification"));
            }

            // will return just removed item
            Object v3 = en.Value;
            if (!v.Equals(v3))
            {
                Assert.False(true, string.Format("Error, Value returned different value after midification"));
            }

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { res = en.MoveNext(); });

            //
            //   Modify collection when enumerated beyond the end
            //
            //  [] Enumerator and short HD (list) modified after enumerating beyond the end
            //
            hd.Clear();
            for (int i = 0; i < valuesShort.Length; i++)
            {
                hd.Add(keysShort[i], valuesShort[i]);
            }

            en = hd.GetEnumerator();
            for (int i = 0; i < hd.Count; i++)
            {
                en.MoveNext();
            }
            curr = (DictionaryEntry)en.Current;
            de = en.Entry;
            k = en.Key;
            v = en.Value;

            cnt = hd.Count;
            hd.Remove(keysShort[0]);
            if (hd.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove item with 0th key"));
            }

            // will return just removed item
            DictionaryEntry curr4 = (DictionaryEntry)en.Current;
            if (!curr.Equals(curr4))
            {
                Assert.False(true, string.Format("Error, current returned different value after midification"));
            }

            // will return just removed item
            DictionaryEntry de4 = en.Entry;
            if (!de.Equals(de4))
            {
                Assert.False(true, string.Format("Error, Entry returned different value after midification"));
            }

            // will return just removed item
            Object k4 = en.Key;
            if (!k.Equals(k4))
            {
                Assert.False(true, string.Format("Error, Key returned different value after midification"));
            }

            // will return just removed item
            Object v4 = en.Value;
            if (!v.Equals(v4))
            {
                Assert.False(true, string.Format("Error, Value returned different value after midification"));
            }

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { res = en.MoveNext(); });

            //
            //  [] Enumerator and long HD (hashtable) modified after enumerating beyond the end

            hd.Clear();
            for (int i = 0; i < valuesLong.Length; i++)
            {
                hd.Add(keysLong[i], valuesLong[i]);
            }

            en = hd.GetEnumerator();
            for (int i = 0; i < hd.Count; i++)
            {
                en.MoveNext();
            }
            curr = (DictionaryEntry)en.Current;
            de = en.Entry;
            k = en.Key;
            v = en.Value;

            cnt = hd.Count;
            hd.Remove(keysLong[0]);
            if (hd.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove item with 0th key"));
            }

            // will return just removed item

            DictionaryEntry curr5 = (DictionaryEntry)en.Current;
            if (!curr.Equals(curr5))
            {
                Assert.False(true, string.Format("Error, current returned different value after midification"));
            }


            // will return just removed item

            DictionaryEntry de5 = en.Entry;
            if (!de.Equals(de5))
            {
                Assert.False(true, string.Format("Error, Entry returned different value after midification"));
            }


            // will return just removed item
            Object k5 = en.Key;
            if (!k.Equals(k5))
            {
                Assert.False(true, string.Format("Error, Key returned different value after midification"));
            }


            // will return just removed item

            Object v5 = en.Value;
            if (!v.Equals(v5))
            {
                Assert.False(true, string.Format("Error, Value returned different value after midification"));
            }


            // exception expected
            Assert.Throws<InvalidOperationException>(() => { res = en.MoveNext(); });

            //  ---------------------------------------------------------
            //   [] Enumerator for empty case-insensitive dictionary
            //  ---------------------------------------------------------
            //
            hd = new HybridDictionary(true);

            en = hd.GetEnumerator();
            type = en.GetType().ToString();
            if (type.IndexOf("Enumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not Enumerator"));
            }
        }
    }
}
