// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class SkipTests
    {
        public class Skip008
        {
            private static int Skip001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Skip(0);
                var rst2 = q.Skip(0);

                return Verification.Allequal(rst1, rst2);
            }

            private static int Skip002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.Skip(0);
                var rst2 = q.Skip(0);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Skip001) + RunTest(Skip002);
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

        public class Skip1
        {
            // source is empty and count>0
            public static int Test1()
            {
                int[] source = { };
                int[] expected = { };
                int count = 3;

                var actual = source.Skip(count);
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

        public class Skip2
        {
            // source is >0 and count=0
            public static int Test2()
            {
                int[] source = { 3, 100, 4, 10 };
                int[] expected = { 3, 100, 4, 10 };
                int count = 0;

                var actual = source.Skip(count);
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

        public class Skip3
        {
            // source is >0 and count<0
            public static int Test3()
            {
                int[] source = { 3, 100, 4, 10 };
                int[] expected = { 3, 100, 4, 10 };
                int count = -10;

                var actual = source.Skip(count);
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

        public class Skip4
        {
            // source is >1 and count=1
            public static int Test4()
            {
                int?[] source = { 3, 100, 4, null, 10 };
                int?[] expected = { 100, 4, null, 10 };
                int count = 1;

                var actual = source.Skip(count);
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

        public class Skip5
        {
            // source is >0 and count=Number of elements in source
            public static int Test5()
            {
                int[] source = { 3, 100, 4, 10 };
                int[] expected = { };
                int count = 4;

                var actual = source.Skip(count);
                return Verification.Allequal(expected, actual);
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

        public class Skip6
        {
            // source is >0 and count=Number of elements in source - 1
            public static int Test6()
            {
                int?[] source = { 3, 100, null, 4, 10 };
                int?[] expected = { 10 };
                int count = 4;

                var actual = source.Skip(count);
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

        public class Skip7
        {
            // source is >0 and count=Number of elements in source + 1
            public static int Test7()
            {
                int[] source = { 3, 100, 4, 10 };
                int[] expected = { };
                int count = 5;

                var actual = source.Skip(count);
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
    }
}
