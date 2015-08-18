// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class MaxTests
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

        public class Max012
        {
            private static int Max001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Max();
                var rst2 = q.Max();

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Max002()
            {
                var q = from x in new[] { "!@#$%^", "C", "AAA", "", "Calling Twice", "SoS", String.Empty }
                        where !String.IsNullOrEmpty(x)
                        select x;

                var rst1 = q.Max<string>();
                var rst2 = q.Max<string>();

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(Max001) + RunTest(Max002);
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

        public class Max10a
        {
            // Type: float, source is empty
            public static int Test10a()
            {
                float[] source = { };

                try
                {
                    var actual = source.Max();
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

        public class Max10b
        {
            // Type: float, source has only one element
            public static int Test10b()
            {
                float[] source = { 5.5f };
                float expected = 5.5f;

                var actual = source.Max();

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

        public class Max10c
        {
            // Type: float, source has all equal values
            public static int Test10c()
            {
                float[] source = { float.NaN, float.NaN, float.NaN, float.NaN };
                float expected = float.NaN;

                var actual = source.Max();

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

        public class Max10d
        {
            // Type: float, maximum value occurs as the first element
            public static int Test10d()
            {
                float[] source = { 112.5f, 4.9f, 30f, 4.7f, 28f };
                float expected = 112.5f;

                var actual = source.Max();

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

        public class Max10e
        {
            // Type: float, maximum value occurs as the last element
            public static int Test10e()
            {
                float[] source = { 6.8f, 9.4f, -10f, 0f, float.NaN, 53.6f };
                float expected = 53.6f;

                var actual = source.Max();

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

        public class Max10f
        {
            // Type: float, maximum value occurs 2/3 times
            public static int Test10f()
            {
                float[] source = { -5.5f, float.PositiveInfinity, 9.9f, float.PositiveInfinity };
                float expected = float.PositiveInfinity;

                var actual = source.Max();

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

        public class Max10g
        {
            // Type: float, selector function is called
            public static int Test10g()
            {
                Data_float[] source = new Data_float[]{ new Data_float{name="Tim", num=40.5f},
                                                new Data_float{name="John", num=-10.25f},
                                                new Data_float{name="Bob", num=100.45f}
            };
                float expected = 100.45f;

                var actual = source.Max((e) => e.num);

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

        public class Max10h
        {
            // Type: float, source is null
            public static int Test10h()
            {
                float[] source = { -5.5f, float.PositiveInfinity, 9.9f, float.PositiveInfinity };

                try
                {
                    source = null;
                    var actual = source.Max();
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

        public class Max10i
        {
            // Type: float, NaN occurs as the 1st element
            public static int Test10i()
            {
                float[] source = { float.NaN, 6.8f, 9.4f, 10f, 0, -5.6f };
                float expected = 10f;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max10j
        {
            // Type: float, NaN occurs as the last element
            public static int Test10j()
            {
                float[] source = { 6.8f, 9.4f, 10f, 0, -5.6f, float.NaN };
                float expected = 10f;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max10k
        {
            // Type: float, NaN occurs as the 1st element and the other element is -ve infinity
            public static int Test10k()
            {
                float[] source = { float.NaN, float.NegativeInfinity };
                float expected = float.NegativeInfinity;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max10l
        {
            // Type: float, NaN occurs as the last element and the other element is -ve infinity
            public static int Test10l()
            {
                float[] source = { float.NegativeInfinity, float.NaN };
                float expected = float.NegativeInfinity;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max10m
        {
            // Type: float, source element has only NaN's
            public static int Test10m()
            {
                float[] source = { float.NaN, float.NaN, float.NaN };

                var actual = source.Max();

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

        public class Max11a
        {
            // Type: float?, source is empty, null is returned
            public static int Test11a()
            {
                float?[] source = { };
                float? expected = null;

                var actual = source.Max();

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

        public class Max11b
        {
            // Type: float?, source has only one element
            public static int Test11b()
            {
                float?[] source = { float.MinValue };
                float? expected = float.MinValue;

                var actual = source.Max();

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

        public class Max11c
        {
            // Type: float?, source has all null values
            public static int Test11c()
            {
                float?[] source = { null, null, null, null, null };
                float? expected = null;

                var actual = source.Max();

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

        public class Max11d
        {
            // Type: float?, Maximum occurs as the first element
            public static int Test11d()
            {
                float?[] source = { 14.50f, null, float.NaN, 10.98f, null, 7.5f, 8.6f };
                float? expected = 14.50f;

                var actual = source.Max();

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

        public class Max11e
        {
            // Type: float?, maximum value occurs as the last element
            public static int Test11e()
            {
                float?[] source = { null, null, null, null, null, 0f };
                float? expected = 0f;

                var actual = source.Max();

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

        public class Max11f
        {
            // Type: float?, maximum value occurs 2/3 times
            public static int Test11f()
            {
                float?[] source = { -6.4f, null, null, -0.5f, -9.4f, -0.5f, -10.9f, -0.5f };
                float? expected = -0.5f;

                var actual = source.Max();

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

        public class Max11g
        {
            // Type: float?, selector function is called
            public static int Test11g()
            {
                Data_Nfloat[] source = new Data_Nfloat[]{ new Data_Nfloat{name="Tim", num=40.5f},
                                                new Data_Nfloat{name="John", num=null},
                                                new Data_Nfloat{name="Bob", num=100.45f}
            };
                float? expected = 100.45f;

                var actual = source.Max((e) => e.num);

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

        public class Max11h
        {
            // Type: float?, source is null
            public static int Test11h()
            {
                float?[] source = { -6.4f, null, null, -0.5f, -9.4f, -0.5f, -10.9f, -0.5f };

                try
                {
                    source = null;
                    var actual = source.Max();
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

        public class Max11i
        {
            // Type: float?, NaN occurs as the 1st element and the sequence includes null
            public static int Test11i()
            {
                float?[] source = { float.NaN, 6.8f, 9.4f, 10f, 0, null, -5.6f };
                float? expected = 10f;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max11j
        {
            // Type: float?, NaN occurs as the last element and the sequence includes null
            public static int Test11j()
            {
                float?[] source = { 6.8f, 9.4f, 10f, 0, null, -5.6f, float.NaN };
                float? expected = 10f;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max11k
        {
            // Type: float?, NaN occurs as the 1st element and the other element is -ve infinity
            public static int Test11k()
            {
                float?[] source = { float.NaN, float.NegativeInfinity };
                float? expected = float.NegativeInfinity;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max11l
        {
            // Type: float?, NaN occurs as the last element and the other element is -ve infinity
            public static int Test11l()
            {
                float?[] source = { float.NegativeInfinity, float.NaN };
                float? expected = float.NegativeInfinity;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max11m
        {
            // Type: float?, source element has only NaN's
            public static int Test11m()
            {
                float?[] source = { float.NaN, float.NaN, float.NaN };

                var actual = source.Max();

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

        public class Max11n
        {
            // Type: float?, NaN occurs as the first element and the rest are null's
            public static int Test11n()
            {
                float?[] source = { float.NaN, null, null, null };

                var actual = source.Max();

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

        public class Max11o
        {
            // Type: float?, NaN occurs as the last element and the rest are null's
            public static int Test11o()
            {
                float?[] source = { null, null, null, float.NaN };

                var actual = source.Max();

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

        public class Max1a
        {
            // Type: int, source is empty
            public static int Test1a()
            {
                int[] source = { };

                try
                {
                    var actual = source.Max();
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

        public class Max1b
        {
            // Type: int, source has only one element
            public static int Test1b()
            {
                int[] source = { 20 };
                int expected = 20;

                var actual = source.Max();

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

        public class Max1c
        {
            // Type: int, source has all equal values
            public static int Test1c()
            {
                int[] source = { -2, -2, -2, -2, -2 };
                int expected = -2;

                var actual = source.Max();

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

        public class Max1d
        {
            // Type: int, maximum value occurs as the first element
            public static int Test1d()
            {
                int[] source = { 16, 9, 10, 7, 8 };
                int expected = 16;

                var actual = source.Max();

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

        public class Max1e
        {
            // Type: int, maximum value occurs as the last element
            public static int Test1e()
            {
                int[] source = { 6, 9, 10, 0, 50 };
                int expected = 50;

                var actual = source.Max();

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

        public class Max1f
        {
            // Type: int, maximum value occurs 2/3 times
            public static int Test1f()
            {
                int[] source = { -6, 0, -9, 0, -10, 0 };
                int expected = 0;

                var actual = source.Max();

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

        public class Max1g
        {
            // Type: int, selector function is called
            public static int Test1g()
            {
                Data_int[] source = new Data_int[]{ new Data_int{name="Tim", num=10},
                                                new Data_int{name="John", num=-105},
                                                new Data_int{name="Bob", num=30}
            };
                int expected = 30;

                var actual = source.Max((e) => e.num);

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

        public class Max2a
        {
            // Type: int?, source is empty, null is returned
            public static int Test2a()
            {
                int?[] source = { };
                int? expected = null;

                var actual = source.Max();

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

        public class Max2b
        {
            // Type: int?, source has only one element
            public static int Test2b()
            {
                int?[] source = { -20 };
                int? expected = -20;

                var actual = source.Max();

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

        public class Max2c
        {
            // Type: int?, source has all null values
            public static int Test2c()
            {
                int?[] source = { null, null, null, null, null };
                int? expected = null;

                var actual = source.Max();

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

        public class Max2d
        {
            // Type: int?, Maximum occurs as the first element
            public static int Test2d()
            {
                int?[] source = { -6, null, -9, -10, null, -17, -18 };
                int? expected = -6;

                var actual = source.Max();

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

        public class Max2e
        {
            // Type: int?, Maximum value occurs as the last element
            public static int Test2e()
            {
                int?[] source = { null, null, null, null, null, -5 };
                int? expected = -5;

                var actual = source.Max();

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

        public class Max2f
        {
            // Type: int?, Maximum value occurs 2/3 times
            public static int Test2f()
            {
                int?[] source = { 6, null, null, 100, 9, 100, 10, 100 };
                int? expected = 100;

                var actual = source.Max();

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

        public class Max2g
        {
            // Type: int?, selector function is called
            public static int Test2g()
            {
                Data_Nint[] source = new Data_Nint[]{ new Data_Nint{name="Tim", num=10},
                                                new Data_Nint{name="John", num=-105},
                                                new Data_Nint{name="Bob", num=null}
            };
                int? expected = 10;

                var actual = source.Max((e) => e.num);

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

        public class Max3a
        {
            // Type: long, source is empty
            public static int Test3a()
            {
                long[] source = { };

                try
                {
                    var actual = source.Max();
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

        public class Max3b
        {
            // Type: long, source has only one element
            public static int Test3b()
            {
                long[] source = { (long)Int32.MaxValue + 10 };
                long expected = (long)Int32.MaxValue + 10;

                var actual = source.Max();

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

        public class Max3c
        {
            // Type: long, source has all equal values
            public static int Test3c()
            {
                long[] source = { 500, 500, 500, 500, 500 };
                long expected = 500;

                var actual = source.Max();

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

        public class Max3d
        {
            // Type: long, maximum value occurs as the first element
            public static int Test3d()
            {
                long[] source = { 250, 49, 130, 47, 28 };
                long expected = 250;

                var actual = source.Max();

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

        public class Max3e
        {
            // Type: long, maximum value occurs as the last element
            public static int Test3e()
            {
                long[] source = { 6, 9, 10, 0, (long)Int32.MaxValue + 50 };
                long expected = (long)Int32.MaxValue + 50;

                var actual = source.Max();

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

        public class Max3f
        {
            // Type: long, maximum value occurs 2/3 times
            public static int Test3f()
            {
                long[] source = { 6, 50, 9, 50, 10, 50 };
                long expected = 50;

                var actual = source.Max();

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

        public class Max3g
        {
            // Type: long, selector function is called
            public static int Test3g()
            {
                Data_long[] source = new Data_long[]{ new Data_long{name="Tim", num=10L},
                                                new Data_long{name="John", num=-105L},
                                                new Data_long{name="Bob", num=Int64.MaxValue}
            };
                long expected = Int64.MaxValue;

                var actual = source.Max((e) => e.num);

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

        public class Max4a
        {
            // Type: long?, source is empty, null is returned
            public static int Test4a()
            {
                long?[] source = { };
                long? expected = null;

                var actual = source.Max();

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

        public class Max4b
        {
            // Type: long?, source has only one element
            public static int Test4b()
            {
                long?[] source = { Int64.MaxValue };
                long? expected = Int64.MaxValue;

                var actual = source.Max();

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

        public class Max4c
        {
            // Type: long?, source has all null values
            public static int Test4c()
            {
                long?[] source = { null, null, null, null, null };
                long? expected = null;

                var actual = source.Max();

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

        public class Max4d
        {
            // Type: long, Maximum occurs as the first element
            public static int Test4d()
            {
                long?[] source = { Int64.MaxValue, null, 9, 10, null, 7, 8 };
                long? expected = Int64.MaxValue;

                var actual = source.Max();

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

        public class Max4e
        {
            // Type: long, maximum value occurs as the last element
            public static int Test4e()
            {
                long?[] source = { null, null, null, null, null, -Int32.MaxValue };
                long? expected = -Int32.MaxValue;

                var actual = source.Max();

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

        public class Max4f
        {
            // Type: long, maximum value occurs 2/3 times
            public static int Test4f()
            {
                long?[] source = { -6, null, null, 0, -9, 0, -10, -30 };
                long? expected = 0;

                var actual = source.Max();

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

        public class Max4g
        {
            // Type: long?, selector function is called
            public static int Test4g()
            {
                Data_Nlong[] source = new Data_Nlong[]{ new Data_Nlong{name="Tim", num=null},
                                                new Data_Nlong{name="John", num=-105L},
                                                new Data_Nlong{name="Bob", num=Int64.MaxValue}
            };
                long? expected = Int64.MaxValue;

                var actual = source.Max((e) => e.num);

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

        public class Max5a
        {
            // Type: double, source is empty
            public static int Test5a()
            {
                double[] source = { };

                try
                {
                    var actual = source.Max();
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

        public class Max5b
        {
            // Type: double, source has only one element
            public static int Test5b()
            {
                double[] source = { 5.5 };
                double expected = 5.5;

                var actual = source.Max();

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

        public class Max5c
        {
            // Type: double, source has all equal values
            public static int Test5c()
            {
                double[] source = { Double.NaN, Double.NaN, Double.NaN, Double.NaN };
                double expected = Double.NaN;

                var actual = source.Max();

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

        public class Max5d
        {
            // Type: double, maximum value occurs as the first element
            public static int Test5d()
            {
                double[] source = { 112.5, 4.9, 30, 4.7, 28 };
                double expected = 112.5;

                var actual = source.Max();

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

        public class Max5e
        {
            // Type: double, maximum value occurs as the last element
            public static int Test5e()
            {
                double[] source = { 6.8, 9.4, -10, 0, Double.NaN, 53.6 };
                double expected = 53.6;

                var actual = source.Max();

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

        public class Max5f
        {
            // Type: double, maximum value occurs 2/3 times
            public static int Test5f()
            {
                double[] source = { -5.5, Double.PositiveInfinity, 9.9, Double.PositiveInfinity };
                double expected = Double.PositiveInfinity;

                var actual = source.Max();

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

        public class Max5g
        {
            // Type: double, selector function is called
            public static int Test5g()
            {
                Data_double[] source = new Data_double[]{ new Data_double{name="Tim", num=40.5},
                                                new Data_double{name="John", num=-10.25},
                                                new Data_double{name="Bob", num=100.45}
            };
                double expected = 100.45;

                var actual = source.Max((e) => e.num);

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

        public class Max5h
        {
            // Type: double, NaN occurs as the 1st element
            public static int Test5h()
            {
                double[] source = { double.NaN, 6.8, 9.4, 10.5, 0, -5.6 };
                double expected = 10.5;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max5i
        {
            // Type: double, NaN occurs as the last element
            public static int Test5i()
            {
                double[] source = { 6.8, 9.4, 10.5, 0, -5.6, double.NaN };
                double expected = 10.5;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max5j
        {
            // Type: double, NaN occurs as the 1st element and the other element is -ve infinity
            public static int Test5j()
            {
                double[] source = { double.NaN, double.NegativeInfinity };
                double expected = double.NegativeInfinity;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max5k
        {
            // Type: double, NaN occurs as the last element and the other element is -ve infinity
            public static int Test5k()
            {
                double[] source = { double.NegativeInfinity, double.NaN, };
                double expected = double.NegativeInfinity;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max5l
        {
            // Type: double, source element has only NaN's
            public static int Test5l()
            {
                double[] source = { double.NaN, double.NaN, double.NaN };

                var actual = source.Max();

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

        public class Max6a
        {
            // Type: double?, source is empty, null is returned
            public static int Test6a()
            {
                double?[] source = { };
                double? expected = null;

                var actual = source.Max();

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

        public class Max6b
        {
            // Type: double?, source has only one element
            public static int Test6b()
            {
                double?[] source = { Double.MinValue };
                double? expected = Double.MinValue;

                var actual = source.Max();

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

        public class Max6c
        {
            // Type: double?, source has all null values
            public static int Test6c()
            {
                double?[] source = { null, null, null, null, null };
                double? expected = null;

                var actual = source.Max();

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

        public class Max6d
        {
            // Type: double?, Maximum occurs as the first element
            public static int Test6d()
            {
                double?[] source = { 14.50, null, Double.NaN, 10.98, null, 7.5, 8.6 };
                double? expected = 14.50;

                var actual = source.Max();

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

        public class Max6e
        {
            // Type: double?, maximum value occurs as the last element
            public static int Test6e()
            {
                double?[] source = { null, null, null, null, null, 0 };
                double? expected = 0;

                var actual = source.Max();

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

        public class Max6f
        {
            // Type: double?, maximum value occurs 2/3 times
            public static int Test6f()
            {
                double?[] source = { -6.4, null, null, -0.5, -9.4, -0.5, -10.9, -0.5 };
                double? expected = -0.5;

                var actual = source.Max();

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

        public class Max6g
        {
            // Type: double?, selector function is called
            public static int Test6g()
            {
                Data_Ndouble[] source = new Data_Ndouble[]{ new Data_Ndouble{name="Tim", num=40.5},
                                                new Data_Ndouble{name="John", num=null},
                                                new Data_Ndouble{name="Bob", num=100.45}
            };
                double expected = 100.45;

                var actual = source.Max((e) => e.num);

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

        public class Max6h
        {
            // Type: double?, NaN occurs as the 1st element and the sequence includes null
            public static int Test6h()
            {
                double?[] source = { double.NaN, 6.8, 9.4, 10.5, 0, null, -5.6 };
                double? expected = 10.5;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max6i
        {
            // Type: double?, NaN occurs as the last element and the sequence includes null
            public static int Test6i()
            {
                double?[] source = { 6.8, 9.4, 10.8, 0, null, -5.6, double.NaN };
                double? expected = 10.8;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max6j
        {
            // Type: double?, NaN occurs as the 1st element and the other element is -ve infinity
            public static int Test6j()
            {
                double?[] source = { double.NaN, double.NegativeInfinity };
                double? expected = double.NegativeInfinity;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max6k
        {
            // Type: double?, NaN occurs as the last element and the other element is -ve infinity
            public static int Test6k()
            {
                double?[] source = { double.NegativeInfinity, double.NaN };
                double? expected = double.NegativeInfinity;

                var actual = source.Max();

                return ((expected == actual) ? 0 : 1);
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

        public class Max6l
        {
            // Type: double?, source element has only NaN's
            public static int Test6l()
            {
                double?[] source = { double.NaN, double.NaN, double.NaN };

                var actual = source.Max();

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

        public class Max6m
        {
            // Type: double?, NaN occurs as the first element and the rest are null's
            public static int Test6m()
            {
                double?[] source = { double.NaN, null, null, null };

                var actual = source.Max();

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

        public class Max6n
        {
            // Type: double?, NaN occurs as the last element and the rest are null's
            public static int Test6n()
            {
                double?[] source = { null, null, null, double.NaN };

                var actual = source.Max();

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

        public class Max7a
        {
            // Type: decimal, source is empty
            public static int Test7a()
            {
                decimal[] source = { };

                try
                {
                    var actual = source.Max();
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

        public class Max7b
        {
            // Type: decimal, source has only one element
            public static int Test7b()
            {
                decimal[] source = { 5.5m };
                decimal expected = 5.5m;

                var actual = source.Max();

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

        public class Max7c
        {
            // Type: decimal, source has all equal values
            public static int Test7c()
            {
                decimal[] source = { -3.4m, -3.4m, -3.4m, -3.4m, -3.4m };
                decimal expected = -3.4m;

                var actual = source.Max();

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

        public class Max7d
        {
            // Type: decimal, maximum value occurs as the first element
            public static int Test7d()
            {
                decimal[] source = { 122.5m, 4.9m, 10m, 4.7m, 28m };
                decimal expected = 122.5m;

                var actual = source.Max();

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

        public class Max7e
        {
            // Type: decimal, maximum value occurs as the last element
            public static int Test7e()
            {
                decimal[] source = { 6.8m, 9.4m, 10m, 0m, 0m, Decimal.MaxValue };
                decimal expected = Decimal.MaxValue;

                var actual = source.Max();

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

        public class Max7f
        {
            // Type: decimal, maximum value occurs 2/3 times
            public static int Test7f()
            {
                decimal[] source = { -5.5m, 0m, 9.9m, -5.5m, 9.9m };
                decimal expected = 9.9m;

                var actual = source.Max();

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

        public class Max7g
        {
            // Type: decimal, selector function is called
            public static int Test7g()
            {
                Data_decimal[] source = new Data_decimal[]{ new Data_decimal{name="Tim", num=420.5m},
                                                new Data_decimal{name="John", num=900.25m},
                                                new Data_decimal{name="Bob", num=10.45m}
            };
                decimal expected = 900.25m;

                var actual = source.Max((e) => e.num);

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

        public class Max8a
        {
            // Type: decimal?, source is empty, null is returned
            public static int Test8a()
            {
                decimal?[] source = { };
                decimal? expected = null;

                var actual = source.Max();

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

        public class Max8b
        {
            // Type: decimal?, source has only one element
            public static int Test8b()
            {
                decimal?[] source = { Decimal.MaxValue };
                decimal? expected = Decimal.MaxValue;

                var actual = source.Max();

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

        public class Max8c
        {
            // Type: decimal?, source has all null values
            public static int Test8c()
            {
                decimal?[] source = { null, null, null, null, null };
                decimal? expected = null;

                var actual = source.Max();

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

        public class Max8d
        {
            // Type: decimal?, Maximum occurs as the first element
            public static int Test8d()
            {
                decimal?[] source = { 14.50m, null, null, 10.98m, null, 7.5m, 8.6m };
                decimal? expected = 14.50m;

                var actual = source.Max();

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

        public class Max8e
        {
            // Type: decimal?, Maximum value occurs as the last element
            public static int Test8e()
            {
                decimal?[] source = { null, null, null, null, null, 0m };
                decimal? expected = 0m;

                var actual = source.Max();

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

        public class Max8f
        {
            // Type: decimal?, maximum value occurs 2/3 times
            public static int Test8f()
            {
                decimal?[] source = { 6.4m, null, null, Decimal.MaxValue, 9.4m, Decimal.MaxValue, 10.9m, Decimal.MaxValue };
                decimal? expected = Decimal.MaxValue;

                var actual = source.Max();

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

        public class Max8g
        {
            // Type: decimal?, selector function is called
            public static int Test8g()
            {
                Data_Ndecimal[] source = new Data_Ndecimal[]{ new Data_Ndecimal{name="Tim", num=420.5m},
                                                new Data_Ndecimal{name="John", num=null},
                                                new Data_Ndecimal{name="Bob", num=10.45m}
            };
                decimal expected = 420.5m;

                var actual = source.Max((e) => e.num);

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

        public class Max9a
        {
            // Type: bool, source is empty, default(T) is NOT null
            public static int Test9a()
            {
                bool[] source = { };

                try
                {
                    var actual = source.Max();
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

        public class Max9b
        {
            // Type: string, source has only one element
            public static int Test9b()
            {
                string[] source = { "Hello" };
                string expected = "Hello";

                var actual = source.Max();

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

        public class Max9c
        {
            // Type: string, source has all equal values
            public static int Test9c()
            {
                string[] source = { "hi", "hi", "hi", "hi" };
                string expected = "hi";

                var actual = source.Max();

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

        public class Max9d
        {
            // Type: string, maximum value occurs as the first element
            public static int Test9d()
            {
                string[] source = { "zzz", "aaa", "abcd", "bark", "temp", "cat" };
                string expected = "zzz";

                var actual = source.Max();

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

        public class Max9e
        {
            // Type: string, maximum value occurs as the last element
            public static int Test9e()
            {
                string[] source = { null, null, null, null, "aAa" };
                string expected = "aAa";

                var actual = source.Max();

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

        public class Max9f
        {
            // Type: string, maximum value occurs 2/3 times
            public static int Test9f()
            {
                string[] source = { "ooo", "ccc", "ccc", "ooo", "ooo", "nnn" };
                string expected = "ooo";

                var actual = source.Max();

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

        public class Max9g
        {
            // Type: string, source has all null values
            public static int Test9g()
            {
                string[] source = { null, null, null, null, null };
                string expected = null;

                var actual = source.Max();

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

        public class Max9h
        {
            // Type: string, selector function is called
            public static int Test9h()
            {
                Data_string[] source = new Data_string[]{ new Data_string{name="Tim", num=420.5m},
                                                new Data_string{name="John", num=900.25m},
                                                new Data_string{name="Bob", num=10.45m}
            };
                string expected = "Tim";

                var actual = source.Max((e) => e.name);

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

        public class Max9i
        {
            // Type: string, source is empty, default(T) is null
            public static int Test9i()
            {
                string[] source = { };
                string expected = default(string);

                var actual = source.Max();

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
