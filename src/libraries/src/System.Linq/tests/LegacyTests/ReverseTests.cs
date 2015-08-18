// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ReverseTests
    {
        public class Reverse004
        {
            private static int Reverse001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Reverse();
                var rst2 = q.Reverse();

                return Verification.Allequal(rst1, rst2);
            }

            private static int Reverse002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.Reverse();
                var rst2 = q.Reverse();

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Reverse001) + RunTest(Reverse002);
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

        public class Reverse1
        {
            // source is empty
            public static int Test1()
            {
                int[] source = { };
                int[] expected = { };

                var actual = source.Reverse();

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

        public class Reverse2
        {
            // source has only one element
            public static int Test2()
            {
                int[] source = { 5 };
                int[] expected = { 5 };

                var actual = source.Reverse();

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

        public class Reverse3
        {
            // source has duplicate elements
            public static int Test3()
            {
                int?[] source = { -10, 0, 5, null, 0, 9, 100, null, 9 };
                int?[] expected = { 9, null, 100, 9, 0, null, 5, 0, -10 };

                var actual = source.Reverse();

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
    }
}
