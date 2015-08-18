// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class DefaultIfEmptyTests
    {
        public class DefaultIfEmpty003
        {
            private static int DefaultIfEmpty001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.DefaultIfEmpty(5);
                var rst2 = q.DefaultIfEmpty(5);

                return Verification.Allequal(rst1, rst2);
            }

            private static int DefaultIfEmpty002()
            {
                IEnumerable<int> ieInt = Functions.NumList(0, 0);

                var q = from x in ieInt
                        select x;

                var rst1 = q.DefaultIfEmpty(88);
                var rst2 = q.DefaultIfEmpty(88);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(DefaultIfEmpty001) + RunTest(DefaultIfEmpty002);
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

        public class DefaultIfEmpty1a
        {
            // source is empty, no defaultValue passed
            public static int Test1a()
            {
                int?[] source = { };
                int?[] expected = { null };

                var actual = source.DefaultIfEmpty();

                return Verification.Allequal(expected, actual);
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

        public class DefaultIfEmpty1b
        {
            // source is empty, no defaultValue passed
            public static int Test1b()
            {
                int[] source = { };
                int[] expected = { default(int) };

                var actual = source.DefaultIfEmpty();

                return Verification.Allequal(expected, actual);
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

        public class DefaultIfEmpty1c
        {
            // source is non-empty, no defaultValue passed
            public static int Test1c()
            {
                int[] source = { 3 };
                int[] expected = { 3 };

                var actual = source.DefaultIfEmpty();

                return Verification.Allequal(expected, actual);
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

        public class DefaultIfEmpty1d
        {
            // source is non-empty, no defaultValue passed
            public static int Test1d()
            {
                int[] source = { 3, -1, 0, 10, 15 };
                int[] expected = { 3, -1, 0, 10, 15 };

                var actual = source.DefaultIfEmpty();

                return Verification.Allequal(expected, actual);
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

        public class DefaultIfEmpty2a
        {
            // source is empty, defaultValue passed
            public static int Test2a()
            {
                int?[] source = { };
                int? defaultValue = 9;
                int?[] expected = { 9 };

                var actual = source.DefaultIfEmpty(defaultValue);

                return Verification.Allequal(expected, actual);
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

        public class DefaultIfEmpty2b
        {
            // source is empty, defaultValue passed
            public static int Test2b()
            {
                int[] source = { };
                int defaultValue = -10;
                int[] expected = { -10 };

                var actual = source.DefaultIfEmpty(defaultValue);

                return Verification.Allequal(expected, actual);
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

        public class DefaultIfEmpty2c
        {
            // source is non-empty, defaultValue passed
            public static int Test2c()
            {
                int[] source = { 3 };
                int defaultValue = 9;
                int[] expected = { 3 };

                var actual = source.DefaultIfEmpty(defaultValue);

                return Verification.Allequal(expected, actual);
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

        public class DefaultIfEmpty2d
        {
            // source is non-empty, defaultValue passed
            public static int Test2d()
            {
                int[] source = { 3, -1, 0, 10, 15 };
                int defaultValue = 9;
                int[] expected = { 3, -1, 0, 10, 15 };

                var actual = source.DefaultIfEmpty(defaultValue);

                return Verification.Allequal(expected, actual);
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
    }
}
