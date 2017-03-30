// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LambdaIdentityNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableBoolTest(bool useInterpreter)
        {
            foreach (bool? value in new bool?[] { null, true, false })
            {
                VerifyIdentityNullableBool(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableByteTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyIdentityNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableCharTest(bool useInterpreter)
        {
            foreach (char? value in new char?[] { null, '\0', '\b', 'A', '\uffff' })
            {
                VerifyIdentityNullableChar(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableDecimalTest(bool useInterpreter)
        {
            foreach (decimal? value in new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyIdentityNullableDecimal(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableDoubleTest(bool useInterpreter)
        {
            foreach (double? value in new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyIdentityNullableDouble(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableEnumTest(bool useInterpreter)
        {
            foreach (E? value in new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyIdentityNullableEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableEnumLongTest(bool useInterpreter)
        {
            foreach (El? value in new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyIdentityNullableEnumLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableFloatTest(bool useInterpreter)
        {
            foreach (float? value in new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyIdentityNullableFloat(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableIntTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIdentityNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableLongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyIdentityNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableStructTest(bool useInterpreter)
        {
            foreach (S? value in new S?[] { null, default(S), new S() })
            {
                VerifyIdentityNullableStruct(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableSByteTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyIdentityNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableStructWithStringTest(bool useInterpreter)
        {
            foreach (Sc? value in new Sc?[] { null, default(Sc), new Sc(), new Sc(null) })
            {
                VerifyIdentityNullableStructWithString(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableStructWithStringAndFieldTest(bool useInterpreter)
        {
            foreach (Scs? value in new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) })
            {
                VerifyIdentityNullableStructWithStringAndField(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableShortTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyIdentityNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableStructWithTwoValuesTest(bool useInterpreter)
        {
            foreach (Sp? value in new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) })
            {
                VerifyIdentityNullableStructWithTwoValues(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableStructWithValueTest(bool useInterpreter)
        {
            foreach (Ss? value in new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) })
            {
                VerifyIdentityNullableStructWithValue(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableUIntTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyIdentityNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableULongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyIdentityNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableUShortTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyIdentityNullableUShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableGenericWithStructRestrictionWithEnumTest(bool useInterpreter)
        {
            CheckLambdaIdentityNullableGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableGenericWithStructRestrictionWithStructTest(bool useInterpreter)
        {
            CheckLambdaIdentityNullableGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaIdentityNullableGenericWithStructRestrictionWithStructWithStringAndFieldTest(bool useInterpreter)
        {
            CheckLambdaIdentityNullableGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckLambdaIdentityNullableGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            foreach (Ts? value in new Ts?[] { default(Ts), new Ts() })
            {
                VerifyIdentityNullableGenericWithStructRestriction<Ts>(value, useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyIdentityNullableBool(bool? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(bool?), "p");

            // parameter hard coded
            Expression<Func<bool?>> e1 =
                Expression.Lambda<Func<bool?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<bool?, bool?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(bool?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<bool?, Func<bool?>>> e2 =
                Expression.Lambda<Func<bool?, Func<bool?>>>(
                    Expression.Lambda<Func<bool?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<bool?, Func<bool?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<bool?, bool?>>> e3 =
                Expression.Lambda<Func<Func<bool?, bool?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<bool?, bool?>>>(
                            Expression.Lambda<Func<bool?, bool?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?, bool?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<bool?, bool?>>> e4 =
                Expression.Lambda<Func<Func<bool?, bool?>>>(
                    Expression.Lambda<Func<bool?, bool?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<bool?, bool?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableByte(byte? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(byte?), "p");

            // parameter hard coded
            Expression<Func<byte?>> e1 =
                Expression.Lambda<Func<byte?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<byte?, byte?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(byte?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<byte?, Func<byte?>>> e2 =
                Expression.Lambda<Func<byte?, Func<byte?>>>(
                    Expression.Lambda<Func<byte?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<byte?, Func<byte?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<byte?, byte?>>> e3 =
                Expression.Lambda<Func<Func<byte?, byte?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<byte?, byte?>>>(
                            Expression.Lambda<Func<byte?, byte?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?, byte?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<byte?, byte?>>> e4 =
                Expression.Lambda<Func<Func<byte?, byte?>>>(
                    Expression.Lambda<Func<byte?, byte?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<byte?, byte?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableChar(char? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(char?), "p");

            // parameter hard coded
            Expression<Func<char?>> e1 =
                Expression.Lambda<Func<char?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<char?, char?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(char?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<char?, Func<char?>>> e2 =
                Expression.Lambda<Func<char?, Func<char?>>>(
                    Expression.Lambda<Func<char?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<char?, Func<char?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<char?, char?>>> e3 =
                Expression.Lambda<Func<Func<char?, char?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<char?, char?>>>(
                            Expression.Lambda<Func<char?, char?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?, char?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<char?, char?>>> e4 =
                Expression.Lambda<Func<Func<char?, char?>>>(
                    Expression.Lambda<Func<char?, char?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<char?, char?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableDecimal(decimal? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(decimal?), "p");

            // parameter hard coded
            Expression<Func<decimal?>> e1 =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<decimal?, decimal?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(decimal?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<decimal?, Func<decimal?>>> e2 =
                Expression.Lambda<Func<decimal?, Func<decimal?>>>(
                    Expression.Lambda<Func<decimal?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<decimal?, Func<decimal?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<decimal?, decimal?>>> e3 =
                Expression.Lambda<Func<Func<decimal?, decimal?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<decimal?, decimal?>>>(
                            Expression.Lambda<Func<decimal?, decimal?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?, decimal?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<decimal?, decimal?>>> e4 =
                Expression.Lambda<Func<Func<decimal?, decimal?>>>(
                    Expression.Lambda<Func<decimal?, decimal?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<decimal?, decimal?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableDouble(double? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(double?), "p");

            // parameter hard coded
            Expression<Func<double?>> e1 =
                Expression.Lambda<Func<double?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<double?, double?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(double?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<double?, Func<double?>>> e2 =
                Expression.Lambda<Func<double?, Func<double?>>>(
                    Expression.Lambda<Func<double?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<double?, Func<double?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<double?, double?>>> e3 =
                Expression.Lambda<Func<Func<double?, double?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<double?, double?>>>(
                            Expression.Lambda<Func<double?, double?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?, double?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<double?, double?>>> e4 =
                Expression.Lambda<Func<Func<double?, double?>>>(
                    Expression.Lambda<Func<double?, double?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<double?, double?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableEnum(E? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(E?), "p");

            // parameter hard coded
            Expression<Func<E?>> e1 =
                Expression.Lambda<Func<E?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<E?, E?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(E?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<E?, Func<E?>>> e2 =
                Expression.Lambda<Func<E?, Func<E?>>>(
                    Expression.Lambda<Func<E?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<E?, Func<E?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<E?, E?>>> e3 =
                Expression.Lambda<Func<Func<E?, E?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<E?, E?>>>(
                            Expression.Lambda<Func<E?, E?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?, E?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<E?, E?>>> e4 =
                Expression.Lambda<Func<Func<E?, E?>>>(
                    Expression.Lambda<Func<E?, E?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<E?, E?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableEnumLong(El? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(El?), "p");

            // parameter hard coded
            Expression<Func<El?>> e1 =
                Expression.Lambda<Func<El?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<El?, El?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(El?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<El?, Func<El?>>> e2 =
                Expression.Lambda<Func<El?, Func<El?>>>(
                    Expression.Lambda<Func<El?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<El?, Func<El?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<El?, El?>>> e3 =
                Expression.Lambda<Func<Func<El?, El?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<El?, El?>>>(
                            Expression.Lambda<Func<El?, El?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?, El?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<El?, El?>>> e4 =
                Expression.Lambda<Func<Func<El?, El?>>>(
                    Expression.Lambda<Func<El?, El?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<El?, El?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableFloat(float? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(float?), "p");

            // parameter hard coded
            Expression<Func<float?>> e1 =
                Expression.Lambda<Func<float?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<float?, float?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(float?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<float?, Func<float?>>> e2 =
                Expression.Lambda<Func<float?, Func<float?>>>(
                    Expression.Lambda<Func<float?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<float?, Func<float?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<float?, float?>>> e3 =
                Expression.Lambda<Func<Func<float?, float?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<float?, float?>>>(
                            Expression.Lambda<Func<float?, float?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?, float?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<float?, float?>>> e4 =
                Expression.Lambda<Func<Func<float?, float?>>>(
                    Expression.Lambda<Func<float?, float?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<float?, float?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableInt(int? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(int?), "p");

            // parameter hard coded
            Expression<Func<int?>> e1 =
                Expression.Lambda<Func<int?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<int?, int?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(int?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<int?, Func<int?>>> e2 =
                Expression.Lambda<Func<int?, Func<int?>>>(
                    Expression.Lambda<Func<int?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<int?, Func<int?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<int?, int?>>> e3 =
                Expression.Lambda<Func<Func<int?, int?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<int?, int?>>>(
                            Expression.Lambda<Func<int?, int?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?, int?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<int?, int?>>> e4 =
                Expression.Lambda<Func<Func<int?, int?>>>(
                    Expression.Lambda<Func<int?, int?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<int?, int?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableLong(long? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(long?), "p");

            // parameter hard coded
            Expression<Func<long?>> e1 =
                Expression.Lambda<Func<long?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<long?, long?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(long?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<long?, Func<long?>>> e2 =
                Expression.Lambda<Func<long?, Func<long?>>>(
                    Expression.Lambda<Func<long?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<long?, Func<long?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<long?, long?>>> e3 =
                Expression.Lambda<Func<Func<long?, long?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<long?, long?>>>(
                            Expression.Lambda<Func<long?, long?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?, long?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<long?, long?>>> e4 =
                Expression.Lambda<Func<Func<long?, long?>>>(
                    Expression.Lambda<Func<long?, long?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<long?, long?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableStruct(S? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(S?), "p");

            // parameter hard coded
            Expression<Func<S?>> e1 =
                Expression.Lambda<Func<S?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<S?, S?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(S?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<S?, Func<S?>>> e2 =
                Expression.Lambda<Func<S?, Func<S?>>>(
                    Expression.Lambda<Func<S?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<S?, Func<S?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<S?, S?>>> e3 =
                Expression.Lambda<Func<Func<S?, S?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<S?, S?>>>(
                            Expression.Lambda<Func<S?, S?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?, S?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<S?, S?>>> e4 =
                Expression.Lambda<Func<Func<S?, S?>>>(
                    Expression.Lambda<Func<S?, S?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<S?, S?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableSByte(sbyte? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(sbyte?), "p");

            // parameter hard coded
            Expression<Func<sbyte?>> e1 =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<sbyte?, sbyte?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(sbyte?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<sbyte?, Func<sbyte?>>> e2 =
                Expression.Lambda<Func<sbyte?, Func<sbyte?>>>(
                    Expression.Lambda<Func<sbyte?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<sbyte?, Func<sbyte?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<sbyte?, sbyte?>>> e3 =
                Expression.Lambda<Func<Func<sbyte?, sbyte?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<sbyte?, sbyte?>>>(
                            Expression.Lambda<Func<sbyte?, sbyte?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?, sbyte?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<sbyte?, sbyte?>>> e4 =
                Expression.Lambda<Func<Func<sbyte?, sbyte?>>>(
                    Expression.Lambda<Func<sbyte?, sbyte?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<sbyte?, sbyte?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableStructWithString(Sc? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Sc?), "p");

            // parameter hard coded
            Expression<Func<Sc?>> e1 =
                Expression.Lambda<Func<Sc?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Sc?, Sc?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(Sc?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<Sc?, Func<Sc?>>> e2 =
                Expression.Lambda<Func<Sc?, Func<Sc?>>>(
                    Expression.Lambda<Func<Sc?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<Sc?, Func<Sc?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<Sc?, Sc?>>> e3 =
                Expression.Lambda<Func<Func<Sc?, Sc?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<Sc?, Sc?>>>(
                            Expression.Lambda<Func<Sc?, Sc?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?, Sc?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<Sc?, Sc?>>> e4 =
                Expression.Lambda<Func<Func<Sc?, Sc?>>>(
                    Expression.Lambda<Func<Sc?, Sc?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<Sc?, Sc?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableStructWithStringAndField(Scs? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Scs?), "p");

            // parameter hard coded
            Expression<Func<Scs?>> e1 =
                Expression.Lambda<Func<Scs?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Scs?, Scs?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(Scs?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<Scs?, Func<Scs?>>> e2 =
                Expression.Lambda<Func<Scs?, Func<Scs?>>>(
                    Expression.Lambda<Func<Scs?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<Scs?, Func<Scs?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<Scs?, Scs?>>> e3 =
                Expression.Lambda<Func<Func<Scs?, Scs?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<Scs?, Scs?>>>(
                            Expression.Lambda<Func<Scs?, Scs?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?, Scs?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<Scs?, Scs?>>> e4 =
                Expression.Lambda<Func<Func<Scs?, Scs?>>>(
                    Expression.Lambda<Func<Scs?, Scs?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<Scs?, Scs?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableShort(short? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(short?), "p");

            // parameter hard coded
            Expression<Func<short?>> e1 =
                Expression.Lambda<Func<short?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<short?, short?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(short?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<short?, Func<short?>>> e2 =
                Expression.Lambda<Func<short?, Func<short?>>>(
                    Expression.Lambda<Func<short?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<short?, Func<short?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<short?, short?>>> e3 =
                Expression.Lambda<Func<Func<short?, short?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<short?, short?>>>(
                            Expression.Lambda<Func<short?, short?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?, short?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<short?, short?>>> e4 =
                Expression.Lambda<Func<Func<short?, short?>>>(
                    Expression.Lambda<Func<short?, short?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<short?, short?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableStructWithTwoValues(Sp? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Sp?), "p");

            // parameter hard coded
            Expression<Func<Sp?>> e1 =
                Expression.Lambda<Func<Sp?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Sp?, Sp?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(Sp?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<Sp?, Func<Sp?>>> e2 =
                Expression.Lambda<Func<Sp?, Func<Sp?>>>(
                    Expression.Lambda<Func<Sp?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<Sp?, Func<Sp?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<Sp?, Sp?>>> e3 =
                Expression.Lambda<Func<Func<Sp?, Sp?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<Sp?, Sp?>>>(
                            Expression.Lambda<Func<Sp?, Sp?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?, Sp?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<Sp?, Sp?>>> e4 =
                Expression.Lambda<Func<Func<Sp?, Sp?>>>(
                    Expression.Lambda<Func<Sp?, Sp?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<Sp?, Sp?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableStructWithValue(Ss? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Ss?), "p");

            // parameter hard coded
            Expression<Func<Ss?>> e1 =
                Expression.Lambda<Func<Ss?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Ss?, Ss?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(Ss?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<Ss?, Func<Ss?>>> e2 =
                Expression.Lambda<Func<Ss?, Func<Ss?>>>(
                    Expression.Lambda<Func<Ss?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<Ss?, Func<Ss?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<Ss?, Ss?>>> e3 =
                Expression.Lambda<Func<Func<Ss?, Ss?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<Ss?, Ss?>>>(
                            Expression.Lambda<Func<Ss?, Ss?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss?, Ss?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<Ss?, Ss?>>> e4 =
                Expression.Lambda<Func<Func<Ss?, Ss?>>>(
                    Expression.Lambda<Func<Ss?, Ss?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<Ss?, Ss?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableUInt(uint? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(uint?), "p");

            // parameter hard coded
            Expression<Func<uint?>> e1 =
                Expression.Lambda<Func<uint?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<uint?, uint?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(uint?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<uint?, Func<uint?>>> e2 =
                Expression.Lambda<Func<uint?, Func<uint?>>>(
                    Expression.Lambda<Func<uint?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<uint?, Func<uint?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<uint?, uint?>>> e3 =
                Expression.Lambda<Func<Func<uint?, uint?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<uint?, uint?>>>(
                            Expression.Lambda<Func<uint?, uint?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?, uint?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<uint?, uint?>>> e4 =
                Expression.Lambda<Func<Func<uint?, uint?>>>(
                    Expression.Lambda<Func<uint?, uint?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<uint?, uint?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableULong(ulong? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(ulong?), "p");

            // parameter hard coded
            Expression<Func<ulong?>> e1 =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ulong?, ulong?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(ulong?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<ulong?, Func<ulong?>>> e2 =
                Expression.Lambda<Func<ulong?, Func<ulong?>>>(
                    Expression.Lambda<Func<ulong?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<ulong?, Func<ulong?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<ulong?, ulong?>>> e3 =
                Expression.Lambda<Func<Func<ulong?, ulong?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<ulong?, ulong?>>>(
                            Expression.Lambda<Func<ulong?, ulong?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?, ulong?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<ulong?, ulong?>>> e4 =
                Expression.Lambda<Func<Func<ulong?, ulong?>>>(
                    Expression.Lambda<Func<ulong?, ulong?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<ulong?, ulong?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableUShort(ushort? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(ushort?), "p");

            // parameter hard coded
            Expression<Func<ushort?>> e1 =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ushort?, ushort?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(ushort?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<ushort?, Func<ushort?>>> e2 =
                Expression.Lambda<Func<ushort?, Func<ushort?>>>(
                    Expression.Lambda<Func<ushort?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<ushort?, Func<ushort?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<ushort?, ushort?>>> e3 =
                Expression.Lambda<Func<Func<ushort?, ushort?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<ushort?, ushort?>>>(
                            Expression.Lambda<Func<ushort?, ushort?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?, ushort?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<ushort?, ushort?>>> e4 =
                Expression.Lambda<Func<Func<ushort?, ushort?>>>(
                    Expression.Lambda<Func<ushort?, ushort?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<ushort?, ushort?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        private static void VerifyIdentityNullableGenericWithStructRestriction<Ts>(Ts? value, bool useInterpreter) where Ts : struct
        {
            ParameterExpression p = Expression.Parameter(typeof(Ts?), "p");

            // parameter hard coded
            Expression<Func<Ts?>> e1 =
                Expression.Lambda<Func<Ts?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Ts?, Ts?>>(p, new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(Ts?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f1 = e1.Compile(useInterpreter);

            // parameter passed into function generator
            Expression<Func<Ts?, Func<Ts?>>> e2 =
                Expression.Lambda<Func<Ts?, Func<Ts?>>>(
                    Expression.Lambda<Func<Ts?>>(p, Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<Ts?, Func<Ts?>> f2 = e2.Compile(useInterpreter);

            // parameter passed into invoked generated function
            Expression<Func<Func<Ts?, Ts?>>> e3 =
                Expression.Lambda<Func<Func<Ts?, Ts?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<Ts?, Ts?>>>(
                            Expression.Lambda<Func<Ts?, Ts?>>(p, new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?, Ts?> f3 = e3.Compile(useInterpreter)();

            // parameter passed into generated function
            Expression<Func<Func<Ts?, Ts?>>> e4 =
                Expression.Lambda<Func<Func<Ts?, Ts?>>>(
                    Expression.Lambda<Func<Ts?, Ts?>>(p, new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<Ts?, Ts?>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(value, f1());
            Assert.Equal(value, f2(value)());
            Assert.Equal(value, f3(value));
            Assert.Equal(value, f4()(value));
        }

        #endregion
    }
}
