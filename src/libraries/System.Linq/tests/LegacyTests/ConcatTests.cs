// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ConcatTests
    {
        public class Concat005
        {
            private static int Concat001()
            {
                var q1 = from x1 in new int?[] { 2, 3, null, 2, null, 4, 5 }
                         select x1;
                var q2 = from x2 in new int?[] { 1, 9, null, 4 }
                         select x2;

                var rst1 = q1.Concat(q2);
                var rst2 = q1.Concat(q2);

                return Verification.Allequal(rst1, rst2);
            }

            private static int Concat002()
            {
                var q1 = from x1 in new[] { "AAA", String.Empty, "q", "C", "#", "!@#$%^", "0987654321", "Calling Twice" }
                         select x1;
                var q2 = from x2 in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS" }
                         select x2;

                var rst1 = q1.Concat(q2);
                var rst2 = q1.Concat(q2);

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(Concat001) + RunTest(Concat002);
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

        public class Concat1
        {
            // first is empty and second is empty
            public static int Test1()
            {
                int[] first = { };
                int[] second = { };
                int[] expected = { };

                var actual = first.Concat(second);
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

        public class Concat2
        {
            // first is empty and second is non-empty
            public static int Test2()
            {
                int[] first = { };
                int[] second = { 2, 6, 4, 6, 2 };
                int[] expected = { 2, 6, 4, 6, 2 };

                var actual = first.Concat(second);
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

        public class Concat3
        {
            // first is non-empty and second is empty
            public static int Test3()
            {
                int[] first = { 2, 6, 4, 6, 2 };
                int[] second = { };
                int[] expected = { 2, 6, 4, 6, 2 };

                var actual = first.Concat(second);
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

        public class Concat4
        {
            // first is non-empty and second is non-empty
            public static int Test4()
            {
                int?[] first = { 2, null, 3, 5, 9 };
                int?[] second = { null, 8, 10 };
                int?[] expected = { 2, null, 3, 5, 9, null, 8, 10 };

                var actual = first.Concat(second);
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
    }
}
