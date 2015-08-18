// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests.LegacyTests
{
    public class AggregateTests
    {
        public class Aggregate004
        {
            public static int Accumulate(int e1, int e2)
            {
                return e1 + e2;
            }

            public static string Accumulate(string s1, string s2)
            {
                return s1 + s2;
            }

            private static int Aggregate001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Aggregate(Accumulate);
                var rst2 = q.Aggregate(Accumulate);
                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Aggregate002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.Aggregate(Accumulate);
                var rst2 = q.Aggregate(Accumulate);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Aggregate001) + RunTest(Aggregate002);
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

        public class Aggregate01a
        {
            // Type: int, source is empty

            public static int Accumulate(int e1, int e2)
            {
                return e1 + e2;
            }

            // overload: only func, source has no elements      
            public static int Test1a()
            {
                int[] source = { };

                try
                {
                    var actual = source.Aggregate(Accumulate);
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

        public class Aggregate01b
        {
            // Type: int, source is empty

            public static int Accumulate(int e1, int e2)
            {
                return e1 + e2;
            }

            // overload: only func, source has one element        
            public static int Test1b()
            {
                int[] source = { 5 };
                int expected = 5;

                var actual = source.Aggregate(Accumulate);

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

        public class Aggregate01c
        {
            // Type: int, source is empty

            public static int Accumulate(int e1, int e2)
            {
                return e1 + e2;
            }

            // overload: only func, source has two elements        
            public static int Test1c()
            {
                int[] source = { 5, 6 };
                int expected = 11;

                var actual = source.Aggregate(Accumulate);

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

        public class Aggregate01d
        {
            // Type: int, source is empty

            public static int Accumulate(int e1, int e2)
            {
                return e1 + e2;
            }

            // overload: only func, source has limited number of elements        
            public static int Test1d()
            {
                int[] source = { 5, 6, 0, -4 };
                int expected = 7;

                var actual = source.Aggregate(Accumulate);

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

        public class Aggregate02a
        {
            // Type: int, source is empty
            public static long Multiply(long e1, int e2)
            {
                return e1 * e2;
            }

            // overload: seed and func, source has no elements        
            public static int Test2a()
            {
                int[] source = { };
                long seed = 2;
                long expected = 2;

                var actual = source.Aggregate(seed, Multiply);

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

        public class Aggregate02b
        {
            // Type: int, source is empty
            public static long Multiply(long e1, int e2)
            {
                return e1 * e2;
            }

            // overload: seed and func, source has one element        
            public static int Test2b()
            {
                int[] source = { 5 };
                long seed = 2;
                long expected = 10;

                var actual = source.Aggregate(seed, Multiply);

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

        public class Aggregate02c
        {
            // Type: int, source is empty
            public static long Multiply(long e1, int e2)
            {
                return e1 * e2;
            }

            // overload: seed and func, source has two elements        
            public static int Test2c()
            {
                int[] source = { 5, 6 };
                long seed = 2;
                long expected = 60;

                var actual = source.Aggregate(seed, Multiply);

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

        public class Aggregate02d
        {
            // Type: int, source is empty
            public static long Multiply(long e1, int e2)
            {
                return e1 * e2;
            }

            // overload: seed and func, source has limited number of elements        
            public static int Test2d()
            {
                int[] source = { 5, 6, 2, -4 };
                long seed = 2;
                long expected = -480;

                var actual = source.Aggregate(seed, Multiply);

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

        public class Aggregate03a
        {
            // Type: int, source is empty
            public static long Multiply(long e1, int e2)
            {
                return e1 * e2;
            }

            // overload: seed, func and resultSelector, source has no elements        
            public static int Test3a()
            {
                int[] source = { };
                long seed = 2;
                double expected = 7;
                Func<long, double> resultSelector = (x) => x + 5;

                var actual = source.Aggregate(seed, Multiply, resultSelector);

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

        public class Aggregate03b
        {
            // Type: int, source is empty
            public static long Multiply(long e1, int e2)
            {
                return e1 * e2;
            }

            // overload: seed, func and resultSelector, source has one element        
            public static int Test3b()
            {
                int[] source = { 5 };
                long seed = 2;
                long expected = 15;
                Func<long, double> resultSelector = (x) => x + 5;

                var actual = source.Aggregate(seed, Multiply, resultSelector);

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

        public class Aggregate03c
        {
            // Type: int, source is empty
            public static long Multiply(long e1, int e2)
            {
                return e1 * e2;
            }

            // overload: seed, func and resultSelector, source has two elements        
            public static int Test3c()
            {
                int[] source = { 5, 6 };
                long seed = 2;
                long expected = 65;
                Func<long, double> resultSelector = (x) => x + 5;

                var actual = source.Aggregate(seed, Multiply, resultSelector);

                return ((expected == actual) ? 0 : 1);
            }

            public static int Main()
            {
                return Test3c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Aggregate03d
        {
            // Type: int, source is empty
            public static long Multiply(long e1, int e2)
            {
                return e1 * e2;
            }

            // overload: seed, func and resultSelector, source has limited number of elements        
            public static int Test3d()
            {
                int[] source = { 5, 6, 2, -4 };
                long seed = 2;
                long expected = -475;
                Func<long, double> resultSelector = (x) => x + 5;

                var actual = source.Aggregate(seed, Multiply, resultSelector);

                return ((expected == actual) ? 0 : 1);
            }

            public static int Main()
            {
                return Test3d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
