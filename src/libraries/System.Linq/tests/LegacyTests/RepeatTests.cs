// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class RepeatTests
    {
        public class Repeat006
        {
            private static int Repeat001()
            {
                var rst1 = Enumerable.Repeat(-3, 0);
                var rst2 = Enumerable.Repeat(-3, 0);

                return Verification.Allequal(rst1, rst2);
            }

            private static int Repeat002()
            {
                var rst1 = Enumerable.Repeat("SSS", 99);
                var rst2 = Enumerable.Repeat("SSS", 99);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Repeat001) + RunTest(Repeat002);
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

        public class Repeat1
        {
            // count<0
            public static int Test1()
            {
                int element = 5;
                int count = -1; // Throws an Exception
                int[] expected = { };

                try
                {
                    var actual = Enumerable.Repeat(element, count);
                    return 1;
                }
                catch (ArgumentOutOfRangeException aoore)
                {
                    if (aoore.CompareParamName("count"))
                        return 0;
                    else
                        return 1;
                }
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

        public class Repeat2
        {
            // count=0
            public static int Test2()
            {
                int element = -15;
                int count = 0;
                int[] expected = { };

                var actual = Enumerable.Repeat(element, count);

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

        public class Repeat3
        {
            // count=1
            public static int Test3()
            {
                int element = -15;
                int count = 1;
                int[] expected = { -15 };

                var actual = Enumerable.Repeat(element, count);

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

        public class Repeat4
        {
            // count>1
            public static int Test4()
            {
                int element = 12;
                int count = 8;
                int[] expected = { 12, 12, 12, 12, 12, 12, 12, 12 };

                var actual = Enumerable.Repeat(element, count);

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

        public class Repeat5
        {
            // count>1 and element is null
            public static int Test5()
            {
                int? element = null;
                int count = 4;
                int?[] expected = { null, null, null, null };

                var actual = Enumerable.Repeat(element, count);

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
    }
}
