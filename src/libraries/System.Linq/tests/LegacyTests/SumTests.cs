// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class SumTests
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

        public class Sum011
        {
            private static int Sum001()
            {
                var q = from x in new int?[] { 9999, 0, 888, -1, 66, null, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Sum();
                var rst2 = q.Sum();

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Sum002()
            {
                var q = from x in new[] { 0m, -1M, 1M, 123.456789m, 9999 }
                        select x;

                var rst1 = q.Sum();
                var rst2 = q.Sum();

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(Sum001) + RunTest(Sum002);
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

        public class Sum10a
        {
            // Type: float?, source is empty
            public static int Test10a()
            {
                float?[] source = { };
                float? expected = 0;

                var actual = source.Sum();

                return ((expected == actual) ? 0 : 1);
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

        public class Sum10b
        {
            // Type: float?, source has only one element
            public static int Test10b()
            {
                float?[] source = { 20.51f };
                float? expected = 20.51f;

                var actual = source.Sum();

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

        public class Sum10c
        {
            // Type: float?, source has limited number of elements
            public static int Test10c()
            {
                float?[] source = { 20.45f, 0f, -10.55f, float.NaN };
                float? expected = float.NaN;

                var actual = source.Sum();

                return (expected.Equals(actual) ? 0 : 1);
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

        public class Sum10d
        {
            // Type: float?, source has only null as elements
            public static int Test10d()
            {
                float?[] source = { null, null, null, null };
                float? expected = 0;

                var actual = source.Sum();

                return (expected.Equals(actual) ? 0 : 1);
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

        public class Sum10e
        {
            // Type: float?, Positive Infinity is returned 
            public static int Test10e()
            {
                float?[] source = { float.MaxValue, float.MaxValue, 0f, 0f };

                var actual = source.Sum();

                return (float.IsPositiveInfinity((float)actual) ? 0 : 1);
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

        public class Sum10f
        {
            // Type: float?, Negative Infinity is returned
            public static int Test10f()
            {
                float?[] source = { -float.MaxValue, -float.MaxValue };

                float? actual = source.Sum();

                return (float.IsNegativeInfinity((float)actual) ? 0 : 1);
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

        public class Sum10g
        {
            // Type: float?, selector function is called
            public static int Test10g()
            {
                Data_Nfloat[] source = new Data_Nfloat[]{ new Data_Nfloat{name="Tim", num=9.5f},
                                                new Data_Nfloat{name="John", num=null},
                                                new Data_Nfloat{name="Bob", num=8.5f}
            };
                float? expected = 18.0f;

                var actual = source.Sum((e) => e.num);

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

        public class Sum10h
        {
            // Type: float?, source is null
            public static int Test10h()
            {
                float?[] source = { -float.MaxValue, -float.MaxValue };

                try
                {
                    source = null;
                    float? actual = source.Sum();
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

        public class Sum1a
        {
            // Type: int, source is empty
            public static int Test1a()
            {
                int[] source = { };
                int expected = 0;

                var actual = source.Sum();

                return ((expected == actual) ? 0 : 1);
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

        public class Sum1b
        {
            // Type: int, source has only one element
            public static int Test1b()
            {
                int[] source = { 20 };
                int expected = 20;

                var actual = source.Sum();

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

        public class Sum1c
        {
            // Type: int, source has limited number of elements
            public static int Test1c()
            {
                int[] source = { 20, 0, -10, 4 };
                int expected = 14;

                var actual = source.Sum();

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

        public class Sum1d
        {
            // Type: int, OverflowException is caused 
            public static int Test1d()
            {
                int[] source = { Int32.MaxValue, 0, -5, 20 };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum1e
        {
            // Type: int, negative OverflowException is caused 
            public static int Test1e()
            {
                int[] source = { -Int32.MaxValue, 0, -5, -20 };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum1f
        {
            // Type: int, selector function is called
            public static int Test1f()
            {
                Data_int[] source = new Data_int[]{ new Data_int{name="Tim", num=10},
                                                new Data_int{name="John", num=50},
                                                new Data_int{name="Bob", num=-30}
            };
                int expected = 30;

                var actual = source.Sum((e) => e.num);

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

        public class Sum2a
        {
            // Type: int?, source is empty
            public static int Test2a()
            {
                int?[] source = { };
                int? expected = 0;

                var actual = source.Sum();

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

        public class Sum2b
        {
            // Type: int?, source has only one element
            public static int Test2b()
            {
                int?[] source = { -9 };
                int? expected = -9;

                var actual = source.Sum();

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

        public class Sum2c
        {
            // Type: int?, source has limited number of elements includes null
            public static int Test2c()
            {
                int?[] source = { 20, 0, -30, null, null, -4 };
                int? expected = -14;

                var actual = source.Sum();

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

        public class Sum2d
        {
            // Type: int?, source has only null elements
            public static int Test2d()
            {
                int?[] source = { null, null, null, null, null };
                int? expected = 0;

                var actual = source.Sum();

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

        public class Sum2e
        {
            // Type: int?, OverflowException is caused 
            public static int Test2e()
            {
                int?[] source = { Int32.MaxValue, 0, -5, 20, null, null };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum2f
        {
            // Type: int?, negative OverflowException is caused 
            public static int Test2f()
            {
                int?[] source = { -Int32.MaxValue, 0, -5, null, null, -20 };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum2g
        {
            // Type: int?, selector function is called
            public static int Test2g()
            {
                Data_Nint[] source = new Data_Nint[]{ new Data_Nint{name="Tim", num=10},
                                                new Data_Nint{name="John", num=null},
                                                new Data_Nint{name="Bob", num=-30}
            };
                int? expected = -20;

                var actual = source.Sum((e) => e.num);

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

        public class Sum3a
        {
            // Type: long, source is empty
            public static int Test3a()
            {
                long[] source = { };
                int expected = 0;

                var actual = source.Sum();

                return ((expected == actual) ? 0 : 1);
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

        public class Sum3b
        {
            // Type: long, source has only one element
            public static int Test3b()
            {
                long[] source = { (long)Int32.MaxValue + 20 };
                long expected = (long)Int32.MaxValue + 20;

                var actual = source.Sum();

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

        public class Sum3c
        {
            // Type: long, source has limited number of elements
            public static int Test3c()
            {
                long[] source = { Int32.MaxValue, 0, -10, 4, 20 };
                long expected = (long)Int32.MaxValue + 14;

                var actual = source.Sum();

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

        public class Sum3d
        {
            // Type: long, OverflowException is caused 
            public static int Test3d()
            {
                long[] source = { Int64.MaxValue, 0, -5, 20 };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum3e
        {
            // Type: long, negative OverflowException is caused 
            public static int Test3e()
            {
                long[] source = { -Int64.MaxValue, 0, -5, 20, -16 };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum3f
        {
            // Type: long, selector function is called
            public static int Test3f()
            {
                Data_long[] source = new Data_long[]{ new Data_long{name="Tim", num=10L},
                                                new Data_long{name="John", num=Int32.MaxValue},
                                                new Data_long{name="Bob", num=40L}
            };
                long expected = (long)Int32.MaxValue + 50;

                var actual = source.Sum((e) => e.num);

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

        public class Sum4a
        {
            // Type: long?, source is empty
            public static int Test4a()
            {
                long?[] source = { };
                long? expected = 0;

                var actual = source.Sum();

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

        public class Sum4b
        {
            // Type: long?, source has only one element
            public static int Test4b()
            {
                long?[] source = { (long)-Int32.MaxValue - 20 };
                long? expected = (long)-Int32.MaxValue - 20;

                var actual = source.Sum();

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

        public class Sum4c
        {
            // Type: long?, source has limited number of elements includes null
            public static int Test4c()
            {
                long?[] source = { (long)Int32.MaxValue, 0, -30, null, null, -4, 100 };
                long? expected = (long)Int32.MaxValue + 66;

                var actual = source.Sum();

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

        public class Sum4d
        {
            // Type: long?, source has only null elements
            public static int Test4d()
            {
                long?[] source = { null, null, null, null, null };
                long? expected = 0;

                var actual = source.Sum();

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

        public class Sum4e
        {
            // Type: long?, OverflowException is caused 
            public static int Test4e()
            {
                long?[] source = { Int64.MaxValue, 0, -5, 20, null, null };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum4f
        {
            // Type: long?, Negative OverflowException is caused 
            public static int Test4f()
            {
                long?[] source = { -Int64.MaxValue, 0, -5, -20, null, null };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum4g
        {
            // Type: long?, selector function is called
            public static int Test4g()
            {
                Data_Nlong[] source = new Data_Nlong[]{ new Data_Nlong{name="Tim", num=10L},
                                                new Data_Nlong{name="John", num=Int32.MaxValue},
                                                new Data_Nlong{name="Bob", num=null}
            };
                long expected = (long)Int32.MaxValue + 10;

                var actual = source.Sum((e) => e.num);

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

        public class Sum5a
        {
            // Type: double, source is empty
            public static int Test5a()
            {
                double[] source = { };
                double expected = 0;

                var actual = source.Sum();

                return ((expected == actual) ? 0 : 1);
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

        public class Sum5b
        {
            // Type: double, source has only one element
            public static int Test5b()
            {
                double[] source = { 20.51 };
                double expected = 20.51;

                var actual = source.Sum();

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

        public class Sum5c
        {
            // Type: double, source has limited number of elements
            public static int Test5c()
            {
                double[] source = { 20.45, 0, -10.55, Double.NaN };
                double expected = Double.NaN;

                var actual = source.Sum();

                return (expected.Equals(actual) ? 0 : 1);
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

        public class Sum5d
        {
            // Type: double, Positive Infinity is returned 
            public static int Test5d()
            {
                double[] source = { Double.MaxValue, Double.MaxValue, 0, 0 };

                double actual = source.Sum();

                return (Double.IsPositiveInfinity(actual) ? 0 : 1);
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

        public class Sum5e
        {
            // Type: double, Negative Infinity is returned
            public static int Test5e()
            {
                double[] source = { -Double.MaxValue, -Double.MaxValue };

                double actual = source.Sum();

                return (Double.IsNegativeInfinity(actual) ? 0 : 1);
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

        public class Sum5g
        {
            // Type: double, selector function is called
            public static int Test5g()
            {
                Data_double[] source = new Data_double[]{ new Data_double{name="Tim", num=9.5},
                                                new Data_double{name="John", num=10.5},
                                                new Data_double{name="Bob", num=3.5}
            };
                double expected = 23.5;

                var actual = source.Sum((e) => e.num);

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

        public class Sum6a
        {
            // Type: double?, source is empty
            public static int Test6a()
            {
                double?[] source = { };
                double? expected = 0;

                var actual = source.Sum();

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

        public class Sum6b
        {
            // Type: double?, source has only one element
            public static int Test6b()
            {
                double?[] source = { 20.51 };
                double? expected = 20.51;

                var actual = source.Sum();

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

        public class Sum6c
        {
            // Type: double?, source has limited number of elements
            public static int Test6c()
            {
                double?[] source = { 20.45, 0, -10.55, Double.NaN };
                double? expected = Double.NaN;

                var actual = source.Sum();

                return (expected.Equals(actual) ? 0 : 1);
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

        public class Sum6d
        {
            // Type: double?, source has only null as elements
            public static int Test6d()
            {
                double?[] source = { null, null, null, null };
                double? expected = 0;

                var actual = source.Sum();

                return (expected.Equals(actual) ? 0 : 1);
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

        public class Sum6e
        {
            // Type: double?, Positive Infinity is returned 
            public static int Test6e()
            {
                double?[] source = { Double.MaxValue, Double.MaxValue, 0, 0 };

                var actual = source.Sum();

                return (Double.IsPositiveInfinity((double)actual) ? 0 : 1);
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

        public class Sum6f
        {
            // Type: double?, Negative Infinity is returned
            public static int Test6f()
            {
                double?[] source = { -Double.MaxValue, -Double.MaxValue };

                double? actual = source.Sum();

                return (Double.IsNegativeInfinity((double)actual) ? 0 : 1);
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

        public class Sum6g
        {
            // Type: double?, selector function is called
            public static int Test6g()
            {
                Data_Ndouble[] source = new Data_Ndouble[]{ new Data_Ndouble{name="Tim", num=9.5},
                                                new Data_Ndouble{name="John", num=null},
                                                new Data_Ndouble{name="Bob", num=8.5}
            };
                double? expected = 18.0;

                var actual = source.Sum((e) => e.num);

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

        public class Sum7a
        {
            // Type: decimal, source is empty
            public static int Test7a()
            {
                decimal[] source = { };
                decimal expected = 0;

                var actual = source.Sum();

                return ((expected == actual) ? 0 : 1);
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

        public class Sum7b
        {
            // Type: decimal, source has only one element
            public static int Test7b()
            {
                decimal[] source = { 20.51m };
                decimal expected = 20.51m;

                var actual = source.Sum();

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

        public class Sum7c
        {
            // Type: decimal, source has limited number of elements
            public static int Test7c()
            {
                decimal[] source = { 20.45m, 0, -10.55m, 5.55m };
                decimal expected = 15.45m;

                var actual = source.Sum();

                return (expected.Equals(actual) ? 0 : 1);
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

        public class Sum7d
        {
            // Type: decimal, Positive Overflow exception is thrown
            public static int Test7d()
            {
                decimal[] source = { Decimal.MaxValue, Decimal.MaxValue, 0, 0 };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum7e
        {
            // Type: decimal, Negative Overflow exception is thrown
            public static int Test7e()
            {
                decimal[] source = { -Decimal.MaxValue, -Decimal.MaxValue };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum7f
        {
            // Type: decimal, selector function is called
            public static int Test7f()
            {
                Data_decimal[] source = new Data_decimal[]{ new Data_decimal{name="Tim", num=20.51m},
                                                new Data_decimal{name="John", num=10m},
                                                new Data_decimal{name="Bob", num=2.33m}
            };
                decimal expected = 32.84m;

                var actual = source.Sum((e) => e.num);

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

        public class Sum8a
        {
            // Type: decimal?, source is empty
            public static int Test8a()
            {
                decimal?[] source = { };
                decimal? expected = 0;

                var actual = source.Sum();

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

        public class Sum8b
        {
            // Type: decimal?, source has only one element
            public static int Test8b()
            {
                decimal?[] source = { 20.51m };
                decimal? expected = 20.51m;

                var actual = source.Sum();

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

        public class Sum8c
        {
            // Type: decimal?, source has limited number of elements
            public static int Test8c()
            {
                decimal?[] source = { 20.45m, 0, null, -10.55m, 5.55m, null, null };
                decimal? expected = 15.45m;

                var actual = source.Sum();

                return (expected.Equals(actual) ? 0 : 1);
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

        public class Sum8d
        {
            // Type: decimal?, source has only null elements
            public static int Test8d()
            {
                decimal?[] source = { null, null, null };
                decimal? expected = 0;

                var actual = source.Sum();

                return (expected.Equals(actual) ? 0 : 1);
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

        public class Sum8e
        {
            // Type: decimal?, Positive Overflow exception is thrown
            public static int Test8e()
            {
                decimal?[] source = { Decimal.MaxValue, Decimal.MaxValue, 0, 0 };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum8f
        {
            // Type: decimal?, Negative Overflow exception is thrown
            public static int Test8f()
            {
                decimal?[] source = { -Decimal.MaxValue, -Decimal.MaxValue };

                try
                {
                    var actual = source.Sum();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
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

        public class Sum8g
        {
            // Type: decimal?, selector function is called
            public static int Test8g()
            {
                Data_Ndecimal[] source = new Data_Ndecimal[]{ new Data_Ndecimal{name="Tim", num=20.51m},
                                                new Data_Ndecimal{name="John", num=null},
                                                new Data_Ndecimal{name="Bob", num=2.33m}
            };
                decimal? expected = 22.84m;

                var actual = source.Sum((e) => e.num);

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

        public class Sum9a
        {
            // Type: float, source is empty
            public static int Test9a()
            {
                float[] source = { };
                float expected = 0;

                var actual = source.Sum();

                return ((expected == actual) ? 0 : 1);
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

        public class Sum9b
        {
            // Type: float, source has only one element
            public static int Test9b()
            {
                float[] source = { 20.51f };
                float expected = 20.51f;

                var actual = source.Sum();

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

        public class Sum9c
        {
            // Type: float, source has limited number of elements
            public static int Test9c()
            {
                float[] source = { 20.45f, 0f, -10.55f, float.NaN };
                float expected = float.NaN;

                var actual = source.Sum();

                return (expected.Equals(actual) ? 0 : 1);
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

        public class Sum9d
        {
            // Type: float, Positive Infinity is returned 
            public static int Test9d()
            {
                float[] source = { float.MaxValue, float.MaxValue, 0f, 0f };

                float actual = source.Sum();

                return (float.IsPositiveInfinity(actual) ? 0 : 1);
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

        public class Sum9e
        {
            // Type: float, Negative Infinity is returned
            public static int Test9e()
            {
                float[] source = { -float.MaxValue, -float.MaxValue };

                float actual = source.Sum();

                return (float.IsNegativeInfinity(actual) ? 0 : 1);
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

        public class Sum9f
        {
            // Type: float, selector function is called
            public static int Test9f()
            {
                Data_float[] source = new Data_float[]{ new Data_float{name="Tim", num=9.5f},
                                                new Data_float{name="John", num=10.5f},
                                                new Data_float{name="Bob", num=3.5f}
            };
                float expected = 23.5f;

                var actual = source.Sum((e) => e.num);

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

        public class Sum9g
        {
            // Type: float, source is null
            public static int Test9g()
            {
                float[] source = { -float.MaxValue, -float.MaxValue };

                try
                {
                    source = null;
                    float actual = source.Sum();
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
                return Test9g();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }
    }
}
