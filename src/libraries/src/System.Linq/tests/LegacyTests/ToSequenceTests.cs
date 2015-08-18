// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ToSequenceTests
    {
        public class ToSequence004
        {
            private static int ToSequence001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.AsEnumerable();
                var rst2 = q.AsEnumerable();

                return Verification.Allequal(rst1, rst2);
            }

            private static int ToSequence002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.AsEnumerable();
                var rst2 = q.AsEnumerable();

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(ToSequence001) + RunTest(ToSequence002);
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

        public class ToSequence1
        {
            // source is null
            public static int Test1()
            {
                int[] source = null;
                IEnumerable<int> expected = null;

                var actual = source.AsEnumerable();

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

        public class ToSequence2
        {
            // source has only one element
            public static int Test2()
            {
                int[] source = { 2 };
                int[] expected = { 2 };

                var actual = source.AsEnumerable();

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

        public class ToSequence3
        {
            // source has limited number of elements
            public static int Test3()
            {
                int?[] source = { -5, 0, 1, -4, 3, null, 10 };
                int?[] expected = { -5, 0, 1, -4, 3, null, 10 };

                var actual = source.AsEnumerable();

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
