// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class SingleOrDefaultTests
    {
        public class SingleOrDefault003
        {
            private static int SingleOrDefault001()
            {
                var q = from x in new[] { 0.12335f }
                        select x;

                var rst1 = q.SingleOrDefault();
                var rst2 = q.SingleOrDefault();

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int SingleOrDefault002()
            {
                var q = from x in new[] { "" }
                        select x;

                Func<string, bool> predicate = Functions.IsEmpty;
                var rst1 = q.SingleOrDefault(predicate);
                var rst2 = q.SingleOrDefault(predicate);

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(SingleOrDefault001) + RunTest(SingleOrDefault002);
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

        public class SingleOrDefault1a
        {
            // source is of type IList, source is empty
            public static int Test1a()
            {
                int?[] source = { };
                int? expected = null;

                IList<int?> list = source as IList<int?>;

                if (list == null) return 1;

                var actual = source.SingleOrDefault();

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

        public class SingleOrDefault1b
        {
            // source is of type IList, source has only one element
            public static int Test1b()
            {
                int[] source = { 4 };
                int expected = 4;

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                var actual = source.SingleOrDefault();

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

        public class SingleOrDefault1c
        {
            // source is of type IList, source has > 1 element
            public static int Test1c()
            {
                int[] source = { 4, 4, 4, 4, 4 };

                IList<int> list = source as IList<int>;

                if (list == null) return 1;

                try
                {
                    var actual = source.SingleOrDefault();
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

        public class SingleOrDefault1d
        {
            // source is NOT of type IList, source is empty
            public static int Test1d()
            {
                IEnumerable<int> source = Functions.NumRange(0, 0);
                int expected = default(int);

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                var actual = source.SingleOrDefault();

                return ((expected == actual) ? 0 : 1);
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

        public class SingleOrDefault1e
        {
            // source is NOT of type IList, source has only one element
            public static int Test1e()
            {
                IEnumerable<int> source = Functions.NumRange(-5, 1);
                int expected = -5;

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                var actual = source.SingleOrDefault();

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

        public class SingleOrDefault1f
        {
            // source is NOT of type IList, source has > 1 element
            public static int Test1f()
            {
                IEnumerable<int> source = Functions.NumRange(3, 5);

                IList<int> list = source as IList<int>;

                if (list != null) return 1;

                try
                {
                    var actual = source.SingleOrDefault();
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

        public class SingleOrDefault2a
        {
            // source is empty
            public static int Test2a()
            {
                int[] source = { };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = default(int);

                var actual = source.SingleOrDefault(predicate);

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

        public class SingleOrDefault2b
        {
            // source has 1 element and predicate is true
            public static int Test2b()
            {
                int[] source = { 4 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 4;

                var actual = source.SingleOrDefault(predicate);

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

        public class SingleOrDefault2c
        {
            // source has 1 element and predicate is false
            public static int Test2c()
            {
                int[] source = { 3 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = default(int);

                var actual = source.SingleOrDefault(predicate);

                return ((expected == actual) ? 0 : 1);
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

        public class SingleOrDefault2d
        {
            // source has > 1 element and predicate is false for all
            public static int Test2d()
            {
                int[] source = { 3, 1, 7, 9, 13, 19 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = default(int);

                var actual = source.SingleOrDefault(predicate);

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

        public class SingleOrDefault2e
        {
            // source has > 1 element and predicate is true only for last element
            public static int Test2e()
            {
                int[] source = { 3, 1, 7, 9, 13, 19, 20 };
                Func<int, bool> predicate = Functions.IsEven;
                int expected = 20;

                var actual = source.SingleOrDefault(predicate);

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

        public class SingleOrDefault2f
        {
            // source has > 1 element and predicate is true for 1st and 5th element
            public static int Test2f()
            {
                int[] source = { 2, 3, 1, 7, 10, 13, 19, 9 };
                Func<int, bool> predicate = Functions.IsEven;

                try
                {
                    var actual = source.SingleOrDefault(predicate);
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
