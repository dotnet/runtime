// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Array
{
    public static unsafe class NullableNewArrayListTests
    {
        #region Tests

        [Fact]
        public static void CheckNullableBoolArrayListTest()
        {
            bool?[][] array = new bool?[][]
                {
                    new bool?[] {  },
                    new bool?[] { true },
                    new bool?[] { true, false }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    bool? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(bool?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableBoolArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableByteArrayListTest()
        {
            byte?[][] array = new byte?[][]
                {
                    new byte?[] {  },
                    new byte?[] { 0 },
                    new byte?[] { 0, 1, byte.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    byte? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(byte?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableByteArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableCharArrayListTest()
        {
            char?[][] array = new char?[][]
                {
                    new char?[] {  },
                    new char?[] { '\0' },
                    new char?[] { '\0', '\b', 'A', '\uffff' }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    char? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(char?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableCharArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableDecimalArrayListTest()
        {
            decimal?[][] array = new decimal?[][]
                {
                    new decimal?[] {  },
                    new decimal?[] { decimal.Zero },
                    new decimal?[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    decimal? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(decimal?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableDecimalArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableDoubleArrayListTest()
        {
            double?[][] array = new double?[][]
                {
                    new double?[] {  },
                    new double?[] { 0 },
                    new double?[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    double? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(double?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableDoubleArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableEnumArrayListTest()
        {
            E?[][] array = new E?[][]
                {
                    new E?[] {  },
                    new E?[] { (E) 0 },
                    new E?[] { (E) 0, E.A, E.B, (E) int.MaxValue, (E) int.MinValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    E? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(E?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableLongEnumArrayListTest()
        {
            El?[][] array = new El?[][]
                {
                    new El?[] {  },
                    new El?[] { (El) 0 },
                    new El?[] { (El) 0, El.A, El.B, (El) long.MaxValue, (El) long.MinValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    El? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(El?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableLongEnumArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableFloatArrayListTest()
        {
            float?[][] array = new float?[][]
                {
                    new float?[] {  },
                    new float?[] { 0 },
                    new float?[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    float? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(float?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableFloatArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableIntArrayListTest()
        {
            int?[][] array = new int?[][]
                {
                    new int?[] {  },
                    new int?[] { 0 },
                    new int?[] { 0, 1, -1, int.MinValue, int.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    int? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(int?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableLongArrayListTest()
        {
            long?[][] array = new long?[][]
                {
                    new long?[] {  },
                    new long?[] { 0 },
                    new long?[] { 0, 1, -1, long.MinValue, long.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    long? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(long?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableLongArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructArrayListTest()
        {
            S?[][] array = new S?[][]
                {
                    new S?[] {  },
                    new S?[] { default(S) },
                    new S?[] { default(S), new S() }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    S? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(S?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableSByteArrayListTest()
        {
            sbyte?[][] array = new sbyte?[][]
                {
                    new sbyte?[] {  },
                    new sbyte?[] { 0 },
                    new sbyte?[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    sbyte? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(sbyte?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableSByteArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructWithStringArrayListTest()
        {
            Sc?[][] array = new Sc?[][]
                {
                    new Sc?[] {  },
                    new Sc?[] { default(Sc) },
                    new Sc?[] { default(Sc), new Sc(), new Sc(null) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Sc? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Sc?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructWithStringArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructWithStringAndValueArrayListTest()
        {
            Scs?[][] array = new Scs?[][]
                {
                    new Scs?[] {  },
                    new Scs?[] { default(Scs) },
                    new Scs?[] { default(Scs), new Scs(), new Scs(null,new S()) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Scs? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Scs?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructWithStringAndValueArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableShortArrayListTest()
        {
            short?[][] array = new short?[][]
                {
                    new short?[] {  },
                    new short?[] { 0 },
                    new short?[] { 0, 1, -1, short.MinValue, short.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    short? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(short?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableShortArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructWithTwoValuesArrayListTest()
        {
            Sp?[][] array = new Sp?[][]
                {
                    new Sp?[] {  },
                    new Sp?[] { default(Sp) },
                    new Sp?[] { default(Sp), new Sp(), new Sp(5,5.0) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Sp? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Sp?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructWithTwoValuesArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructWithValueArrayListTest()
        {
            Ss?[][] array = new Ss?[][]
                {
                    new Ss?[] {  },
                    new Ss?[] { default(Ss) },
                    new Ss?[] { default(Ss), new Ss(), new Ss(new S()) }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Ss? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Ss?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructWithValueArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableUIntArrayListTest()
        {
            uint?[][] array = new uint?[][]
                {
                    new uint?[] {  },
                    new uint?[] { 0 },
                    new uint?[] { 0, 1, uint.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    uint? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(uint?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableUIntArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableULongArrayListTest()
        {
            ulong?[][] array = new ulong?[][]
                {
                    new ulong?[] {  },
                    new ulong?[] { 0 },
                    new ulong?[] { 0, 1, ulong.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    ulong? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(ulong?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableULongArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckNullableUShortArrayListTest()
        {
            ushort?[][] array = new ushort?[][]
                {
                    new ushort?[] {  },
                    new ushort?[] { 0 },
                    new ushort?[] { 0, 1, ushort.MaxValue }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    ushort? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(ushort?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableUShortArrayList(array[i], exprs[i]);
            }
        }

        [Fact]
        public static void CheckGenericNullableEnumArrayListTest()
        {
            CheckNullableGenericWithStructRestrictionArrayList<E>();
        }

        [Fact]
        public static void CheckGenericNullableStructArrayListTest()
        {
            CheckNullableGenericWithStructRestrictionArrayList<S>();
        }

        [Fact]
        public static void CheckGenericNullableStructWithStringAndValueArrayListTest()
        {
            CheckNullableGenericWithStructRestrictionArrayList<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckNullableGenericWithStructRestrictionArrayList<Ts>() where Ts : struct
        {
            Ts?[][] array = new Ts?[][]
                {
                    new Ts?[] {  },
                    new Ts?[] { default(Ts) },
                    new Ts?[] { default(Ts), new Ts() }
                };
            Expression[][] exprs = new Expression[array.Length][];
            for (int i = 0; i < array.Length; i++)
            {
                exprs[i] = new Expression[array[i].Length];
                for (int j = 0; j < array[i].Length; j++)
                {
                    Ts? val = array[i][j];
                    exprs[i][j] = Expression.Constant(val, typeof(Ts?));
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableGenericWithStructRestrictionArrayList<Ts>(array[i], exprs[i]);
            }
        }

        #endregion

        #region  verifiers

        private static void VerifyNullableBoolArrayList(bool?[] val, Expression[] exprs)
        {
            Expression<Func<bool?[]>> e =
                Expression.Lambda<Func<bool?[]>>(
                    Expression.NewArrayInit(typeof(bool?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?[]> f = e.Compile();
            bool?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableByteArrayList(byte?[] val, Expression[] exprs)
        {
            Expression<Func<byte?[]>> e =
                Expression.Lambda<Func<byte?[]>>(
                    Expression.NewArrayInit(typeof(byte?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?[]> f = e.Compile();
            byte?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableCharArrayList(char?[] val, Expression[] exprs)
        {
            Expression<Func<char?[]>> e =
                Expression.Lambda<Func<char?[]>>(
                    Expression.NewArrayInit(typeof(char?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?[]> f = e.Compile();
            char?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableDecimalArrayList(decimal?[] val, Expression[] exprs)
        {
            Expression<Func<decimal?[]>> e =
                Expression.Lambda<Func<decimal?[]>>(
                    Expression.NewArrayInit(typeof(decimal?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?[]> f = e.Compile();
            decimal?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableDoubleArrayList(double?[] val, Expression[] exprs)
        {
            Expression<Func<double?[]>> e =
                Expression.Lambda<Func<double?[]>>(
                    Expression.NewArrayInit(typeof(double?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?[]> f = e.Compile();
            double?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableEnumArrayList(E?[] val, Expression[] exprs)
        {
            Expression<Func<E?[]>> e =
                Expression.Lambda<Func<E?[]>>(
                    Expression.NewArrayInit(typeof(E?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?[]> f = e.Compile();
            E?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableLongEnumArrayList(El?[] val, Expression[] exprs)
        {
            Expression<Func<El?[]>> e =
                Expression.Lambda<Func<El?[]>>(
                    Expression.NewArrayInit(typeof(El?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?[]> f = e.Compile();
            El?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableFloatArrayList(float?[] val, Expression[] exprs)
        {
            Expression<Func<float?[]>> e =
                Expression.Lambda<Func<float?[]>>(
                    Expression.NewArrayInit(typeof(float?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?[]> f = e.Compile();
            float?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableIntArrayList(int?[] val, Expression[] exprs)
        {
            Expression<Func<int?[]>> e =
                Expression.Lambda<Func<int?[]>>(
                    Expression.NewArrayInit(typeof(int?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?[]> f = e.Compile();
            int?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableLongArrayList(long?[] val, Expression[] exprs)
        {
            Expression<Func<long?[]>> e =
                Expression.Lambda<Func<long?[]>>(
                    Expression.NewArrayInit(typeof(long?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?[]> f = e.Compile();
            long?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableStructArrayList(S?[] val, Expression[] exprs)
        {
            Expression<Func<S?[]>> e =
                Expression.Lambda<Func<S?[]>>(
                    Expression.NewArrayInit(typeof(S?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?[]> f = e.Compile();
            S?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableSByteArrayList(sbyte?[] val, Expression[] exprs)
        {
            Expression<Func<sbyte?[]>> e =
                Expression.Lambda<Func<sbyte?[]>>(
                    Expression.NewArrayInit(typeof(sbyte?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?[]> f = e.Compile();
            sbyte?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableStructWithStringArrayList(Sc?[] val, Expression[] exprs)
        {
            Expression<Func<Sc?[]>> e =
                Expression.Lambda<Func<Sc?[]>>(
                    Expression.NewArrayInit(typeof(Sc?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?[]> f = e.Compile();
            Sc?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableStructWithStringAndValueArrayList(Scs?[] val, Expression[] exprs)
        {
            Expression<Func<Scs?[]>> e =
                Expression.Lambda<Func<Scs?[]>>(
                    Expression.NewArrayInit(typeof(Scs?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?[]> f = e.Compile();
            Scs?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableShortArrayList(short?[] val, Expression[] exprs)
        {
            Expression<Func<short?[]>> e =
                Expression.Lambda<Func<short?[]>>(
                    Expression.NewArrayInit(typeof(short?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?[]> f = e.Compile();
            short?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableStructWithTwoValuesArrayList(Sp?[] val, Expression[] exprs)
        {
            Expression<Func<Sp?[]>> e =
                Expression.Lambda<Func<Sp?[]>>(
                    Expression.NewArrayInit(typeof(Sp?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?[]> f = e.Compile();
            Sp?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableStructWithValueArrayList(Ss?[] val, Expression[] exprs)
        {
            Expression<Func<Ss?[]>> e =
                Expression.Lambda<Func<Ss?[]>>(
                    Expression.NewArrayInit(typeof(Ss?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss?[]> f = e.Compile();
            Ss?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableUIntArrayList(uint?[] val, Expression[] exprs)
        {
            Expression<Func<uint?[]>> e =
                Expression.Lambda<Func<uint?[]>>(
                    Expression.NewArrayInit(typeof(uint?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?[]> f = e.Compile();
            uint?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableULongArrayList(ulong?[] val, Expression[] exprs)
        {
            Expression<Func<ulong?[]>> e =
                Expression.Lambda<Func<ulong?[]>>(
                    Expression.NewArrayInit(typeof(ulong?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?[]> f = e.Compile();
            ulong?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableUShortArrayList(ushort?[] val, Expression[] exprs)
        {
            Expression<Func<ushort?[]>> e =
                Expression.Lambda<Func<ushort?[]>>(
                    Expression.NewArrayInit(typeof(ushort?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?[]> f = e.Compile();
            ushort?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        private static void VerifyNullableGenericWithStructRestrictionArrayList<Ts>(Ts?[] val, Expression[] exprs) where Ts : struct
        {
            Expression<Func<Ts?[]>> e =
                Expression.Lambda<Func<Ts?[]>>(
                    Expression.NewArrayInit(typeof(Ts?), exprs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?[]> f = e.Compile();
            Ts?[] result = f();
            Assert.Equal(val.Length, result.Length);
            for (int i = 0; i < result.Length; i++)
            {
                Assert.Equal(val[i], result[i]);
            }
        }

        #endregion
    }
}
