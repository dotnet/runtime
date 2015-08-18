// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class EmptyTests
    {
        public class Empty003
        {
            private static int Empty001()
            {
                var rst1 = Enumerable.Empty<int>();
                var rst2 = Enumerable.Empty<int>();

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Empty002()
            {
                var rst1 = Enumerable.Empty<string>();
                var rst2 = Enumerable.Empty<string>();

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(Empty001) + RunTest(Empty002);
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

        public class Empty1
        {
            // Type: Empty Sequence of int
            public static int Test1()
            {
                int[] expected = { };

                IEnumerable<int> actual = Enumerable.Empty<int>();

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

        public class Empty2
        {
            // Type: Empty Sequence of string
            public static int Test2()
            {
                string[] expected = { };

                IEnumerable<string> actual = Enumerable.Empty<string>();

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
    }
}
