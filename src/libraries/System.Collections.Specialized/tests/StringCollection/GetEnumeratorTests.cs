// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetEnumeratorStringCollectionTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            StringCollection sc;
            StringEnumerator en;

            string curr;        // Eumerator.Current value

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

            // [] StringCollection GetEnumerator()
            //-----------------------------------------------------------------

            sc = new StringCollection();

            // [] Enumerator for empty collection
            //
            en = sc.GetEnumerator();
            string type = en.GetType().ToString();
            if (type.IndexOf("StringEnumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not StringEnumerator"));
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
            Assert.Throws<InvalidOperationException>(() => { curr = en.Current; });

            //
            //   Filled collection  
            // [] Enumerator for filled collection
            //
            sc.AddRange(values);

            en = sc.GetEnumerator();
            type = en.GetType().ToString();
            if (type.IndexOf("StringEnumerator", 0) == 0)
            {
                Assert.False(true, string.Format("Error, type is not StringEnumerator"));
            }

            //
            //  MoveNext should return true
            //

            for (int i = 0; i < sc.Count; i++)
            {
                res = en.MoveNext();
                if (!res)
                {
                    Assert.False(true, string.Format("Error, MoveNext returned false", i));
                }

                curr = en.Current;
                if (String.Compare(curr, sc[i]) != 0)
                {
                    Assert.False(true, string.Format("Error, Current returned \"{1}\" instead of \"{2}\"", i, curr, sc[i]));
                }
                // while we didn't MoveNext, Current should return the same value
                string curr1 = en.Current;
                if (String.Compare(curr, curr1) != 0)
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
            Assert.Throws<InvalidOperationException>(() => { curr = en.Current; });

            en.Reset();

            //
            //  Attempt to get Current should result in exception
            //
            Assert.Throws<InvalidOperationException>(() => { curr = en.Current; });

            //
            //  [] Modify collection when enumerating
            //
            if (sc.Count < 1)
                sc.AddRange(values);

            en = sc.GetEnumerator();
            res = en.MoveNext();
            if (!res)
            {
                Assert.False(true, string.Format("Error, MoveNext returned false"));
            }
            int cnt = sc.Count;
            sc.RemoveAt(0);
            if (sc.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove 0-item"));
            }

            // will return just removed item
            curr = en.Current;
            if (String.Compare(curr, values[0]) != 0)
            {
                Assert.False(true, string.Format("Error, current returned {0} instead of {1}", curr, values[0]));
            }

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { res = en.MoveNext(); });
        }
    }
}
