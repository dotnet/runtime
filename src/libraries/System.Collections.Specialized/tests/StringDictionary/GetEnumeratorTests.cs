// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetEnumeratorStringDictionaryTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            StringDictionary sd;
            IEnumerator en;
            DictionaryEntry curr;        // Enumerator.Current value
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

            // [] StringDictionary GetEnumerator()
            //-----------------------------------------------------------------

            sd = new StringDictionary();

            // [] Enumerator for empty dictionary
            //
            en = sd.GetEnumerator();
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
            //   Filled collection
            // [] Enumerator for filled dictionary
            //
            for (int i = 0; i < values.Length; i++)
            {
                sd.Add(keys[i], values[i]);
            }

            en = sd.GetEnumerator();
            type = en.GetType().ToString();
            if (type.IndexOf("Enumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not Enumerator"));
            }

            //
            //  MoveNext should return true
            //

            for (int i = 0; i < sd.Count; i++)
            {
                res = en.MoveNext();
                if (!res)
                {
                    Assert.False(true, string.Format("Error, MoveNext returned false", i));
                }

                curr = (DictionaryEntry)en.Current;
                //
                //enumerator enumerates in different than added order
                // so we'll check Contains
                //
                if (!sd.ContainsValue(curr.Value.ToString()))
                {
                    Assert.False(true, string.Format("Error, Current dictionary doesn't contain value from enumerator", i));
                }
                if (!sd.ContainsKey(curr.Key.ToString()))
                {
                    Assert.False(true, string.Format("Error, Current dictionary doesn't contain key from enumerator", i));
                }
                if (String.Compare(sd[curr.Key.ToString()], curr.Value.ToString()) != 0)
                {
                    Assert.False(true, string.Format("Error, Value for current Key is different in dictionary", i));
                }

                // while we didn't MoveNext, Current should return the same value
                DictionaryEntry curr1 = (DictionaryEntry)en.Current;
                if (!curr.Equals(curr1))
                {
                    Assert.False(true, string.Format("Error, second call of Current returned different result", i));
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
            en.Reset();

            //
            //  Attempt to get Current should result in exception
            //
            Assert.Throws<InvalidOperationException>(() => { curr = (DictionaryEntry)en.Current; });

            //
            // [] Modify dictionary when enumerating
            //
            if (sd.Count < 1)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    sd.Add(keys[i], values[i]);
                }
            }

            en = sd.GetEnumerator();
            res = en.MoveNext();
            if (!res)
            {
                Assert.False(true, string.Format("Error, MoveNext returned false"));
            }
            curr = (DictionaryEntry)en.Current;
            int cnt = sd.Count;
            sd.Remove(keys[0]);
            if (sd.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove item with 0th key"));
            }

            // will return just removed item
            DictionaryEntry curr2 = (DictionaryEntry)en.Current;
            if (!curr.Equals(curr2))
            {
                Assert.False(true, string.Format("Error, current returned different value after midification"));
            }

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { res = en.MoveNext(); });

            //
            // [] Modify dictionary when enumerated beyond the end
            //
            sd.Clear();
            for (int i = 0; i < values.Length; i++)
            {
                sd.Add(keys[i], values[i]);
            }

            en = sd.GetEnumerator();
            for (int i = 0; i < sd.Count; i++)
            {
                en.MoveNext();
            }
            curr = (DictionaryEntry)en.Current;

            curr = (DictionaryEntry)en.Current;
            cnt = sd.Count;
            sd.Remove(keys[0]);
            if (sd.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove item with 0th key"));
            }

            // will return just removed item
            curr2 = (DictionaryEntry)en.Current;
            if (!curr.Equals(curr2))
            {
                Assert.False(true, string.Format("Error, current returned different value after midification"));
            }

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { res = en.MoveNext(); });
        }
    }
}
