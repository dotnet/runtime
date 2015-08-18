// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class OfTypeTests
    {
        public class OfType011
        {
            private static int OfType001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.OfType<int>();
                var rst2 = q.OfType<int>();

                return Verification.Allequal(rst1, rst2);
            }

            private static int OfType002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.OfType<string>();
                var rst2 = q.OfType<string>();

                return Verification.Allequal(rst1, rst2);
            }

            public static int Main()
            {
                int ret = RunTest(OfType001) + RunTest(OfType002);
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

        public class OfType1
        {
            // source is empty
            public static int Test1()
            {
                Object[] source = { };
                int[] expected = { };

                IEnumerable<int> actual = source.OfType<int>();

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

        public class OfType10
        {
            //  source is an int and type is long
            public static int Test10()
            {
                int[] source = { 99, 45, 81 };
                long[] expected = { };

                IEnumerable<long> actual = source.OfType<long>();

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test10();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class OfType2
        {
            // source does NOT have any object of type int
            public static int Test2()
            {
                Object[] source = { "Hello", 3.5, "Test" };
                int[] expected = { };

                IEnumerable<int> actual = source.OfType<int>();

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

        public class OfType3
        {
            // only the first element in source is of type int
            public static int Test3()
            {
                Object[] source = { 10, "Hello", 3.5, "Test" };
                int[] expected = { 10 };

                IEnumerable<int> actual = source.OfType<int>();

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

        public class OfType4
        {
            // All elements in source is of type int?
            public static int Test4()
            {
                Object[] source = { 10, -4, null, null, 4, 9 };
                int?[] expected = { 10, -4, 4, 9 };

                IEnumerable<int?> actual = source.OfType<int?>();

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

        public class OfType5
        {
            // When 2nd, 5th and the last element is of type int
            public static int Test5()
            {
                Object[] source = { 3.5m, -4, "Test", "Check", 4, 8.0, 10.5, 9 };
                int[] expected = { -4, 4, 9 };

                IEnumerable<int> actual = source.OfType<int>();

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

        public class OfType6
        {
            //  source is an int and type is int?
            public static int Test6()
            {
                int[] source = { -4, 4, 9 };
                int?[] expected = { -4, 4, 9 };

                IEnumerable<int?> actual = source.OfType<int?>();

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

        public class OfType7
        {
            //  source is an int? and type is int
            public static int Test7()
            {
                int?[] source = { null, -4, 4, null, 9 };
                int[] expected = { -4, 4, 9 };

                IEnumerable<int> actual = source.OfType<int>();

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

        public class OfType8
        {
            //  source is an string and type is decimal?
            public static int Test8()
            {
                string[] source = { "Test1", "Test2", "Test9" };
                decimal?[] expected = { };

                IEnumerable<decimal?> actual = source.OfType<decimal?>();

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test8();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class OfType9
        {
            //  source is an long and type is double
            public static int Test9()
            {
                long[] source = { 99L, 45L, 81L };
                double[] expected = { };

                IEnumerable<double> actual = source.OfType<double>();

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test9();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
