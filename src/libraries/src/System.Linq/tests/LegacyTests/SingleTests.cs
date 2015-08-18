// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class SingleTests
    {
        public class Single003
        {
            private static int Single001()
            {
                var q = from x in new[] { 999.9m }
                        select x;

                var rst1 = q.Single();
                var rst2 = q.Single();

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Single002()
            {
                var q = from x in new[] { "!@#$%^" }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.Single<string>();
                var rst2 = q.Single<string>();

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Single0003()
            {
                var q = from x in new[] { 0 }
                        select x;

                var rst1 = q.Single();
                var rst2 = q.Single();

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(Single001) + RunTest(Single002) + RunTest(Single0003);
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

        public class Single1a
        {
            // source is of type IList, source is empty
            public static int Test1a()
            {
                int[] source = { };

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                try
                {
                    var actual = source.Single();
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

        public class Single1b
        {
            // source is of type IList, source has only one element
            public static int Test1b()
            {
                int[] source = { 4 };
                int expected = 4;

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                var actual = source.Single();

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

        public class Single1c
        {
            // source is of type IList, source has > 1 element
            public static int Test1c()
            {
                int[] source = { 4, 4, 4, 4, 4 };

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                try
                {
                    var actual = source.Single();
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
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

        public class Single1d
        {
            // source is NOT of type IList, source is empty
            public static int Test1d()
            {
                IEnumerable<int> source = Functions.NumRange(0, 0);

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                try
                {
                    var actual = source.Single();
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

        public class Single1e
        {
            // source is NOT of type IList, source has only one element
            public static int Test1e()
            {
                IEnumerable<int> source = Functions.NumRange(-5, 1);
                int expected = -5;

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                var actual = source.Single();

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

        public class Single1f
        {
            // source is NOT of type IList, source has > 1 element
            public static int Test1f()
            {
                IEnumerable<int> source = Functions.NumRange(3, 5);

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                try
                {
                    var actual = source.Single();
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
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

        public class Single2a
        {
            // source is empty
            public static int Test2a()
            {
                int[] source = { };
                Func<int, bool> predicate = Functions.IsEven;

                try
                {
                    var actual = source.Single(predicate);
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

        public class Single2b
        {
            // source has 1 element and predicate is true
            public static int Test2b()
            {
                int[] source = { 4 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 4;

                var actual = source.Single(predicate);

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

        public class Single2c
        {
            // source has 1 element and predicate is false
            public static int Test2c()
            {
                int[] source = { 3 };
                Func<int, bool> predicate = Functions.IsEven;

                try
                {
                    var actual = source.Single(predicate);
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

        public class Single2d
        {
            // source has > 1 element and predicate is false for all
            public static int Test2d()
            {
                int[] source = { 3, 1, 7, 9, 13, 19 };
                Func<int, bool> predicate = Functions.IsEven;

                try
                {
                    var actual = source.Single(predicate);
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
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

        public class Single2e
        {
            // source has > 1 element and predicate is true only for last element
            public static int Test2e()
            {
                int[] source = { 3, 1, 7, 9, 13, 19, 20 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 20;

                var actual = source.Single(predicate);

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

        public class Single2f
        {
            // source has > 1 element and predicate is true for 1st and last element
            public static int Test2f()
            {
                int[] source = { 2, 3, 1, 7, 9, 13, 19, 10 };
                Func<int, bool> predicate = Functions.IsEven;

                try
                {
                    var actual = source.Single(predicate);
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test2f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
