// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class OrderByDescendingTests
    {
        // Class which is passed as an argument for EqualityComparer
        public class CaseInsensitiveComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return string.Compare(x.ToLower(), y.ToLower());
            }
        }

        // EqualityComparer to ignore case sensitivity: Added to support PLINQ
        public class CaseInsensitiveEqualityComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                if (string.Compare(x.ToLower(), y.ToLower()) == 0) return true;
                return false;
            }

            public int GetHashCode(string obj)
            {
                return getCaseInsensitiveString(obj).GetHashCode();
            }

            private string getCaseInsensitiveString(string word)
            {
                char[] wordchars = word.ToCharArray();
                String newWord = "";
                foreach (char c in wordchars)
                {
                    newWord = newWord + (c.ToString().ToLower());
                }
                return newWord;
            }
        }

        private struct Record
        {
#pragma warning disable 0649
            public string Name;
            public int Score;
#pragma warning restore 0649

        }

        public class OrderByDescending009
        {
            private static int OrderByDescending001()
            {
                var q = from x1 in new int[] { 1, 6, 0, -1, 3 }
                        from x2 in new int[] { 55, 49, 9, -100, 24, 25 }
                        select new { a1 = x1, a2 = x2 };

                var rst1 = q.OrderByDescending(e => e.a1);
                var rst2 = q.OrderByDescending(e => e.a1);

                return Verification.Allequal(rst1, rst2);
            }

            private static int OrderByDescending002()
            {
                var q = from x1 in new[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                        from x2 in new[] { "!@#$%^", "C", "AAA", "", null, "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x2)
                        select new { a1 = x1, a2 = x2 };

                var rst1 = q.OrderByDescending(e => e.a1).ThenBy(f => f.a2);
                var rst2 = q.OrderByDescending(e => e.a1).ThenBy(f => f.a2);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(OrderByDescending001) + RunTest(OrderByDescending002);
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

        public class OrderByDescending1
        {
            // Overload-1: source is empty
            public static int Test1()
            {
                int[] source = { };
                int[] expected = { };

                var actual = source.OrderByDescending((e) => e);

                return Verification.Allequal(expected, actual);
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

        public class OrderByDescending2
        {
            // Overload-1: keySelector returns null
            public static int Test2()
            {
                int?[] source = { null, null, null };
                int?[] expected = { null, null, null };

                var actual = source.OrderByDescending((e) => e);

                return Verification.Allequal(expected, actual);
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

        public class OrderByDescending3
        {
            // Overload-1: All elements have the same key
            public static int Test3()
            {
                int?[] source = { 9, 9, 9, 9, 9, 9 };
                int?[] expected = { 9, 9, 9, 9, 9, 9 };

                var actual = source.OrderByDescending((e) => e);

                return Verification.Allequal(expected, actual);
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

        public class OrderByDescending4
        {
            // Overload-2: All elements have different keys.
            // Verify keySelector function is called.
            public static int Test4()
            {
                Record[] source = new Record[]{ new Record{Name = "Alpha", Score = 90},
                                            new Record{Name = "Robert", Score = 45},
                                            new Record{Name = "Prakash", Score = 99},
                                            new Record{ Name = "Bob", Score = 0}
            };
                Record[] expected = new Record[]{new Record{Name = "Robert", Score = 45},
                                             new Record{Name = "Prakash", Score = 99},
                                             new Record{Name = "Bob", Score = 0},
                                             new Record{Name = "Alpha", Score = 90}
            };

                var actual = source.OrderByDescending((e) => e.Name, null);

                return Verification.Allequal(expected, actual);
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

        public class OrderByDescending5
        {
            // Overload-2: 1st and last elements have same key and duplicate elements.
            // Verify the given comparer function is called. Also verifies order is preserved
            // This test calls AllequalCompaer with a EqualityComparer which is casInsensitive. This change is to help PLINQ team run our tests
            public static int Test5()
            {
                string[] source = { "Prakash", "Alpha", "DAN", "dan", "Prakash" };
                string[] expected = { "Prakash", "Prakash", "DAN", "dan", "Alpha" };

                var actual = source.OrderByDescending((e) => e, new CaseInsensitiveComparer());

                return Verification.AllequalComparer(expected, actual, new CaseInsensitiveEqualityComparer());
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

        public class OrderByDescending6
        {
            // Overload-2: 1st and last elements have same key and duplicate elements.
            // Verify default comparer is called.
            public static int Test6()
            {
                string[] source = { "Prakash", "Alpha", "DAN", "dan", "Prakash" };
                string[] expected = { "Prakash", "Prakash", "DAN", "dan", "Alpha" };

                var actual = source.OrderByDescending((e) => e, null);

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test6();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class OrderByDescending7
        {
            // Overload-2: Elements are in ascending order
            public static int Test7()
            {
                int[] source = { -75, -50, 0, 5, 9, 30, 100 };
                int[] expected = { 100, 30, 9, 5, 0, -50, -75 };

                var actual = source.OrderByDescending((e) => e, null);

                return Verification.Allequal(expected, actual);
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

        public class OrderByDescending8
        {
            // Overload-2: Verify Order is preserved
            public static int Test8()
            {
                Record[] source = new Record[]{ new Record{Name = "Alpha", Score = 90},
                                            new Record{Name = "Robert", Score = 45},
                                            new Record{Name = "Prakash", Score = 99},
                                            new Record{ Name = "Bob", Score = 90},
                                            new Record{Name = "Thomas", Score = 45},
                                            new Record{Name = "Tim", Score = 45},
                                            new Record{Name = "Mark", Score = 45},
            };
                Record[] expected = new Record[]{new Record{Name = "Prakash", Score = 99},
                                             new Record{Name = "Alpha", Score = 90},
                                             new Record{ Name = "Bob", Score = 90},
                                             new Record{Name = "Robert", Score = 45},
                                             new Record{Name = "Thomas", Score = 45},
                                             new Record{Name = "Tim", Score = 45},
                                             new Record{Name = "Mark", Score = 45},
            };

                var actual = source.Select((e, i) => new { V = e, I = i }).OrderByDescending((e) => e.V.Score).ThenBy((e) => e.I).Select((e) => e.V);
                //var actual = source.OrderByDescending((e) => e.Score, null);

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test8();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}