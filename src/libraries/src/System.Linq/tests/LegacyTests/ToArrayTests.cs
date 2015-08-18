// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class ToArrayTests
    {
        public class ToArray006
        {
            private static int ToArray001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.ToArray<int>();
                var rst2 = q.ToArray<int>();

                return Verification.Allequal(rst1, rst2);
            }

            private static int ToArray002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.ToArray<string>();
                var rst2 = q.ToArray<string>();

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(ToArray001) + RunTest(ToArray002);
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

        public class ToArray1
        {
            // source is of type ICollection and source is empty
            public static int Test1()
            {
                int[] source = { };
                int[] expected = { };

                ICollection<int> collection = source as ICollection<int>;
                if (collection == null) return 1;

                var actual = source.ToArray();

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

        public class ToArray2
        {
            // source is of type ICollection and source has few elements
            public static int Test2()
            {
                int?[] source = { -5, null, 0, 10, 3, -1, null, 4, 9 };
                int?[] expected = { -5, null, 0, 10, 3, -1, null, 4, 9 };

                ICollection<int?> collection = source as ICollection<int?>;
                if (collection == null) return 1;

                var actual = source.ToArray();

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

        public class ToArray3
        {
            // source is NOT of type ICollection and source is empty
            public static int Test3()
            {
                IEnumerable<int> source = Functions.NumList(-4, 0);
                int[] expected = { };

                ICollection<int> collection = source as ICollection<int>;
                if (collection != null) return 1;

                var actual = source.ToArray();

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

        public class ToArray4
        {
            // source is NOT of type ICollection and source has few elements
            public static int Test4()
            {
                IEnumerable<int> source = Functions.NumList(-4, 10);
                int[] expected = { -4, -3, -2, -1, 0, 1, 2, 3, 4, 5 };

                ICollection<int> collection = source as ICollection<int>;
                if (collection != null) return 1;

                var actual = source.ToArray();

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

        public class ToArray5
        {
            // source is NOT of type ICollection and source has null elements only
            public static int Test5()
            {
                IEnumerable<int?> source = Functions.NullSeq(5);
                int?[] expected = { null, null, null, null, null };

                ICollection<int?> collection = source as ICollection<int?>;
                if (collection != null) return 1;

                var actual = source.ToArray();

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
