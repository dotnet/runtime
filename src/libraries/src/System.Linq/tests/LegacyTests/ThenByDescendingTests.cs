// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ThenByDescendingTests
    {
        // Class which is passed as an argument for EqualityComparer
        public class CaseInsensitiveComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return string.Compare(x.ToLower(), y.ToLower());
            }
        }

        private struct Record
        {
#pragma warning disable 0649
            public string Name;
            public string City;
            public string Country;
#pragma warning restore 0649
        }
        public class ThenByDescending004
        {
            private static int ThenByDescending001()
            {
                var q = from x1 in new int[] { 1, 6, 0, -1, 3 }
                        from x2 in new int[] { 55, 49, 9, -100, 24, 25 }
                        select new { a1 = x1, a2 = x2 };

                var rst1 = q.OrderByDescending(e => e.a2).ThenByDescending(f => f.a1);
                var rst2 = q.OrderByDescending(e => e.a2).ThenByDescending(f => f.a1);

                return Verification.Allequal(rst1, rst2);
            }

            private static int ThenByDescending002()
            {
                var q = from x1 in new[] { 55, 49, 9, -100, 24, 25, -1, 0 }
                        from x2 in new[] { "!@#$%^", "C", "AAA", "", null, "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x2)
                        select new { a1 = x1, a2 = x2 };

                var rst1 = q.OrderBy(e => e.a1).ThenByDescending(f => f.a2);
                var rst2 = q.OrderBy(e => e.a1).ThenByDescending(f => f.a2);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(ThenByDescending001) + RunTest(ThenByDescending002);
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

        public class ThenByDescending1
        {
            // Overload-1: source is empty
            public static int Test1()
            {
                int[] source = { };
                int[] expected = { };

                var actual = source.OrderBy((e) => e).ThenByDescending((e) => (e));

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

        public class ThenByDescending2
        {
            // Overload-1: primary keys are in ascending order and secondary keys in descending
            public static int Test2()
            {
                Record[] source = new Record[]{
                               new Record{Name = "Jim", City = "Minneapolis", Country = "USA"},
                               new Record{Name = "Tim", City = "Seattle", Country = "USA"},
                               new Record{Name = "Philip", City = "Orlando", Country = "USA"},
                               new Record{Name = "Chris", City = "London", Country = "UK"},
                               new Record{Name = "Rob", City = "Kent", Country = "UK"}
            };
                Record[] expected = new Record[]{
                                 new Record{Name = "Chris", City = "London", Country = "UK"},
                                 new Record{Name = "Rob", City = "Kent", Country = "UK"},
                                 new Record{Name = "Tim", City = "Seattle", Country = "USA"},
                                 new Record{Name = "Philip", City = "Orlando", Country = "USA"},
                                 new Record{Name = "Jim", City = "Minneapolis", Country = "USA"}
            };

                var actual = source.OrderBy((e) => e.Country).ThenByDescending((e) => e.City);

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

        public class ThenByDescending3
        {
            // Overload-1: primary keys are in descending order and secondary keys in descending
            public static int Test3()
            {
                Record[] source = new Record[]{
                               new Record{Name = "Jim", City = "Minneapolis", Country = "USA"},
                               new Record{Name = "Tim", City = "Seattle", Country = "USA"},
                               new Record{Name = "Philip", City = "Orlando", Country = "USA"},
                               new Record{Name = "Chris", City = "London", Country = "UK"},
                               new Record{Name = "Rob", City = "Kent", Country = "UK"}
            };
                Record[] expected = new Record[]{
                                 new Record{Name = "Tim", City = "Seattle", Country = "USA"},
                                 new Record{Name = "Philip", City = "Orlando", Country = "USA"},
                                 new Record{Name = "Jim", City = "Minneapolis", Country = "USA"},
                                 new Record{Name = "Chris", City = "London", Country = "UK"},
                                 new Record{Name = "Rob", City = "Kent", Country = "UK"}
            };

                var actual = source.OrderByDescending((e) => e.Country).ThenByDescending((e) => e.City);

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
    }
}
