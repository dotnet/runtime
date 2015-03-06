// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class GetCurrentTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            StringCollection sc;
            StringEnumerator en;
            string curr;        // Enumerator.Current value
            bool res;           // boolean result of MoveNext()

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

            // [] StringEnumerator.Current
            //-----------------------------------------------------------------

            sc = new StringCollection();

            //
            // [] on Empty Collection
            //

            //
            //  Attempt to get Current should result in exception
            //
            en = sc.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => { curr = en.Current; });

            //
            // [] on Filled collection
            //
            sc.AddRange(values);
            en = sc.GetEnumerator();

            //
            //  Attempt to get Current before first MoveNext() should result in exception
            //
            Assert.Throws<InvalidOperationException>(() => { curr = en.Current; });

            //
            //   verify for all items of the collection
            //
            int ind = sc.Count;

            en.Reset();
            for (int i = 0; i < ind; i++)
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

            //
            // Move beyond the last item and try to get Current - exception expected
            //
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

            //
            // [] Modify collection when enumerating
            //
            if (sc.Count < 1)
                sc.AddRange(values);

            //
            // modify the collection and call CUrrent before first MoveNext()
            //
            en = sc.GetEnumerator();
            sc.RemoveAt(0);
            Assert.Throws<InvalidOperationException>(() => { curr = en.Current; });

            //
            // Enumerate to the middle item of the collection, modify the collection,
            //  and get Current
            //
            // create valid enumerator
            //
            en = sc.GetEnumerator();
            for (int i = 0; i < sc.Count / 2; i++)
            {
                res = en.MoveNext();
            }
            curr = en.Current;

            curr = en.Current;
            sc.RemoveAt(0);

            // should return previous current
            if (String.Compare(curr, en.Current) != 0)
            {
                Assert.False(true, string.Format("Error, current returned {0} instead of {1}", en.Current, curr));
            }

            // cal Current second time in a row
            // should return previous current
            if (String.Compare(curr, en.Current) != 0)
            {
                Assert.False(true, string.Format("Error, current returned {0} instead of {1}", en.Current, curr));
            }

            //
            // Enumerate to the last item of the collection, modify the collection,
            //  and get Current
            //
            // create valid enumerator
            //
            en = sc.GetEnumerator();
            for (int i = 0; i < sc.Count; i++)
            {
                res = en.MoveNext();
            }
            curr = en.Current;

            sc.RemoveAt(0);

            // no exception expected
            if (String.Compare(en.Current, curr) != 0)
            {
                Assert.False(true, string.Format("Error, no exception"));
            }

            //
            // [] Modify collection after enumerated beyond the end
            //
            if (sc.Count < 1)
                sc.AddRange(values);

            en = sc.GetEnumerator();
            for (int i = 0; i < sc.Count; i++)
            {
                res = en.MoveNext();
            }
            res = en.MoveNext();              // should be beyond the end
            if (res)
            {
                Assert.False(true, string.Format("Error, MoveNext returned true after moving beyond the end"));
            }

            int cnt = sc.Count;
            sc.RemoveAt(0);
            if (sc.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove 0-item"));
            }

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { curr = en.Current; });
        }
    }
}
