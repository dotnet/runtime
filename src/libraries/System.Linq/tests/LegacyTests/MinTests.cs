// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class MinTests
    {
        // struct used to test selector function is called
        public struct Data_int
        {
            public string name;
            public int num;
        }

        // struct used to test selector function is called
        public struct Data_Nint
        {
            public string name;
            public int? num;
        }

        // struct used to test selector function is called
        public struct Data_long
        {
            public string name;
            public long num;
        }

        // struct used to test selector function is called
        public struct Data_Nlong
        {
            public string name;
            public long? num;
        }

        // struct used to test selector function is called
        public struct Data_double
        {
            public string name;
            public double num;
        }

        // struct used to test selector function is called
        public struct Data_Ndouble
        {
            public string name;
            public double? num;
        }

        // struct used to test selector function is called
        public struct Data_decimal
        {
            public string name;
            public decimal num;
        }

        // struct used to test selector function is called
        public struct Data_Ndecimal
        {
            public string name;
            public decimal? num;
        }

        // struct used to test selector function is called
        public struct Data_string
        {
            public string name;
            public decimal num;
        }

        // struct used to test selector function is called
        public struct Data_float
        {
            public string name;
            public float num;
        }

        // struct used to test selector function is called
        public struct Data_Nfloat
        {
            public string name;
            public float? num;
        }

        public class Min012
        {
            private static int Min001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Min<int>();
                var rst2 = q.Min<int>();
                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Min002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.Min<string>();
                var rst2 = q.Min<string>();

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(Min001) + RunTest(Min002);
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

        public class Min10a
        {
            // Type: float, source is empty
            public static int Test10a()
            {
                float[] source = { };

                try
                {
                    var actual = source.Min();
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test10a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10b
        {
            // Type: float, source has only one element
            public static int Test10b()
            {
                float[] source = { 5.5f };
                float expected = 5.5f;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test10b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10c
        {
            // Type: float, source has all equal values
            public static int Test10c()
            {
                float[] source = { float.NaN, float.NaN, float.NaN, float.NaN };
                float expected = float.NaN;

                var actual = source.Min();

                return ((expected.Equals(actual)) ? 0 : 1);
            }


            public static int Main()
            {
                return Test10c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10d
        {
            // Type: float, minimum value occurs as the first element
            public static int Test10d()
            {
                float[] source = { -2.5f, 4.9f, 130f, 4.7f, 28f };
                float expected = -2.5f;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test10d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10e
        {
            // Type: float, minimum value occurs as the last element
            public static int Test10e()
            {
                float[] source = { 6.8f, 9.4f, 10f, 0, -5.6f };
                float expected = -5.6f;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test10e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10f
        {
            // Type: float, minimum value occurs 2/3 times
            public static int Test10f()
            {
                float[] source = { -5.5f, float.NegativeInfinity, 9.9f, float.NegativeInfinity };
                float expected = float.NegativeInfinity;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test10f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10g
        {
            // Type: float, selector function is called
            public static int Test10g()
            {
                Data_float[] source = new Data_float[]{ new Data_float{name="Tim", num=-45.5f},
                                                new Data_float{name="John", num=-132.5f},
                                                new Data_float{name="Bob", num=20.45f}
            };
                float expected = -132.5f;

                var actual = source.Min((e) => e.num);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test10g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10h
        {
            // Type: float, source is null
            public static int Test10h()
            {
                float[] source = { -5.5f, float.NegativeInfinity, 9.9f, float.NegativeInfinity };

                try
                {
                    source = null;
                    var actual = source.Min();
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (!ane.CompareParamName("source")) return 1;
                    return 0;
                }
            }


            public static int Main()
            {
                return Test10h();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10i
        {
            // Type: float, NaN occurs as the 1st element
            public static int Test10i()
            {
                float[] source = { float.NaN, 6.8f, 9.4f, 10f, 0, -5.6f };

                var actual = source.Min();

                return (float.IsNaN(actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test10i();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10j
        {
            // Type: float, NaN occurs as the last element
            public static int Test10j()
            {
                float[] source = { 6.8f, 9.4f, 10f, 0, -5.6f, float.NaN };

                var actual = source.Min();

                return (float.IsNaN(actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test10j();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10k
        {
            // Type: float, NaN occurs as the 1st element and the other element is -ve infinity
            public static int Test10k()
            {
                float[] source = { float.NaN, float.NegativeInfinity };

                var actual = source.Min();

                return (float.IsNaN(actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test10k();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10l
        {
            // Type: float, NaN occurs as the last element and the other element is -ve infinity
            public static int Test10l()
            {
                float[] source = { float.NegativeInfinity, float.NaN };

                var actual = source.Min();

                return (float.IsNaN(actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test10l();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min10m
        {
            // Type: float, source element has only NaN's
            public static int Test10m()
            {
                float[] source = { float.NaN, float.NaN, float.NaN };

                var actual = source.Min();

                return (float.IsNaN(actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test10m();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11a
        {
            // Type: float?, source is empty, null is returned
            public static int Test11a()
            {
                float?[] source = { };
                float? expected = null;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11b
        {
            // Type: float?, source has only one element
            public static int Test11b()
            {
                float?[] source = { float.MinValue };
                float? expected = float.MinValue;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11c
        {
            // Type: float?, source has all null values
            public static int Test11c()
            {
                float?[] source = { null, null, null, null, null };
                float? expected = null;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11d
        {
            // Type: float?, Minimum occurs as the first element
            public static int Test11d()
            {
                float?[] source = { -4.50f, null, 10.98f, null, 7.5f, 8.6f };
                float? expected = -4.50f;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11e
        {
            // Type: float?, minimum value occurs as the last element
            public static int Test11e()
            {
                float?[] source = { null, null, null, null, null, 0f };
                float? expected = 0f;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11f
        {
            // Type: float?, minimum value occurs 2/3 times
            public static int Test11f()
            {
                float?[] source = { 6.4f, null, null, -0.5f, 9.4f, -0.5f, 10.9f, -0.5f };
                float? expected = -0.5f;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11g
        {
            // Type: float?, selector function is called
            public static int Test11g()
            {
                Data_Nfloat[] source = new Data_Nfloat[]{ new Data_Nfloat{name="Tim", num=-45.5f},
                                                new Data_Nfloat{name="John", num=-132.5f},
                                                new Data_Nfloat{name="Bob", num=null}
            };
                float? expected = -132.5f;

                var actual = source.Min((e) => e.num);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11h
        {
            // Type: float?, source is null
            public static int Test11h()
            {
                float?[] source = { 6.4f, null, null, -0.5f, 9.4f, -0.5f, 10.9f, -0.5f };

                try
                {
                    source = null;
                    var actual = source.Min();
                    return 1;
                }
                catch (ArgumentNullException ane)
                {
                    if (!ane.CompareParamName("source")) return 1;
                    return 0;
                }
            }


            public static int Main()
            {
                return Test11h();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11i
        {
            // Type: float?, NaN occurs as the 1st element and the sequence includes null
            public static int Test11i()
            {
                float?[] source = { float.NaN, 6.8f, 9.4f, 10f, 0, null, -5.6f };

                var actual = source.Min();

                return (float.IsNaN((float)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11i();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11j
        {
            // Type: float?, NaN occurs as the last element and the sequence includes null
            public static int Test11j()
            {
                float?[] source = { 6.8f, 9.4f, 10f, 0, null, -5.6f, float.NaN };

                var actual = source.Min();

                return (float.IsNaN((float)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11j();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11k
        {
            // Type: float?, NaN occurs as the 1st element and the other element is -ve infinity
            public static int Test11k()
            {
                float?[] source = { float.NaN, float.NegativeInfinity };

                var actual = source.Min();

                return (float.IsNaN((float)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11k();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11l
        {
            // Type: float?, NaN occurs as the last element and the other element is -ve infinity
            public static int Test11l()
            {
                float?[] source = { float.NegativeInfinity, float.NaN };

                var actual = source.Min();

                return (float.IsNaN((float)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11l();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11m
        {
            // Type: float?, source element has only NaN's
            public static int Test11m()
            {
                float?[] source = { float.NaN, float.NaN, float.NaN };

                var actual = source.Min();

                return (float.IsNaN((float)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11m();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11n
        {
            // Type: float?, NaN occurs as the first element and the rest are null's
            public static int Test11n()
            {
                float?[] source = { float.NaN, null, null, null };

                var actual = source.Min();

                return (float.IsNaN((float)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11n();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min11o
        {
            // Type: float?, NaN occurs as the last element and the rest are null's
            public static int Test11o()
            {
                float?[] source = { null, null, null, float.NaN };

                var actual = source.Min();

                return (float.IsNaN((float)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test11o();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min1a
        {
            // Type: int, source is empty
            public static int Test1a()
            {
                int[] source = { };

                try
                {
                    var actual = source.Min();
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test1a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min1b
        {
            // Type: int, source has only one element
            public static int Test1b()
            {
                int[] source = { 20 };
                int expected = 20;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min1c
        {
            // Type: int, source has all equal values
            public static int Test1c()
            {
                int[] source = { -2, -2, -2, -2, -2 };
                int expected = -2;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min1d
        {
            // Type: int, minimum value occurs as the first element
            public static int Test1d()
            {
                int[] source = { 6, 9, 10, 7, 8 };
                int expected = 6;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min1e
        {
            // Type: int, minimum value occurs as the last element
            public static int Test1e()
            {
                int[] source = { 6, 9, 10, 0, -5 };
                int expected = -5;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min1f
        {
            // Type: int, minimum value occurs 2/3 times
            public static int Test1f()
            {
                int[] source = { 6, 0, 9, 0, 10, 0 };
                int expected = 0;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min1g
        {
            // Type: int, selector function is called
            public static int Test1g()
            {
                Data_int[] source = new Data_int[]{ new Data_int{name="Tim", num=10},
                                                new Data_int{name="John", num=-105},
                                                new Data_int{name="Bob", num=-30}
            };
                int expected = -105;

                var actual = source.Min((e) => e.num);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test1g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min2a
        {
            // Type: int?, source is empty, null is returned
            public static int Test2a()
            {
                int?[] source = { };
                int? expected = null;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min2b
        {
            // Type: int?, source has only one element
            public static int Test2b()
            {
                int?[] source = { 20 };
                int? expected = 20;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min2c
        {
            // Type: int?, source has all null values
            public static int Test2c()
            {
                int?[] source = { null, null, null, null, null };
                int? expected = null;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min2d
        {
            // Type: int, Minimum occurs as the first element
            public static int Test2d()
            {
                int?[] source = { 6, null, 9, 10, null, 7, 8 };
                int? expected = 6;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min2e
        {
            // Type: int, minimum value occurs as the last element
            public static int Test2e()
            {
                int?[] source = { null, null, null, null, null, -5 };
                int? expected = -5;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min2f
        {
            // Type: int, minimum value occurs 2/3 times
            public static int Test2f()
            {
                int?[] source = { 6, null, null, 0, 9, 0, 10, 0 };
                int? expected = 0;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min2g
        {
            // Type: int?, selector function is called
            public static int Test2g()
            {
                Data_Nint[] source = new Data_Nint[]{ new Data_Nint{name="Tim", num=10},
                                                new Data_Nint{name="John", num=null},
                                                new Data_Nint{name="Bob", num=-30}
            };
                int expected = -30;

                var actual = source.Min((e) => e.num);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test2g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min3a
        {
            // Type: long, source is empty
            public static int Test3a()
            {
                long[] source = { };

                try
                {
                    var actual = source.Min();
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test3a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min3b
        {
            // Type: long, source has only one element
            public static int Test3b()
            {
                long[] source = { (long)Int32.MaxValue + 10 };
                long expected = (long)Int32.MaxValue + 10;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test3b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min3c
        {
            // Type: long, source has all equal values
            public static int Test3c()
            {
                long[] source = { 500, 500, 500, 500, 500 };
                long expected = 500;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test3c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min3d
        {
            // Type: long, minimum value occurs as the first element
            public static int Test3d()
            {
                long[] source = { -250, 49, 130, 47, 28 };
                long expected = -250;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test3d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min3e
        {
            // Type: long, minimum value occurs as the last element
            public static int Test3e()
            {
                long[] source = { 6, 9, 10, 0, (long)-Int32.MaxValue - 50 };
                long expected = (long)-Int32.MaxValue - 50;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test3e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min3f
        {
            // Type: long, minimum value occurs 2/3 times
            public static int Test3f()
            {
                long[] source = { 6, -5, 9, -5, 10, -5 };
                long expected = -5;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test3f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min3g
        {
            // Type: long, selector function is called
            public static int Test3g()
            {
                Data_long[] source = new Data_long[]{ new Data_long{name="Tim", num=10L},
                                                new Data_long{name="John", num=Int64.MinValue},
                                                new Data_long{name="Bob", num=-10L}
            };
                long expected = Int64.MinValue;

                var actual = source.Min((e) => e.num);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test3g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min4a
        {
            // Type: long?, source is empty, null is returned
            public static int Test4a()
            {
                long?[] source = { };
                long? expected = null;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test4a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min4b
        {
            // Type: long?, source has only one element
            public static int Test4b()
            {
                long?[] source = { Int64.MaxValue };
                long? expected = Int64.MaxValue;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test4b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min4c
        {
            // Type: long?, source has all null values
            public static int Test4c()
            {
                long?[] source = { null, null, null, null, null };
                long? expected = null;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test4c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min4d
        {
            // Type: long, Minimum occurs as the first element
            public static int Test4d()
            {
                long?[] source = { Int64.MinValue, null, 9, 10, null, 7, 8 };
                long? expected = Int64.MinValue;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test4d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min4e
        {
            // Type: long, minimum value occurs as the last element
            public static int Test4e()
            {
                long?[] source = { null, null, null, null, null, -Int32.MaxValue };
                long? expected = -Int32.MaxValue;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test4e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min4f
        {
            // Type: long, minimum value occurs 2/3 times
            public static int Test4f()
            {
                long?[] source = { 6, null, null, 0, 9, 0, 10, 0 };
                long? expected = 0;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test4f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min4g
        {
            // Type: long?, selector function is called
            public static int Test4g()
            {
                Data_Nlong[] source = new Data_Nlong[]{ new Data_Nlong{name="Tim", num=null},
                                                new Data_Nlong{name="John", num=Int64.MinValue},
                                                new Data_Nlong{name="Bob", num=-10L}
            };
                long? expected = Int64.MinValue;

                var actual = source.Min((e) => e.num);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test4g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5a
        {
            // Type: double, source is empty
            public static int Test5a()
            {
                double[] source = { };

                try
                {
                    var actual = source.Min();
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test5a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5b
        {
            // Type: double, source has only one element
            public static int Test5b()
            {
                double[] source = { 5.5 };
                double expected = 5.5;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5c
        {
            // Type: double, source has all equal values
            public static int Test5c()
            {
                double[] source = { Double.NaN, Double.NaN, Double.NaN, Double.NaN };
                double expected = Double.NaN;

                var actual = source.Min();

                return ((expected.Equals(actual)) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5d
        {
            // Type: double, minimum value occurs as the first element
            public static int Test5d()
            {
                double[] source = { -2.5, 4.9, 130, 4.7, 28 };
                double expected = -2.5;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5e
        {
            // Type: double, minimum value occurs as the last element
            public static int Test5e()
            {
                double[] source = { 6.8, 9.4, 10, 0, -5.6 };
                double expected = -5.6;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5f
        {
            // Type: double, minimum value occurs 2/3 times
            public static int Test5f()
            {
                double[] source = { -5.5, Double.NegativeInfinity, 9.9, Double.NegativeInfinity };
                double expected = Double.NegativeInfinity;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5g
        {
            // Type: double, selector function is called
            public static int Test5g()
            {
                Data_double[] source = new Data_double[]{ new Data_double{name="Tim", num=-45.5},
                                                new Data_double{name="John", num=-132.5},
                                                new Data_double{name="Bob", num=20.45}
            };
                double expected = -132.5;

                var actual = source.Min((e) => e.num);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5h
        {
            // Type: double, NaN occurs as the 1st element
            public static int Test5h()
            {
                double[] source = { double.NaN, 6.8, 9.4, 10, 0, -5.6 };

                var actual = source.Min();

                return (double.IsNaN(actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5h();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5i
        {
            // Type: double, NaN occurs as the last element
            public static int Test5i()
            {
                double[] source = { 6.8, 9.4, 10, 0, -5.6, double.NaN };

                var actual = source.Min();

                return (double.IsNaN(actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5i();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5j
        {
            // Type: double, NaN occurs as the 1st element and the other element is -ve infinity
            public static int Test5j()
            {
                double[] source = { double.NaN, double.NegativeInfinity };

                var actual = source.Min();

                return (double.IsNaN(actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5j();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5k
        {
            // Type: double, NaN occurs as the last element and the other element is -ve infinity
            public static int Test5k()
            {
                double[] source = { double.NegativeInfinity, double.NaN, };

                var actual = source.Min();

                return (double.IsNaN(actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5k();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min5l
        {
            // Type: double, source element has only NaN's
            public static int Test5l()
            {
                double[] source = { double.NaN, double.NaN, double.NaN };

                var actual = source.Min();

                return (double.IsNaN(actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test5l();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6a
        {
            // Type: double?, source is empty, null is returned
            public static int Test6a()
            {
                double?[] source = { };
                double? expected = null;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6b
        {
            // Type: double?, source has only one element
            public static int Test6b()
            {
                double?[] source = { Double.MinValue };
                double? expected = Double.MinValue;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6c
        {
            // Type: double?, source has all null values
            public static int Test6c()
            {
                double?[] source = { null, null, null, null, null };
                double? expected = null;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6d
        {
            // Type: double?, Minimum occurs as the first element
            public static int Test6d()
            {
                double?[] source = { -4.50, null, 10.98, null, 7.5, 8.6 };
                double? expected = -4.50;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6e
        {
            // Type: double?, minimum value occurs as the last element
            public static int Test6e()
            {
                double?[] source = { null, null, null, null, null, 0 };
                double? expected = 0;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6f
        {
            // Type: double?, minimum value occurs 2/3 times
            public static int Test6f()
            {
                double?[] source = { 6.4, null, null, -0.5, 9.4, -0.5, 10.9, -0.5 };
                double? expected = -0.5;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6g
        {
            // Type: double?, selector function is called
            public static int Test6g()
            {
                Data_Ndouble[] source = new Data_Ndouble[]{ new Data_Ndouble{name="Tim", num=-45.5},
                                                new Data_Ndouble{name="John", num=-132.5},
                                                new Data_Ndouble{name="Bob", num=null}
            };
                double expected = -132.5;

                var actual = source.Min((e) => e.num);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6h
        {
            // Type: double?, NaN occurs as the 1st element and the sequence includes null
            public static int Test6h()
            {
                double?[] source = { double.NaN, 6.8, 9.4, 10.0, 0.0, null, -5.6 };

                var actual = source.Min();

                return (double.IsNaN((double)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6h();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6i
        {
            // Type: double?, NaN occurs as the last element and the sequence includes null
            public static int Test6i()
            {
                double?[] source = { 6.8, 9.4, 10, 0.0, null, -5.6f, double.NaN };

                var actual = source.Min();

                return (double.IsNaN((double)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6i();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6j
        {
            // Type: double?, NaN occurs as the 1st element and the other element is -ve infinity
            public static int Test6j()
            {
                double?[] source = { double.NaN, double.NegativeInfinity };

                var actual = source.Min();

                return (double.IsNaN((double)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6j();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6k
        {
            // Type: double?, NaN occurs as the last element and the other element is -ve infinity
            public static int Test6k()
            {
                double?[] source = { double.NegativeInfinity, double.NaN };

                var actual = source.Min();

                return (double.IsNaN((double)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6k();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6l
        {
            // Type: double?, source element has only NaN's
            public static int Test6l()
            {
                double?[] source = { double.NaN, double.NaN, double.NaN };

                var actual = source.Min();

                return (double.IsNaN((double)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6l();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6m
        {
            // Type: double?, NaN occurs as the first element and the rest are null's
            public static int Test6m()
            {
                double?[] source = { double.NaN, null, null, null };

                var actual = source.Min();

                return (double.IsNaN((double)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6m();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min6n
        {
            // Type: double?, NaN occurs as the last element and the rest are null's
            public static int Test6n()
            {
                double?[] source = { null, null, null, double.NaN };

                var actual = source.Min();

                return (double.IsNaN((double)actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test6n();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min7a
        {
            // Type: decimal, source is empty
            public static int Test7a()
            {
                decimal[] source = { };

                try
                {
                    var actual = source.Min();
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test7a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min7b
        {
            // Type: decimal, source has only one element
            public static int Test7b()
            {
                decimal[] source = { 5.5m };
                decimal expected = 5.5m;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test7b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min7c
        {
            // Type: decimal, source has all equal values
            public static int Test7c()
            {
                decimal[] source = { -3.4m, -3.4m, -3.4m, -3.4m, -3.4m };
                decimal expected = -3.4m;

                var actual = source.Min();

                return ((expected.Equals(actual)) ? 0 : 1);
            }


            public static int Main()
            {
                return Test7c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min7d
        {
            // Type: decimal, minimum value occurs as the first element
            public static int Test7d()
            {
                decimal[] source = { -2.5m, 4.9m, 130m, 4.7m, 28m };
                decimal expected = -2.5m;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test7d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min7e
        {
            // Type: decimal, minimum value occurs as the last element
            public static int Test7e()
            {
                decimal[] source = { 6.8m, 9.4m, 10m, 0m, 0m, Decimal.MinValue };
                decimal expected = Decimal.MinValue;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test7e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min7f
        {
            // Type: decimal, minimum value occurs 2/3 times
            public static int Test7f()
            {
                decimal[] source = { -5.5m, 0m, 9.9m, -5.5m, 5m };
                decimal expected = -5.5m;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test7f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min7g
        {
            // Type: decimal, selector function is called
            public static int Test7g()
            {
                Data_decimal[] source = new Data_decimal[]{ new Data_decimal{name="Tim", num=100.45m},
                                                new Data_decimal{name="John", num=10.5m},
                                                new Data_decimal{name="Bob", num=0.05m}
            };
                decimal expected = 0.05m;

                var actual = source.Min((e) => e.num);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test7g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min8a
        {
            // Type: decimal?, source is empty, null is returned
            public static int Test8a()
            {
                decimal?[] source = { };
                decimal? expected = null;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test8a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min8b
        {
            // Type: decimal?, source has only one element
            public static int Test8b()
            {
                decimal?[] source = { Decimal.MaxValue };
                decimal? expected = Decimal.MaxValue;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test8b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min8c
        {
            // Type: decimal?, source has all null values
            public static int Test8c()
            {
                decimal?[] source = { null, null, null, null, null };
                decimal? expected = null;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test8c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min8d
        {
            // Type: decimal?, Minimum occurs as the first element
            public static int Test8d()
            {
                decimal?[] source = { -4.50m, null, null, 10.98m, null, 7.5m, 8.6m };
                decimal? expected = -4.50m;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test8d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min8e
        {
            // Type: decimal?, Minimum value occurs as the last element
            public static int Test8e()
            {
                decimal?[] source = { null, null, null, null, null, 0m };
                decimal? expected = 0m;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test8e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min8f
        {
            // Type: decimal?, minimum value occurs 2/3 times
            public static int Test8f()
            {
                decimal?[] source = { 6.4m, null, null, Decimal.MinValue, 9.4m, Decimal.MinValue, 10.9m, Decimal.MinValue };
                decimal? expected = Decimal.MinValue;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test8f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min8g
        {
            // Type: decimal?, selector function is called
            public static int Test8g()
            {
                Data_Ndecimal[] source = new Data_Ndecimal[]{ new Data_Ndecimal{name="Tim", num=100.45m},
                                                new Data_Ndecimal{name="John", num=10.5m},
                                                new Data_Ndecimal{name="Bob", num=null}
            };
                decimal expected = 10.5m;

                var actual = source.Min((e) => e.num);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test8g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min9a
        {
            // Type: bool, source is empty,  default(T) is NOT null
            public static int Test9a()
            {
                bool[] source = { };

                try
                {
                    var actual = source.Min();
                    return 1;
                }
                catch (InvalidOperationException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test9a();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min9b
        {
            // Type: string, source has only one element
            public static int Test9b()
            {
                string[] source = { "Hello" };
                string expected = "Hello";

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test9b();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min9c
        {
            // Type: string, source has all equal values
            public static int Test9c()
            {
                string[] source = { "hi", "hi", "hi", "hi" };
                string expected = "hi";

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test9c();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min9d
        {
            // Type: string, minimum value occurs as the first element
            public static int Test9d()
            {
                string[] source = { "aaa", "abcd", "bark", "temp", "cat" };
                string expected = "aaa";

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test9d();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min9e
        {
            // Type: string, minimum value occurs as the last element
            public static int Test9e()
            {
                string[] source = { null, null, null, null, "aAa" };
                string expected = "aAa";

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test9e();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min9f
        {
            // Type: string, minimum value occurs 2/3 times
            public static int Test9f()
            {
                string[] source = { "ooo", "www", "www", "ooo", "ooo", "ppp" };
                string expected = "ooo";

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test9f();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min9g
        {
            // Type: string, source has all null values
            public static int Test9g()
            {
                string[] source = { null, null, null, null, null };
                string expected = null;

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test9g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min9h
        {
            // Type: string, selector function is called
            public static int Test9h()
            {
                Data_string[] source = new Data_string[]{ new Data_string{name="Tim", num=100.45m},
                                                new Data_string{name="John", num=10.5m},
                                                new Data_string{name="Bob", num=0.05m}
            };
                string expected = "Bob";

                var actual = source.Min((e) => e.name);

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test9h();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Min9i
        {
            // Type: string, source is empty,  default(T) is null 
            public static int Test9i()
            {
                string[] source = { };
                string expected = default(string);

                var actual = source.Min();

                return ((expected == actual) ? 0 : 1);
            }


            public static int Main()
            {
                return Test9i();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
