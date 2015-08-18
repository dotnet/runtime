// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ToLookupTests
    {
        // Class which is passed as an argument for EqualityComparer
        public class AnagramEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if ((x == null) && (y == null))
                    return true;

                if ((x == null) || (y == null))
                    return false;

                return getCanonicalString(x) == getCanonicalString(y);
            }

            public int GetHashCode(string obj)
            {
                return getCanonicalString(obj).GetHashCode();
            }

            private string getCanonicalString(string word)
            {
                char[] wordChars = word.ToCharArray();
                Array.Sort<char>(wordChars);
                return new string(wordChars);
            }
        }

        private struct Record
        {
            public string Name;
            public int Score;
        }

        public class Helper
        {
            // The following helper verification function will be used when the PLINQ team runs these tests
            // This is a non-order preserving verification function
#if PLINQ
        public static int MatchAll<K, T>(IEnumerable<K> key, IEnumerable<T> element, System.Linq.ILookup<K, T> lookup)
        {
            int num = 0;

            if ((lookup == null) &&(key == null) &&(element == null)) return 0;

            if ((lookup == null) || (key == null && element == null)) 
            {
                Console.WriteLine("expected key : {0}", key == null ? "null" : key.Count().ToString());
                Console.WriteLine("expected element : {0}", element == null ? "null" : element.Count().ToString());
                Console.WriteLine("actual lookup: {0}", lookup == null ? "null" : lookup.Count().ToString());
                return 1;
            }

            try
            {
                List<T> expectedResults = new List<T>(element);

                using (IEnumerator<K> k1 = key.GetEnumerator())
                using (IEnumerator<T> e1 = element.GetEnumerator())
                {
                    while (k1.MoveNext())
                    {
                        if (!lookup.Contains(k1.Current)) return 1;

                        foreach (T e in lookup[k1.Current])
                        {
                            if (!expectedResults.Contains(e)) return 1;
                            expectedResults.Remove(e);
                        }
                        num = num + 1;
                    }
                }

                if (expectedResults.Count != 0) return 1;
                if (lookup.Count != num) return 1;
                return 0;
            }
            catch(AggregateException ae)
            {
                var innerExceptions = ae.Flatten().InnerExceptions;
                if (innerExceptions.Where(ex => ex != null).Select(ex => ex.GetType()).Distinct().Count() == 1)
                {
                    throw innerExceptions.First();
                }
                else
                {
                    Console.WriteLine(ae);
                }
                return 1;
            }  
        }
#else
            // Helper function to verify that all elements in dictionary are matched (Order Preserving)
            public static int MatchAll<K, T>(IEnumerable<K> key, IEnumerable<T> element, System.Linq.ILookup<K, T> lookup)
            {
                int num = 0;

                if ((lookup == null) && (key == null) && (element == null)) return 0;
                using (IEnumerator<K> k1 = key.GetEnumerator())
                using (IEnumerator<T> e1 = element.GetEnumerator())
                {
                    while (k1.MoveNext())
                    {
                        if (!lookup.Contains(k1.Current)) return 1;

                        foreach (T e in lookup[k1.Current])
                        {
                            e1.MoveNext();
                            if (!Equals(e, e1.Current)) return 1;
                        }
                        num = num + 1;
                    }
                }
                if (lookup.Count != num) return 1;
                return 0;
            }
#endif
        }

        public class ToLookup006
        {
            private static int ToLookup001()
            {
                var q1 = from x1 in new string[] { "Alen", "Felix", null, null, "X", "Have Space", "Clinton", "" }
                         select x1; ;

                var q2 = from x2 in new int[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                         select x2;

                var q = from x3 in q1
                        from x4 in q2
                        select new { a1 = x3, a2 = x4 };

                var rst1 = q.ToLookup((e) => e.a1);
                var rst2 = q.ToLookup((e) => e.a1);

                return ((null == rst2) ? 1 : 0);
                // return Helper.MatchAll(q1, q2, q);
                // return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(ToLookup001);
                if (0 != ret)
                    Console.Write(s_errorMessage);

                return ret;
            }

            private static string s_errorMessage = String.Empty;
            private delegate int D();

            private static int RunTest(D m)
            {
                int n = m();
                if (0 != n)
                    s_errorMessage += m.ToString() + " - FAILED!\r\n";
                return n;
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToLookup1
        {
            // Overload-1: keySelector returns null.
            public static int Test1()
            {
                string[] key = { "Chris", "Bob", null, "Tim" };
                int[] element = { 50, 95, 55, 90 };
                Record[] source = new Record[4];

                source[0].Name = key[0]; source[0].Score = element[0];
                source[1].Name = key[1]; source[1].Score = element[1];
                source[2].Name = key[2]; source[2].Score = element[2];
                source[3].Name = key[3]; source[3].Score = element[3];

                var result = source.ToLookup((e) => e.Name);

                return Helper.MatchAll(key, source, result);
            }


            public static int Main()
            {
                return Test1();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToLookup2
        {
            // Overload-2: Only one element is present and given Comparer is used.
            public static int Test2()
            {
                string[] key = { "Chris" };
                int[] element = { 50 };
                Record[] source = new Record[1];

                // key is an anagram of Chris
                source[0].Name = "risCh"; source[0].Score = element[0];

                var result = source.ToLookup((e) => e.Name, new AnagramEqualityComparer());

                return Helper.MatchAll(key, source, result);
            }


            public static int Main()
            {
                return Test2();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToLookup3
        {
            // Overload-3: All elements are unique and elementSelector function is called.
            public static int Test3()
            {
                string[] key = { "Chris", "Prakash", "Tim", "Robert", "Brian" };
                int[] element = { 50, 100, 95, 60, 80 };
                Record[] source = new Record[5];

                source[0].Name = key[0]; source[0].Score = element[0];
                source[1].Name = key[1]; source[1].Score = element[1];
                source[2].Name = key[2]; source[2].Score = element[2];
                source[3].Name = key[3]; source[3].Score = element[3];
                source[4].Name = key[4]; source[4].Score = element[4];

                var result = source.ToLookup((e) => e.Name, (e) => e.Score);

                return Helper.MatchAll(key, element, result);
            }


            public static int Main()
            {
                return Test3();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToLookup4
        {
            // Overload-4: keySelector has duplicate values
            public static int Test4()
            {
                string[] key = { "Chris", "Prakash", "Robert" };
                int[] element = { 50, 80, 100, 95, 99, 56 };
                Record[] source = new Record[6];

                source[0].Name = key[0]; source[0].Score = element[0];
                source[1].Name = key[1]; source[1].Score = element[2];
                source[2].Name = key[2]; source[2].Score = element[5];
                source[3].Name = key[1]; source[3].Score = element[3];
                source[4].Name = key[0]; source[4].Score = element[1];
                source[5].Name = key[1]; source[5].Score = element[4];

                System.Linq.ILookup<string, int> result = source.ToLookup((e) => e.Name, (e) => e.Score, new AnagramEqualityComparer());

                return Helper.MatchAll(key, element, result);
            }


            public static int Main()
            {
                return Test4();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToLookup5
        {
            //Overload-4: source is empty
            public static int Test5()
            {
                string[] key = { };
                int[] element = { };
                Record[] source = new Record[] { };

                System.Linq.ILookup<string, int> result = source.ToLookup((e) => e.Name, (e) => e.Score, new AnagramEqualityComparer());

                return Helper.MatchAll(key, element, result);
            }


            public static int Main()
            {
                return Test5();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class ToLookup7
        {
            // DDB:171937
            public static int Test7()
            {
                string[] key = { null };
                string[] element = { null };
                string[] source = new string[] { null };

                System.Linq.ILookup<string, string> result = source.ToLookup((e) => e, (e) => e, EqualityComparer<string>.Default);

                return Helper.MatchAll(key, element, result);
            }


            public static int Main()
            {
                return Test7();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
