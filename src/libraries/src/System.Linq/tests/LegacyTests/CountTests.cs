// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class CountTests
    {
        public class Count007
        {
            private static int Count001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Count<int>();
                var rst2 = q.Count<int>();

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Count002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.Count<string>();
                var rst2 = q.Count<string>();

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(Count001) + RunTest(Count002);
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

        public class Count1a
        {
            // source implements ICollection<T> and source is empty
            public static int Test1a()
            {
                int[] data = { };
                int expected = 0;

                var actual = data.Count();
                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Count1b
        {
            // source is empty
            public static int Test1b()
            {
                int[] data = { };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 0;

                var actual = data.Count(predicate);
                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Count2a
        {
            // source implements ICollection<T> and source is empty
            public static int Test2a()
            {
                int?[] data = { -10, 4, 9, null, 11 };
                int expected = 5;

                var actual = data.Count();
                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Count2b
        {
            // source has one element and predicate is true
            public static int Test2b()
            {
                int[] data = { 4 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 1;

                var actual = data.Count(predicate);
                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Count3a
        {
            // source does NOT implement ICollection<T> and source is empty
            public static int Test3a()
            {
                IEnumerable<int> data = Functions.NumRange(0, 0);
                int expected = 0;

                var actual = data.Count();
                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test3a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Count3b
        {
            // source has one element and predicate is false
            public static int Test3b()
            {
                int[] data = { 5 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 0;

                var actual = data.Count(predicate);
                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test3b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Count4a
        {
            // source does NOT implement ICollection<T> and source has one element
            public static int Test4a()
            {
                IEnumerable<int> data = Functions.NumRange(5, 1);
                int expected = 1;

                var actual = data.Count();
                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test4a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Count4b
        {
            // source has limited number of elements and predicate true for 1st and last element
            public static int Test4b()
            {
                int[] data = { 2, 5, 7, 9, 29, 10 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 2;

                var actual = data.Count(predicate);
                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test4b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Count5a
        {
            // source does NOT implement ICollection<T> and source has > 1 elements
            public static int Test5a()
            {
                IEnumerable<int> data = Functions.NumRange(5, 10);
                int expected = 10;

                var actual = data.Count();
                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Count5b
        {
            // source has limited number of elements and predicate true for all the elements
            public static int Test5b()
            {
                int[] data = { 2, 20, 22, 100, 50, 10 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 6;

                var actual = data.Count(predicate);
                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}