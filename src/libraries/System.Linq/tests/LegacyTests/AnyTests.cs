// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class AnyTests
    {
        public class Any003
        {
            private static int Any001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                Func<int, bool> predicate = Functions.IsEven;
                var rst1 = q.Any(predicate);
                var rst2 = q.Any(predicate);

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Any002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        select x;

                Func<string, bool> predicate = Functions.IsEmpty;
                var rst1 = q.Any<string>(predicate);
                var rst2 = q.Any<string>(predicate);

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(Any001) + RunTest(Any002);
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

        public class Any1a
        {
            // Overload without predicate, source is empty
            public static int Test1a()
            {
                int[] source = { };
                bool expected = false;

                var actual = source.Any();

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

        public class Any1b
        {
            // Overload without predicate, source has one element
            public static int Test1b()
            {
                int[] source = { 3 };
                bool expected = true;

                var actual = source.Any();

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

        public class Any1c
        {
            // Overload without predicate, source has limited elements
            public static int Test1c()
            {
                int?[] source = { null, null, null, null };
                bool expected = true;

                var actual = source.Any();

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

        public class Any2a
        {
            // Overload with predicate, source is empty
            public static int Test2a()
            {
                int[] source = { };
                Func<int, bool> predicate = Functions.IsEven;
                bool expected = false;

                var actual = source.Any(predicate);

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

        public class Any2b
        {
            // Overload with predicate, source has one element and predicate is true
            public static int Test2b()
            {
                int[] source = { 4 };
                Func<int, bool> predicate = Functions.IsEven;
                bool expected = true;

                var actual = source.Any(predicate);

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

        public class Any2c
        {
            // Overload with predicate, source has one element and predicate is false
            public static int Test2c()
            {
                int[] source = { 5 };
                Func<int, bool> predicate = Functions.IsEven;
                bool expected = false;

                var actual = source.Any(predicate);

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

        public class Any2d
        {
            // Overload with predicate, predicate is true only for last element
            public static int Test2d()
            {
                int[] source = { 5, 9, 3, 7, 4 };
                Func<int, bool> predicate = Functions.IsEven;
                bool expected = true;

                var actual = source.Any(predicate);

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

        public class Any2e
        {
            // Overload with predicate, predicate is true only for 2nd element
            public static int Test2e()
            {
                int[] source = { 5, 8, 9, 3, 7, 11 };
                Func<int, bool> predicate = Functions.IsEven;
                bool expected = true;

                var actual = source.Any(predicate);

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