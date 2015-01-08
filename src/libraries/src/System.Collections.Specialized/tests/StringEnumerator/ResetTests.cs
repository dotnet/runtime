// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class ResetTests
    {
        public const int MAX_LEN = 50;          // max length of random strings

        [Fact]
        public void Test01()
        {
            StringCollection sc;
            StringEnumerator en;
            string curr;        // Eumerator.Current value
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

            // [] StringEnumerator.Reset()
            //-----------------------------------------------------------------

            sc = new StringCollection();

            //
            // [] on Empty Collection
            //

            //
            //  no exception
            //
            en = sc.GetEnumerator();
            en.Reset();

            //
            //  Attempt to get Current should result in exception
            //
            Assert.Throws<InvalidOperationException>(() => { curr = en.Current; });

            //
            // [] Add item to collection and Reset()
            //
            int cnt = sc.Count;
            sc.Add(values[0]);
            if (sc.Count != 1)
            {
                Assert.False(true, string.Format("Error, failed to add item"));
            }

            Assert.Throws<InvalidOperationException>(() => { en.Reset(); });

            //
            // [] on Filled collection
            //
            sc.AddRange(values);
            en = sc.GetEnumerator();

            //
            //  Reset() should not result in any exceptions
            //
            en.Reset();
            en.Reset();

            //
            // [] Move to 0th item and Reset()
            //
            if (!en.MoveNext())
            {
                Assert.False(true, string.Format("Error, MoveNext() returned false"));
            }
            curr = en.Current;
            if (String.Compare(curr, values[0]) != 0)
            {
                Assert.False(true, string.Format("Error, Current returned wrong value"));
            }

            // Reset() and repeat two checks
            en.Reset();
            if (!en.MoveNext())
            {
                Assert.False(true, string.Format("Error, MoveNext() returned false"));
            }
            if (String.Compare(en.Current, curr) != 0)
            {
                Assert.False(true, string.Format("Error, Current returned wrong value"));
            }


            //
            // [] Move to Count/2 item and Reset()
            //
            int ind = sc.Count / 2;

            en.Reset();
            for (int i = 0; i < ind + 1; i++)
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

            // Reset() and check 0th item
            en.Reset();
            if (!en.MoveNext())
            {
                Assert.False(true, string.Format("Error, MoveNext() returned false"));
            }
            if (String.Compare(en.Current, sc[0]) != 0)
            {
                Assert.False(true, string.Format("Error, Current returned wrong value"));
            }

            //
            // [] Move to the last item and Reset()
            //
            ind = sc.Count;

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

            // Reset() and check 0th item
            en.Reset();
            if (!en.MoveNext())
            {
                Assert.False(true, string.Format("Error, MoveNext() returned false"));
            }
            if (String.Compare(en.Current, sc[0]) != 0)
            {
                Assert.False(true, string.Format("Error, Current returned wrong value"));
            }

            //
            // [] Move beyond the last item and Reset()
            //
            en.Reset();
            for (int i = 0; i < ind; i++)
            {
                res = en.MoveNext();
            }
            // next MoveNext should bring us outside of the collection
            //
            res = en.MoveNext();
            res = en.MoveNext();
            if (res)
            {
                Assert.False(true, string.Format("Error, MoveNext returned true"));
            }
            // Reset() and check 0th item
            en.Reset();
            if (!en.MoveNext())
            {
                Assert.False(true, string.Format("Error, MoveNext() returned false"));
            }
            if (String.Compare(en.Current, sc[0]) != 0)
            {
                Assert.False(true, string.Format("Error, Current returned wrong value"));
            }

            //
            //  Attempt to get Current should result in exception
            //
            en.Reset();
            Assert.Throws<InvalidOperationException>(() => { curr = en.Current; });

            //
            // [] Modify collection when enumerating
            //
            if (sc.Count < 1)
                sc.AddRange(values);

            //
            // modify the collection and call Reset() before first MoveNext()
            //
            en = sc.GetEnumerator();
            sc.RemoveAt(0);
            Assert.Throws<InvalidOperationException>(() => { en.Reset(); });

            //
            // Enumerate to the middle item of the collection, modify the collection,
            //  and call Reset()
            //
            // create valid enumerator
            //
            en = sc.GetEnumerator();
            for (int i = 0; i < sc.Count / 2; i++)
            {
                res = en.MoveNext();
            }
            curr = en.Current;
            sc.RemoveAt(0);

            // will return previous current
            if (String.Compare(curr, en.Current) != 0)
            {
                Assert.False(true, string.Format("Error, current returned {0} instead of {1}", en.Current, curr));
            }

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { en.Reset(); });

            //
            // Enumerate to the last item of the collection, modify the collection,
            //  and call Reset()
            //
            // create valid enumerator
            //
            en = sc.GetEnumerator();
            for (int i = 0; i < sc.Count; i++)
            {
                res = en.MoveNext();
            }
            sc.RemoveAt(0);

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { en.Reset(); });

            //
            // [] Modify collection after enumerating beyond the end
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

            cnt = sc.Count;
            sc.RemoveAt(0);
            if (sc.Count != cnt - 1)
            {
                Assert.False(true, string.Format("Error, didn't remove 0-item"));
            }

            // exception expected
            Assert.Throws<InvalidOperationException>(() => { en.Reset(); });
        }
    }
}
