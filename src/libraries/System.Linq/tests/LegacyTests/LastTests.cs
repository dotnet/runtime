// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class LastTests
    {
        public class Last003
        {
            private static int Last001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Last<int>();
                var rst2 = q.Last<int>();
                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Last002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.Last<string>();
                var rst2 = q.Last<string>();

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(Last001) + RunTest(Last002);
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

        public class Last1a
        {
            // source is of type IList, source is empty
            public static int Test1a()
            {
                int[] source = { };

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                try
                {
                    var actual = source.Last();
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
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

        public class Last1b
        {
            // source is of type IList, source has one element
            public static int Test1b()
            {
                int[] source = { 5 };
                int expected = 5;

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                var actual = source.Last();

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

        public class Last1c
        {
            // source is of type IList, source has > 1 element
            public static int Test1c()
            {
                int?[] source = { -10, 2, 4, 3, 0, 2, null };
                int? expected = null;

                IList<int?> list = source as IList<int?>;

                if (list == null) return 1;

                var actual = source.Last();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Last1d
        {
            // source is NOT of type IList, source is empty
            public static int Test1d()
            {
                IEnumerable<int> source = Functions.NumList(0, 0);

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                try
                {
                    var actual = source.Last();
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test1d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Last1e
        {
            // source is NOT of type IList, source has one element
            public static int Test1e()
            {
                IEnumerable<int> source = Functions.NumList(-5, 1);
                int expected = -5;

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                var actual = source.Last();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Last1f
        {
            // source is NOT of type IList, source has > 1 element
            public static int Test1f()
            {
                IEnumerable<int> source = Functions.NumList(3, 10);
                int expected = 12;

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                var actual = source.Last();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Last2a
        {
            // source is empty
            public static int Test2a()
            {
                int[] source = { };
                Func<int, bool> predicate = Functions.IsEven;

                try
                {
                    var actual = source.Last(predicate);
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
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

        public class Last2b
        {
            // source has one element, predicate is true
            public static int Test2b()
            {
                int[] source = { 4 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 4;

                var actual = source.Last(predicate);

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

        public class Last2c
        {
            // source has > one element, predicate is false for all
            public static int Test2c()
            {
                int[] source = { 9, 5, 1, 3, 17, 21 };
                Func<int, bool> predicate = Functions.IsEven;

                try
                {
                    var actual = source.Last(predicate);
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test2c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Last2d
        {
            // source has > one element, predicate is true only for last element
            public static int Test2d()
            {
                int[] source = { 9, 5, 1, 3, 17, 21, 50 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 50;

                var actual = source.Last(predicate);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Last2e
        {
            // source has > one element, predicate is true for 3rd, 6th and 8th element
            public static int Test2e()
            {
                int[] source = { 3, 7, 10, 7, 9, 2, 11, 18, 13, 11 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 18;

                var actual = source.Last(predicate);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
