// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class RangeTests
    {
        public class Range006
        {
            private static int Range001()
            {
                var rst1 = Enumerable.Range(-1, 2);
                var rst2 = Enumerable.Range(-1, 2);

                return Verification.Allequal(rst1, rst2);
            }

            private static int Range002()
            {
                var rst1 = Enumerable.Range(0, 0);
                var rst2 = Enumerable.Range(0, 0);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Range001) + RunTest(Range002);
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

        public class Range1
        {
            // count<0
            public static int Test1()
            {
                int start = 5;
                int count = -1; // Throws an Exception
                int[] expected = { };

                try
                {
                    var actual = Enumerable.Range(start, count);
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

        public class Range2
        {
            // start+count-1 is greater than int.MaxValue
            public static int Test2()
            {
                int start = Int32.MaxValue - 10;
                int count = 20; // Throws an Exception
                int[] expected = { };

                try
                {
                    var actual = Enumerable.Range(start, count);
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
                return Test2();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Range3
        {
            // count=0
            public static int Test3()
            {
                int start = 5;
                int count = 0;
                int[] expected = { };

                var actual = Enumerable.Range(start, count);

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

        public class Range4
        {
            // count=1 and start<0
            public static int Test4()
            {
                int start = -5;
                int count = 1;
                int[] expected = { -5 };

                var actual = Enumerable.Range(start, count);

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

        public class Range5
        {
            // count>1
            public static int Test5()
            {
                int start = 12;
                int count = 6;
                int[] expected = { 12, 13, 14, 15, 16, 17 };

                var actual = Enumerable.Range(start, count);

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
