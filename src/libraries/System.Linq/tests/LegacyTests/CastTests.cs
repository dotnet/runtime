// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class CastTests
    {
        public class Helper
        {
            // Helper Method for Test15 and Test16
            public static int GenericTest<T>(object o, T[] expected)
            {
                byte? i = 10;
                Object[] source = { -1, 0, o, i };

                IEnumerable<int?> source1 = source.Cast<int?>();
                IEnumerable<T> actual = source1.Cast<T>();

                return Verification.Allequal(expected, actual);
            }
        }

        public class Cast019
        {
            private static int Cast001()
            {
                try
                {
                    var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                            where x > Int32.MinValue
                            select x;

                    var rst1 = q.Cast<long>();
                    var rst2 = q.Cast<long>();

                    Verification.Allequal(rst1, rst2);
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
                catch (Exception) { return 1; }
                return 1;
            }

            private static int Cast002()
            {
                try
                {
                    var q = from x in new byte[] { 0, 255, 127, 128, 1, 33, 99 }
                            select x;

                    var rst1 = q.Cast<ushort>();
                    var rst2 = q.Cast<ushort>();
                    Verification.Allequal(rst1, rst2);
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
                catch (Exception) { return 1; }
                return 1;
            }

            public static int Main()
            {
                int ret = RunTest(Cast001) + RunTest(Cast002);
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

        public class Cast1
        {
            // source is empty
            public static int Test1()
            {
                Object[] source = { };
                int[] expected = { };

                IEnumerable<int> actual = source.Cast<int>();

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

        public class Cast10
        {
            // source of type int? to object and IEnumerable<int?> Cast to type long
            // DDB: 137558
            public static int Test10()
            {
                int? i = 10;
                Object[] source = { -4, 1, 2, 3, 9, i };

                long[] expected = { -4L, 1L, 2L, 3L, 9L, (long)i };

                try
                {
                    IEnumerable<int?> source1 = source.Cast<int?>();
                    IEnumerable<long> actual = source1.Cast<long>();
                    Verification.Allequal(expected, actual);
                    return 1;
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
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

        public class Cast11
        {
            // source of type int? to object and IEnumerable<int?> Cast to type long?
            // DDB: 137558
            public static int Test11()
            {
                try
                {
                    int? i = 10;
                    Object[] source = { -4, 1, 2, 3, 9, null, i };
                    long?[] expected = { -4L, 1L, 2L, 3L, 9L, null, (long?)i };

                    IEnumerable<int?> source1 = source.Cast<int?>();
                    IEnumerable<long?> actual = source1.Cast<long?>();
                    Verification.Allequal(actual, expected);
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
                catch (Exception) { return 1; }
                return 1;
            }


            public static int Main()
            {
                return Test11();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Cast12
        {
            // source of type int? to object cast to IEnumerable<int?> 
            // DDB: 137558
            public static int Test12()
            {
                int? i = 10;
                Object[] source = { -4, 1, 2, 3, 9, null, i };
                int?[] expected = { -4, 1, 2, 3, 9, null, i };

                IEnumerable<int?> actual = source.Cast<int?>();

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test12();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Cast13
        {
            // source of type object cast to IEnumerable<int> 
            // DDB: 137558
            public static int Test13()
            {
                Object[] source = { -4, 1, 2, 3, 9, "45" };
                int[] expected = { -4, 1, 2, 3, 9, 45 };

                try
                {
                    IEnumerable<int> actual = source.Cast<int>();
                    Verification.Allequal(expected, actual);
                    return 1;
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test13();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Cast14
        {
            // source of type int Cast to type double
            // DDB: 137558
            public static int Test14()
            {
                try
                {
                    int[] source = new int[] { -4, 1, 2, 9 };
                    double[] expected = { -4.0, 1.0, 2.0, 9.0 };

                    IEnumerable<double> actual = source.Cast<double>();
                    Verification.Allequal(actual, expected);
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
                catch (Exception) { return 1; }
                return 1;
            }


            public static int Main()
            {
                return Test14();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Cast15
        {
            // Cast involving Generic types
            // DDB: 137558
            public static int Test15()
            {
                try
                {
                    long?[] expected = { -1L, 0L, null, 10L };

                    var x = Helper.GenericTest<long?>(null, expected);
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
                catch (Exception)
                { return 1; }
                return 1;
            }

            public static int Main()
            {
                return Test15();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Cast16
        {
            // Cast involving Generic types
            // DDB: 137558
            public static int Test16()
            {
                try
                {
                    long[] expected = { -1L, 0L, 9L, 10L };

                    var x = Helper.GenericTest<long>(9L, expected);
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
                catch (Exception) { return 1; }
                return 1;
            }


            public static int Main()
            {
                return Test16();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Cast17
        {
            // source of type object Cast to type string
            // DDB: 137558
            public static int Test17()
            {
                object[] source = { "Test1", "4.5", null, "Test2" };
                string[] expected = { "Test1", "4.5", null, "Test2" };

                IEnumerable<string> actual = source.Cast<string>();

                return Verification.Allequal(expected, actual);
            }


            public static int Main()
            {
                return Test17();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Cast18
        {
            //testing array conversion using .Cast()
            // From Silverlight testing
            public static int Test18()
            {
                try
                {
                    var actual = new[] { -4 }.Cast<long>().ToList();
                    if (actual[0] == -4)
                        return 1;
                    return 1;
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
            }

            public static int Main()
            {
                return Test18();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Cast2
        {
            // first element cannot be cast to type int: Test for InvalidCastException
            public static int Test2()
            {
                Object[] source = { "Test", 3, 5, 10 };
                int[] expected = { 3, 5, 10 };

                try
                {
                    IEnumerable<int> actual = source.Cast<int>();
                    Verification.Allequal(expected, actual);
                    return 1;
                }
                catch (InvalidCastException)
                {
                    return 0;
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

        public class Cast3
        {
            // last element cannot be cast to type int: Test for InvalidCastException
            public static int Test3()
            {
                Object[] source = { -5, 9, 0, 5, 9, "Test" };
                int[] expected = { -5, 9, 0, 5, 9, 10 };

                try
                {
                    IEnumerable<int> actual = source.Cast<int>();
                    Verification.Allequal(expected, actual);
                    return 1;
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
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

        public class Cast4
        {
            // All elements in source can be cast to int?
            public static int Test4()
            {
                Object[] source = { 3, null, 5, -4, 0, null, 9 };
                int?[] expected = { 3, null, 5, -4, 0, null, 9 };

                IEnumerable<int?> actual = source.Cast<int?>();

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

        public class Cast5
        {
            // source of type int Cast to type long
            // DDB: 137558
            public static int Test5()
            {
                try
                {
                    int[] source = new int[] { -4, 1, 2, 3, 9 };
                    long[] expected = { -4L, 1L, 2L, 3L, 9L };

                    IEnumerable<long> actual = source.Cast<long>();
                    Verification.Allequal(expected, actual);
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
                catch (Exception) { return 1; }
                return 1;
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

        public class Cast6
        {
            // source of type int Cast to type long?
            // DDB: 137558
            public static int Test6()
            {
                try
                {
                    int[] source = new int[] { -4, 1, 2, 3, 9 };
                    long?[] expected = { -4L, 1L, 2L, 3L, 9L };

                    IEnumerable<long?> actual = source.Cast<long?>();
                    Verification.Allequal(expected, actual);
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
                catch (Exception) { return 1; }
                return 1;
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

        public class Cast7
        {
            // source of type int? Cast to type long
            // DDB: 137558
            public static int Test7()
            {
                try
                {
                    int?[] source = new int?[] { -4, 1, 2, 3, 9 };
                    long[] expected = { -4L, 1L, 2L, 3L, 9L };

                    IEnumerable<long> actual = source.Cast<long>();
                    Verification.Allequal(expected, actual);
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
                catch (Exception) { return 1; }
                return 1;
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

        public class Cast8
        {
            // source of type int? Cast to type long with null value
            // DDB: 137558
            public static int Test8()
            {
                int?[] source = new int?[] { -4, 1, 2, 3, 9 };
                long[] expected = { -4L, 1L, 2L, 3L, 9L };

                try
                {
                    IEnumerable<long> actual = source.Cast<long>();
                    Verification.Allequal(expected, actual);
                    return 1;
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
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

        public class Cast9
        {
            // source of type int? Cast to type long?
            // DDB: 137558
            public static int Test9()
            {
                try
                {
                    int?[] source = new int?[] { -4, 1, 2, 3, 9, null };
                    long?[] expected = { -4L, 1L, 2L, 3L, 9L, null };

                    IEnumerable<long?> actual = source.Cast<long?>();
                    Verification.Allequal(expected, actual);
                }
                catch (InvalidCastException)
                {
                    return 0;
                }
                catch (Exception) { return 1; }
                return 1;
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