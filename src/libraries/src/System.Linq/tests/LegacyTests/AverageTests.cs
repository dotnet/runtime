// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Linq.Tests.LegacyTests
{
    public class AverageTests
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

        public class Average011
        {
            private static int Average001()
            {
                var q = from x in new[] { 9999, 0, 888, -1, 66, -777, 1, 2, -12345 }
                        where x > Int32.MinValue
                        select x;

                var rst1 = q.Average();
                var rst2 = q.Average();

                return ((rst1 == rst2) ? 0 : 1);
            }

            private static int Average002()
            {
                var q = from x in new long?[] { Int32.MaxValue, 0, 255, 127, 128, 1, 33, 99, null, Int32.MinValue }
                        select x;

                var rst1 = q.Average();
                var rst2 = q.Average();

                return ((rst1 == rst2) ? 0 : 1);
            }

            public static int Main()
            {
                int ret = RunTest(Average001) + RunTest(Average002);
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

        public class Average10a
        {
            // Type: float?, source is empty
            public static int Test10a()
            {
                float?[] source = { };
                float? expected = null;

                var actual = source.Average();

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

        public class Average10b
        {
            // Type: float?, source has only one element
            public static int Test10b()
            {
                float?[] source = { float.MinValue };
                float? expected = float.MinValue;

                var actual = source.Average();

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

        public class Average10c
        {
            // Type: float?, source has all equal values and average is zero
            public static int Test10c()
            {
                float?[] source = { 0f, 0f, 0f, 0f, 0f };
                float? expected = 0f;

                var actual = source.Average();

                return ((expected == actual) ? 0 : 1);
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

        public class Average10d
        {
            // Type: float?, source has different values
            public static int Test10d()
            {
                float?[] source = { 5.5f, 0, null, null, null, 15.5f, 40.5f, null, null, -23.5f };
                float? expected = 7.6f;

                var actual = source.Average();

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

        public class Average10e
        {
            // Type: float?, source has 1 non-null value
            public static int Test10e()
            {
                float?[] source = { null, null, null, null, 45f };
                float? expected = 45f;

                var actual = source.Average();

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

        public class Average10f
        {
            // Type: float?, source has all null values
            public static int Test10f()
            {
                float?[] source = { null, null, null, null, null };
                float? expected = null;

                var actual = source.Average();

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

        public class Average10g
        {
            // Type: float?, selector function is called
            public static int Test10g()
            {
                Data_Nfloat[] source = new Data_Nfloat[]{     new Data_Nfloat{name="Tim", num=5.5f},
                                                          new Data_Nfloat{name="John", num=15.5f},
                                                          new Data_Nfloat{name="Bob", num=null}
            };
                float? expected = 10.5f;

                var actual = source.Average((e) => e.num);

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

        public class Average1a
        {
            // Type: int, source is empty
            public static int Test1a()
            {
                int[] source = { };

                try
                {
                    var actual = source.Average();
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

        public class Average1b
        {
            // Type: int, source has only one element
            public static int Test1b()
            {
                int[] source = { 5 };
                double expected = 5;

                var actual = source.Average();

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

        public class Average1c
        {
            // Type: int, source has all equal values and average is zero
            public static int Test1c()
            {
                int[] source = { 0, 0, 0, 0, 0 };
                double expected = 0;

                var actual = source.Average();

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

        public class Average1d
        {
            // Type: int, source has different values
            public static int Test1d()
            {
                int[] source = { 5, -10, 15, 40, 28 };
                double expected = 15.6;

                var actual = source.Average();

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

        public class Average1e
        {
            // Type: int, selector function is called
            public static int Test1e()
            {
                Data_int[] source = new Data_int[]{ new Data_int{name="Tim", num=10},
                                                new Data_int{name="John", num=-10},
                                                new Data_int{name="Bob", num=15}
            };
                double expected = 5;

                var actual = source.Average((e) => e.num);

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

        public class Average2a
        {
            // Type: int?, source is empty
            public static int Test2a()
            {
                int?[] source = { };
                double? expected = null;

                var actual = source.Average();

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

        public class Average2b
        {
            // Type: int?, source has only one element
            public static int Test2b()
            {
                int?[] source = { -5 };
                double? expected = -5;

                var actual = source.Average();

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

        public class Average2c
        {
            // Type: int?, source has all equal values and average is zero
            public static int Test2c()
            {
                int?[] source = { 0, 0, 0, 0, 0 };
                double? expected = 0;

                var actual = source.Average();

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

        public class Average2d
        {
            // Type: int?, source has different values
            public static int Test2d()
            {
                int?[] source = { 5, -10, null, null, null, 15, 40, 28, null, null };
                double? expected = 15.6;

                var actual = source.Average();

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

        public class Average2e
        {
            // Type: int?, source has 1 non-null value
            public static int Test2e()
            {
                int?[] source = { null, null, null, null, 50 };
                double? expected = 50;

                var actual = source.Average();

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

        public class Average2f
        {
            // Type: int?, source has all null values
            public static int Test2f()
            {
                int?[] source = { null, null, null, null, null };
                double? expected = null;

                var actual = source.Average();

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

        public class Average2g
        {
            // Type: int?, selector function is called
            public static int Test2g()
            {
                Data_Nint[] source = new Data_Nint[]{ new Data_Nint{name="Tim", num=10},
                                                  new Data_Nint{name="John", num=null},
                                                  new Data_Nint{name="Bob", num=10}
            };
                double? expected = 10;

                var actual = source.Average((e) => e.num);

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

        public class Average3a
        {
            // Type: long, source is empty
            public static int Test3a()
            {
                long[] source = { };

                try
                {
                    var actual = source.Average();
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

        public class Average3b
        {
            // Type: long, source has only one element
            public static int Test3b()
            {
                long[] source = { Int64.MaxValue };
                double expected = Int64.MaxValue;

                var actual = source.Average();

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

        public class Average3c
        {
            // Type: long, source has all equal values and average is zero
            public static int Test3c()
            {
                long[] source = { 0, 0, 0, 0, 0 };
                double expected = 0;

                var actual = source.Average();

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

        public class Average3d
        {
            // Type: long, source has different values
            public static int Test3d()
            {
                long[] source = { 5, -10, 15, 40, 28 };
                double expected = 15.6;

                var actual = source.Average();

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

        public class Average3e
        {
            // Type: long, selector function is called
            public static int Test3e()
            {
                Data_long[] source = new Data_long[]{ new Data_long{name="Tim", num=40L},
                                                  new Data_long{name="John", num=50L},
                                                  new Data_long{name="Bob", num=60L}
            };
                double expected = 50;

                var actual = source.Average((e) => e.num);

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

        public class Average3f
        {
            [Fact]
            // Type: long, OverflowException is thrown by param sum
            public void Test()
            {
                long[] source = { Int64.MaxValue, Int64.MaxValue };

                Assert.Throws<OverflowException>(() => source.Average());
            }
        }

        public class Average4a
        {
            // Type: long?, source is empty
            public static int Test4a()
            {
                long?[] source = { };
                double? expected = null;

                var actual = source.Average();

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

        public class Average4b
        {
            // Type: long?, source has only one element
            public static int Test4b()
            {
                long?[] source = { Int64.MinValue };
                double? expected = Int64.MinValue;

                var actual = source.Average();

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

        public class Average4c
        {
            // Type: long?, source has all equal values and average is zero
            public static int Test4c()
            {
                long?[] source = { 0, 0, 0, 0, 0 };
                double? expected = 0;

                var actual = source.Average();

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

        public class Average4d
        {
            // Type: long?, source has different values
            public static int Test4d()
            {
                long?[] source = { 5, -10, null, null, null, 15, 40, 28, null, null };
                double? expected = 15.6;

                var actual = source.Average();

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

        public class Average4e
        {
            // Type: long?, source has 1 non-null value
            public static int Test4e()
            {
                long?[] source = { null, null, null, null, 50 };
                double? expected = 50;

                var actual = source.Average();

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

        public class Average4f
        {
            // Type: long?, source has all null values
            public static int Test4f()
            {
                long?[] source = { null, null, null, null, null };
                double? expected = null;

                var actual = source.Average();

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

        public class Average4g
        {
            // Type: long?, selector function is called
            public static int Test4g()
            {
                Data_Nlong[] source = new Data_Nlong[]{ new Data_Nlong{name="Tim", num=40L},
                                                    new Data_Nlong{name="John", num=null},
                                                    new Data_Nlong{name="Bob", num=30L}
            };
                double? expected = 35;

                var actual = source.Average((e) => e.num);

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

        public class Average5a
        {
            // Type: double, source is empty
            public static int Test5a()
            {
                double[] source = { };

                try
                {
                    var actual = source.Average();
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

        public class Average5b
        {
            // Type: double, source has only one element
            public static int Test5b()
            {
                double[] source = { Double.MaxValue };
                double expected = Double.MaxValue;

                var actual = source.Average();

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

        public class Average5c
        {
            // Type: double, source has all equal values and average is zero
            public static int Test5c()
            {
                double[] source = { 0.0, 0.0, 0.0, 0.0, 0.0 };
                double expected = 0;

                var actual = source.Average();

                return ((expected == actual) ? 0 : 1);
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

        public class Average5d
        {
            // Type: double, source has different values
            public static int Test5d()
            {
                double[] source = { 5.5, -10, 15.5, 40.5, 28.5 };
                double expected = 16;

                var actual = source.Average();

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

        public class Average5e
        {
            // Type: double, source has NaN
            public static int Test5e()
            {
                double[] source = { 5.58, Double.NaN, 30, 4.55, 19.38 };
                double expected = Double.NaN;

                var actual = source.Average();

                return ((expected.Equals(actual)) ? 0 : 1);
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

        public class Average5f
        {
            // Type: double, selector function is called
            public static int Test5f()
            {
                Data_double[] source = new Data_double[]{ new Data_double{name="Tim", num=5.5},
                                                      new Data_double{name="John", num=15.5},
                                                      new Data_double{name="Bob", num=3.0}
            };
                double expected = 8.0;

                var actual = source.Average((e) => e.num);

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

        public class Average6a
        {
            // Type: double?, source is empty
            public static int Test6a()
            {
                double?[] source = { };
                double? expected = null;

                var actual = source.Average();

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

        public class Average6b
        {
            // Type: double?, source has only one element
            public static int Test6b()
            {
                double?[] source = { Double.MinValue };
                double? expected = Double.MinValue;

                var actual = source.Average();

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

        public class Average6c
        {
            // Type: double?, source has all equal values and average is zero
            public static int Test6c()
            {
                double?[] source = { 0, 0, 0, 0, 0 };
                double? expected = 0;

                var actual = source.Average();

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

        public class Average6d
        {
            // Type: double?, source has different values
            public static int Test6d()
            {
                double?[] source = { 5.5, 0, null, null, null, 15.5, 40.5, null, null, -23.5 };
                double? expected = 7.6;

                var actual = source.Average();

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

        public class Average6e
        {
            // Type: double?, source has 1 non-null value
            public static int Test6e()
            {
                double?[] source = { null, null, null, null, 45 };
                double? expected = 45;

                var actual = source.Average();

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

        public class Average6f
        {
            // Type: double?, source has all null values
            public static int Test6f()
            {
                double?[] source = { null, null, null, null, null };
                double? expected = null;

                var actual = source.Average();

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

        public class Average6g
        {
            // Type: double?, source has all NaN value
            public static int Test6g()
            {
                double?[] source = { -23.5, 0, Double.NaN, 54.3, 0.56 };
                double? expected = Double.NaN;

                var actual = source.Average();

                return ((expected.Equals(actual)) ? 0 : 1);
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

        public class Average6h
        {
            // Type: double?, selector function is called
            public static int Test6h()
            {
                Data_Ndouble[] source = new Data_Ndouble[]{ new Data_Ndouble{name="Tim", num=5.5},
                                                        new Data_Ndouble{name="John", num=15.5},
                                                        new Data_Ndouble{name="Bob", num=null}
            };
                double? expected = 10.5;

                var actual = source.Average((e) => e.num);

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

        public class Average7a
        {
            // Type: decimal, source is empty
            public static int Test7a()
            {
                decimal[] source = { };

                try
                {
                    var actual = source.Average();
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

        public class Average7b
        {
            // Type: decimal, source has only one element
            public static int Test7b()
            {
                decimal[] source = { Decimal.MaxValue };
                decimal expected = Decimal.MaxValue;

                var actual = source.Average();

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

        public class Average7c
        {
            // Type: decimal, source has all equal values and average is zero
            public static int Test7c()
            {
                decimal[] source = { 0.0m, 0.0m, 0.0m, 0.0m, 0.0m };
                decimal expected = 0m;

                var actual = source.Average();

                return ((expected == actual) ? 0 : 1);
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

        public class Average7d
        {
            // Type: decimal, source has different values
            public static int Test7d()
            {
                decimal[] source = { 5.5m, -10m, 15.5m, 40.5m, 28.5m };
                decimal expected = 16m;

                var actual = source.Average();

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

        public class Average7e
        {
            // Type: decimal, selector function is called
            public static int Test7e()
            {
                Data_decimal[] source = new Data_decimal[]{ new Data_decimal{name="Tim", num=5.5m},
                                                        new Data_decimal{name="John", num=15.5m},
                                                        new Data_decimal{name="Bob", num=3.0m}
            };
                decimal expected = 8.0m;

                var actual = source.Average((e) => e.num);

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

        public class Average8a
        {
            // Type: decimal?, source is empty
            public static int Test8a()
            {
                decimal?[] source = { };
                decimal? expected = null;

                var actual = source.Average();

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

        public class Average8b
        {
            // Type: decimal?, source has only one element
            public static int Test8b()
            {
                decimal?[] source = { Decimal.MinValue };
                decimal? expected = Decimal.MinValue;

                var actual = source.Average();

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

        public class Average8c
        {
            // Type: decimal?, source has all equal values and average is zero
            public static int Test8c()
            {
                decimal?[] source = { 0m, 0m, 0m, 0m, 0m };
                decimal? expected = 0m;

                var actual = source.Average();

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

        public class Average8d
        {
            // Type: decimal?, source has different values
            public static int Test8d()
            {
                decimal?[] source = { 5.5m, 0, null, null, null, 15.5m, 40.5m, null, null, -23.5m };
                decimal? expected = 7.6m;

                var actual = source.Average();

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

        public class Average8e
        {
            // Type: decimal?, source has 1 non-null value
            public static int Test8e()
            {
                decimal?[] source = { null, null, null, null, 45m };
                decimal? expected = 45m;

                var actual = source.Average();

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

        public class Average8f
        {
            // Type: decimal?, source has all null values
            public static int Test8f()
            {
                decimal?[] source = { null, null, null, null, null };
                decimal? expected = null;

                var actual = source.Average();

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

        public class Average8g
        {
            // Type: decimal, selector function is called
            public static int Test8g()
            {
                Data_Ndecimal[] source = new Data_Ndecimal[]{ new Data_Ndecimal{name="Tim", num=5.5m},
                                                          new Data_Ndecimal{name="John", num=15.5m},
                                                          new Data_Ndecimal{name="Bob", num=null}
            };
                decimal? expected = 10.5m;

                var actual = source.Average((e) => e.num);

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

        public class Average8h
        {
            // Type: decimal?, OverflowException is thrown by param sum
            public static int Test8h()
            {
                decimal?[] source = { decimal.MaxValue, decimal.MaxValue };

                try
                {
                    var actual = source.Average();
                    return 1;
                }
                catch (OverflowException)
                {
                    return 0;
                }
            }


            public static int Main()
            {
                return Test8h();
            }

            [Fact]
            public void Test()
            {
                Assert.Equal(0, Main());
            }
        }

        public class Average9a
        {
            // Type: float, source is empty
            public static int Test9a()
            {
                float[] source = { };

                try
                {
                    var actual = source.Average();
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

        public class Average9b
        {
            // Type: float, source has only one element
            public static int Test9b()
            {
                float[] source = { float.MaxValue };
                float expected = float.MaxValue;

                var actual = source.Average();

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

        public class Average9c
        {
            // Type: float, source has all equal values and average is zero
            public static int Test9c()
            {
                float[] source = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
                float expected = 0f;

                var actual = source.Average();

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

        public class Average9d
        {
            // Type: float, source has different values
            public static int Test9d()
            {
                float[] source = { 5.5f, -10f, 15.5f, 40.5f, 28.5f };
                float expected = 16f;

                var actual = source.Average();

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

        public class Average9e
        {
            // Type: float, selector function is called
            public static int Test9e()
            {
                Data_float[] source = new Data_float[]{     new Data_float{name="Tim", num=5.5f},
                                                        new Data_float{name="John", num=15.5f},
                                                        new Data_float{name="Bob", num=3.0f}
            };
                float expected = 8.0f;

                var actual = source.Average((e) => e.num);

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
    }
}