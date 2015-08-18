// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class OrderByTests
    {
        // Class which is passed as an argument for Comparer
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

        // Class to test a bad comparer: DDB: 48535
        public class BadComparer1 : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return 1;
            }
        }

        // Class to test a bad comparer: DDB: 48535
        public class BadComparer2 : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                return -1;
            }
        }

        private struct Record
        {
#pragma warning disable 0649

            public string Name;
            public int Score;
#pragma warning restore 0649

        }

        public class OrderBy011
        {
            private static int OrderBy001()
            {
                var q = from x1 in new int[] { 1, 6, 0, -1, 3 }
                        from x2 in new int[] { 55, 49, 9, -100, 24, 25 }
                        select new { a1 = x1, a2 = x2 };

                var rst1 = q.OrderBy(e => e.a1).ThenBy(f => f.a2);
                var rst2 = q.OrderBy(e => e.a1).ThenBy(f => f.a2);

                return Verification.Allequal(rst1, rst2);
            }

            private static int OrderBy002()
            {
                var q = from x1 in new[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                        from x2 in new[] { "!@#$%^", "C", "AAA", "", null, "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x2)
                        select new { a1 = x1, a2 = x2 };

                var rst1 = q.OrderBy(e => e.a1);
                var rst2 = q.OrderBy(e => e.a1);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(OrderBy001) + RunTest(OrderBy002);
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

        public class OrderBy1
        {
            // Overload-1: source is empty
            public static int Test1()
            {
                int[] source = { };
                int[] expected = { };

                var actual = source.OrderBy((e) => e);

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

        public class OrderBy10
        {
            // Overload-2: Test to verify that the QuickSort function handles underflow OutOfBounds Exception
            // DDB: 48535
            public static int Test10()
            {
                int[] source = { 1 };
                int[] expected = { 1 };

                var actual = source.OrderBy((e) => e, new BadComparer2());

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test10();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class OrderBy2
        {
            // Overload-1: keySelector returns null
            public static int Test2()
            {
                int?[] source = { null, null, null };
                int?[] expected = { null, null, null };

                var actual = source.OrderBy((e) => e);

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

        public class OrderBy3
        {
            // Overload-1: All elements have the same key
            public static int Test3()
            {
                int?[] source = { 9, 9, 9, 9, 9, 9 };
                int?[] expected = { 9, 9, 9, 9, 9, 9 };

                var actual = source.OrderBy((e) => e);

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

        public class OrderBy4
        {
            // Overload-2: All elements have different keys.
            // Verify keySelector function is called.
            public static int Test4()
            {
                Record[] source = new Record[]{ new Record{Name = "Tim", Score = 90},
                                            new Record{Name = "Robert", Score = 45},
                                            new Record{Name = "Prakash", Score = 99}
            };
                Record[] expected = new Record[]{new Record{Name = "Prakash", Score = 99},
                                             new Record{Name = "Robert", Score = 45},
                                             new Record{Name = "Tim", Score = 90}
            };

                var actual = source.OrderBy((e) => e.Name, null);

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

        public class OrderBy5
        {
            // Overload-2: 1st and last elements have same key and duplicate elements.
            // Verify the given comparer function is called.
            // This test calls AllequalCompaer with a EqualityComparer which is casInsensitive. This change is to help PLINQ team run our tests
            public static int Test5()
            {
                string[] source = { "Prakash", "Alpha", "dan", "DAN", "Prakash" };
                string[] expected = { "Alpha", "dan", "DAN", "Prakash", "Prakash" };

                var actual = source.OrderBy((e) => e, new CaseInsensitiveComparer());

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

        public class OrderBy6
        {
            // Overload-2: 1st and last elements have same key and duplicate elements.
            // Verify default comparer is called. (Also verifies that Order is preserved)
            public static int Test6()
            {
                string[] source = { "Prakash", "Alpha", "dan", "DAN", "Prakash" };
                string[] expected = { "Alpha", "dan", "DAN", "Prakash", "Prakash" };

                var actual = source.OrderBy((e) => e, null);

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

        public class OrderBy7
        {
            // Overload-2: Elements are in descending order
            public static int Test7()
            {
                int?[] source = { 100, 30, 9, 5, 0, -50, -75, null };
                int?[] expected = { null, -75, -50, 0, 5, 9, 30, 100 };

                var actual = source.OrderBy((e) => e, null);

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

        public class OrderBy8
        {
            // Overload-2: All elements have same keys, verify Order is preserved
            public static int Test8()
            {
                Record[] source = new Record[]{ new Record{Name = "Tim", Score = 90},
                                            new Record{Name = "Robert", Score = 90},
                                            new Record{Name = "Prakash", Score = 90},
                                            new Record{Name = "Jim", Score = 90},
                                            new Record{Name = "John", Score = 90},
                                            new Record{Name = "Albert", Score = 90},
            };
                Record[] expected = new Record[]{new Record{Name = "Tim", Score = 90},
                                            new Record{Name = "Robert", Score = 90},
                                            new Record{Name = "Prakash", Score = 90},
                                            new Record{Name = "Jim", Score = 90},
                                            new Record{Name = "John", Score = 90},
                                            new Record{Name = "Albert", Score = 90},
            };

                var actual = source.Select((e, i) => new { V = e, I = i }).OrderBy((e) => e.V.Score).ThenBy((e) => e.I).Select((e) => e.V);
                //var actual = source.OrderBy((e) => e.Score, null);

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

        public class OrderBy9
        {
            // Overload-2: Test to verify that the QuickSort function handles overflow OutOfBounds Exception
            // DDB: 48535
            public static int Test9()
            {
                int[] source = { 1 };
                int[] expected = { 1 };

                var actual = source.OrderBy((e) => e, new BadComparer1());

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test9();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
