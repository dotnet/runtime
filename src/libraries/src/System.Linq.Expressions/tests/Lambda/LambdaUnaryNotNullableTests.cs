// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LambdaUnaryNotNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaUnaryNotNullableByteTest(bool useInterpreter)
        {
            foreach (byte? value in new byte?[] { null, 0, 1, byte.MaxValue })
            {
                VerifyUnaryNotNullableByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaUnaryNotNullableIntTest(bool useInterpreter)
        {
            foreach (int? value in new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyUnaryNotNullableInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaUnaryNotNullableLongTest(bool useInterpreter)
        {
            foreach (long? value in new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyUnaryNotNullableLong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaUnaryNotNullableSByteTest(bool useInterpreter)
        {
            foreach (sbyte? value in new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifyUnaryNotNullableSByte(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaUnaryNotNullableShortTest(bool useInterpreter)
        {
            foreach (short? value in new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyUnaryNotNullableShort(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaUnaryNotNullableUIntTest(bool useInterpreter)
        {
            foreach (uint? value in new uint?[] { null, 0, 1, uint.MaxValue })
            {
                VerifyUnaryNotNullableUInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaUnaryNotNullableULongTest(bool useInterpreter)
        {
            foreach (ulong? value in new ulong?[] { null, 0, 1, ulong.MaxValue })
            {
                VerifyUnaryNotNullableULong(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLambdaUnaryNotNullableUShortTest(bool useInterpreter)
        {
            foreach (ushort? value in new ushort?[] { null, 0, 1, ushort.MaxValue })
            {
                VerifyUnaryNotNullableUShort(value, useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyUnaryNotNullableByte(byte? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(byte?), "p");

            // parameter hard coded
            Expression<Func<byte?>> e1 =
                Expression.Lambda<Func<byte?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<byte?, byte?>>(
                            Expression.Not(p),
                            new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(byte?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f1 = e1.Compile(useInterpreter);

            // function generator that takes a parameter
            Expression<Func<byte?, Func<byte?>>> e2 =
                Expression.Lambda<Func<byte?, Func<byte?>>>(
                    Expression.Lambda<Func<byte?>>(
                        Expression.Not(p),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<byte?, Func<byte?>> f2 = e2.Compile(useInterpreter);

            // function generator
            Expression<Func<Func<byte?, byte?>>> e3 =
                Expression.Lambda<Func<Func<byte?, byte?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<byte?, byte?>>>(
                            Expression.Lambda<Func<byte?, byte?>>(
                                Expression.Not(p),
                                new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?, byte?> f3 = e3.Compile(useInterpreter)();

            // parameter-taking function generator
            Expression<Func<Func<byte?, byte?>>> e4 =
                Expression.Lambda<Func<Func<byte?, byte?>>>(
                    Expression.Lambda<Func<byte?, byte?>>(
                        Expression.Not(p),
                        new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<byte?, byte?>> f4 = e4.Compile(useInterpreter);

            byte? expected = unchecked((byte?)~value);

            Assert.Equal(expected, f1());
            Assert.Equal(expected, f2(value)());
            Assert.Equal(expected, f3(value));
            Assert.Equal(expected, f4()(value));
        }

        private static void VerifyUnaryNotNullableInt(int? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(int?), "p");

            // parameter hard coded
            Expression<Func<int?>> e1 =
                Expression.Lambda<Func<int?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<int?, int?>>(
                            Expression.Not(p),
                            new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(int?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f1 = e1.Compile(useInterpreter);

            // function generator that takes a parameter
            Expression<Func<int?, Func<int?>>> e2 =
                Expression.Lambda<Func<int?, Func<int?>>>(
                    Expression.Lambda<Func<int?>>(
                        Expression.Not(p),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<int?, Func<int?>> f2 = e2.Compile(useInterpreter);

            // function generator
            Expression<Func<Func<int?, int?>>> e3 =
                Expression.Lambda<Func<Func<int?, int?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<int?, int?>>>(
                            Expression.Lambda<Func<int?, int?>>(
                                Expression.Not(p),
                                new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?, int?> f3 = e3.Compile(useInterpreter)();

            // parameter-taking function generator
            Expression<Func<Func<int?, int?>>> e4 =
                Expression.Lambda<Func<Func<int?, int?>>>(
                    Expression.Lambda<Func<int?, int?>>(
                        Expression.Not(p),
                        new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<int?, int?>> f4 = e4.Compile(useInterpreter);

            int? expected = ~value;

            Assert.Equal(expected, f1());
            Assert.Equal(expected, f2(value)());
            Assert.Equal(expected, f3(value));
            Assert.Equal(expected, f4()(value));
        }

        private static void VerifyUnaryNotNullableLong(long? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(long?), "p");

            // parameter hard coded
            Expression<Func<long?>> e1 =
                Expression.Lambda<Func<long?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<long?, long?>>(
                            Expression.Not(p),
                            new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(long?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f1 = e1.Compile(useInterpreter);

            // function generator that takes a parameter
            Expression<Func<long?, Func<long?>>> e2 =
                Expression.Lambda<Func<long?, Func<long?>>>(
                    Expression.Lambda<Func<long?>>(
                        Expression.Not(p),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<long?, Func<long?>> f2 = e2.Compile(useInterpreter);

            // function generator
            Expression<Func<Func<long?, long?>>> e3 =
                Expression.Lambda<Func<Func<long?, long?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<long?, long?>>>(
                            Expression.Lambda<Func<long?, long?>>(
                                Expression.Not(p),
                                new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?, long?> f3 = e3.Compile(useInterpreter)();

            // parameter-taking function generator
            Expression<Func<Func<long?, long?>>> e4 =
                Expression.Lambda<Func<Func<long?, long?>>>(
                    Expression.Lambda<Func<long?, long?>>(
                        Expression.Not(p),
                        new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<long?, long?>> f4 = e4.Compile(useInterpreter);

            long? expected = ~value;

            Assert.Equal(expected, f1());
            Assert.Equal(expected, f2(value)());
            Assert.Equal(expected, f3(value));
            Assert.Equal(expected, f4()(value));
        }

        private static void VerifyUnaryNotNullableSByte(sbyte? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(sbyte?), "p");

            // parameter hard coded
            Expression<Func<sbyte?>> e1 =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<sbyte?, sbyte?>>(
                            Expression.Not(p),
                            new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(sbyte?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f1 = e1.Compile(useInterpreter);

            // function generator that takes a parameter
            Expression<Func<sbyte?, Func<sbyte?>>> e2 =
                Expression.Lambda<Func<sbyte?, Func<sbyte?>>>(
                    Expression.Lambda<Func<sbyte?>>(
                        Expression.Not(p),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<sbyte?, Func<sbyte?>> f2 = e2.Compile(useInterpreter);

            // function generator
            Expression<Func<Func<sbyte?, sbyte?>>> e3 =
                Expression.Lambda<Func<Func<sbyte?, sbyte?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<sbyte?, sbyte?>>>(
                            Expression.Lambda<Func<sbyte?, sbyte?>>(
                                Expression.Not(p),
                                new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?, sbyte?> f3 = e3.Compile(useInterpreter)();

            // parameter-taking function generator
            Expression<Func<Func<sbyte?, sbyte?>>> e4 =
                Expression.Lambda<Func<Func<sbyte?, sbyte?>>>(
                    Expression.Lambda<Func<sbyte?, sbyte?>>(
                        Expression.Not(p),
                        new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<sbyte?, sbyte?>> f4 = e4.Compile(useInterpreter);

            sbyte? expected = (sbyte?)~value;

            Assert.Equal(expected, f1());
            Assert.Equal(expected, f2(value)());
            Assert.Equal(expected, f3(value));
            Assert.Equal(expected, f4()(value));
        }

        private static void VerifyUnaryNotNullableShort(short? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(short?), "p");

            // parameter hard coded
            Expression<Func<short?>> e1 =
                Expression.Lambda<Func<short?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<short?, short?>>(
                            Expression.Not(p),
                            new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(short?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f1 = e1.Compile(useInterpreter);

            // function generator that takes a parameter
            Expression<Func<short?, Func<short?>>> e2 =
                Expression.Lambda<Func<short?, Func<short?>>>(
                    Expression.Lambda<Func<short?>>(
                        Expression.Not(p),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<short?, Func<short?>> f2 = e2.Compile(useInterpreter);

            // function generator
            Expression<Func<Func<short?, short?>>> e3 =
                Expression.Lambda<Func<Func<short?, short?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<short?, short?>>>(
                            Expression.Lambda<Func<short?, short?>>(
                                Expression.Not(p),
                                new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?, short?> f3 = e3.Compile(useInterpreter)();

            // parameter-taking function generator
            Expression<Func<Func<short?, short?>>> e4 =
                Expression.Lambda<Func<Func<short?, short?>>>(
                    Expression.Lambda<Func<short?, short?>>(
                        Expression.Not(p),
                        new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<short?, short?>> f4 = e4.Compile(useInterpreter);

            short? expected = (short?)~value;

            Assert.Equal(expected, f1());
            Assert.Equal(expected, f2(value)());
            Assert.Equal(expected, f3(value));
            Assert.Equal(expected, f4()(value));
        }

        private static void VerifyUnaryNotNullableUInt(uint? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(uint?), "p");

            // parameter hard coded
            Expression<Func<uint?>> e1 =
                Expression.Lambda<Func<uint?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<uint?, uint?>>(
                            Expression.Not(p),
                            new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(uint?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f1 = e1.Compile(useInterpreter);

            // function generator that takes a parameter
            Expression<Func<uint?, Func<uint?>>> e2 =
                Expression.Lambda<Func<uint?, Func<uint?>>>(
                    Expression.Lambda<Func<uint?>>(
                        Expression.Not(p),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<uint?, Func<uint?>> f2 = e2.Compile(useInterpreter);

            // function generator
            Expression<Func<Func<uint?, uint?>>> e3 =
                Expression.Lambda<Func<Func<uint?, uint?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<uint?, uint?>>>(
                            Expression.Lambda<Func<uint?, uint?>>(
                                Expression.Not(p),
                                new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?, uint?> f3 = e3.Compile(useInterpreter)();

            // parameter-taking function generator
            Expression<Func<Func<uint?, uint?>>> e4 =
                Expression.Lambda<Func<Func<uint?, uint?>>>(
                    Expression.Lambda<Func<uint?, uint?>>(
                        Expression.Not(p),
                        new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<uint?, uint?>> f4 = e4.Compile(useInterpreter);

            uint? expected = ~value;

            Assert.Equal(expected, f1());
            Assert.Equal(expected, f2(value)());
            Assert.Equal(expected, f3(value));
            Assert.Equal(expected, f4()(value));
        }

        private static void VerifyUnaryNotNullableULong(ulong? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(ulong?), "p");

            // parameter hard coded
            Expression<Func<ulong?>> e1 =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ulong?, ulong?>>(
                            Expression.Not(p),
                            new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(ulong?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f1 = e1.Compile(useInterpreter);

            // function generator that takes a parameter
            Expression<Func<ulong?, Func<ulong?>>> e2 =
                Expression.Lambda<Func<ulong?, Func<ulong?>>>(
                    Expression.Lambda<Func<ulong?>>(
                        Expression.Not(p),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<ulong?, Func<ulong?>> f2 = e2.Compile(useInterpreter);

            // function generator
            Expression<Func<Func<ulong?, ulong?>>> e3 =
                Expression.Lambda<Func<Func<ulong?, ulong?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<ulong?, ulong?>>>(
                            Expression.Lambda<Func<ulong?, ulong?>>(
                                Expression.Not(p),
                                new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?, ulong?> f3 = e3.Compile(useInterpreter)();

            // parameter-taking function generator
            Expression<Func<Func<ulong?, ulong?>>> e4 =
                Expression.Lambda<Func<Func<ulong?, ulong?>>>(
                    Expression.Lambda<Func<ulong?, ulong?>>(
                        Expression.Not(p),
                        new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<ulong?, ulong?>> f4 = e4.Compile(useInterpreter);

            ulong? expected = ~value;

            Assert.Equal(expected, f1());
            Assert.Equal(expected, f2(value)());
            Assert.Equal(expected, f3(value));
            Assert.Equal(expected, f4()(value));
        }

        private static void VerifyUnaryNotNullableUShort(ushort? value, bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(ushort?), "p");

            // parameter hard coded
            Expression<Func<ushort?>> e1 =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ushort?, ushort?>>(
                            Expression.Not(p),
                            new ParameterExpression[] { p }),
                        new Expression[] { Expression.Constant(value, typeof(ushort?)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f1 = e1.Compile(useInterpreter);

            // function generator that takes a parameter
            Expression<Func<ushort?, Func<ushort?>>> e2 =
                Expression.Lambda<Func<ushort?, Func<ushort?>>>(
                    Expression.Lambda<Func<ushort?>>(
                        Expression.Not(p),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p });
            Func<ushort?, Func<ushort?>> f2 = e2.Compile(useInterpreter);

            // function generator
            Expression<Func<Func<ushort?, ushort?>>> e3 =
                Expression.Lambda<Func<Func<ushort?, ushort?>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<ushort?, ushort?>>>(
                            Expression.Lambda<Func<ushort?, ushort?>>(
                                Expression.Not(p),
                                new ParameterExpression[] { p }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?, ushort?> f3 = e3.Compile(useInterpreter)();

            // parameter-taking function generator
            Expression<Func<Func<ushort?, ushort?>>> e4 =
                Expression.Lambda<Func<Func<ushort?, ushort?>>>(
                    Expression.Lambda<Func<ushort?, ushort?>>(
                        Expression.Not(p),
                        new ParameterExpression[] { p }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<ushort?, ushort?>> f4 = e4.Compile(useInterpreter);

            ushort? expected = unchecked((ushort?)~value);

            Assert.Equal(expected, f1());
            Assert.Equal(expected, f2(value)());
            Assert.Equal(expected, f3(value));
            Assert.Equal(expected, f4()(value));
        }

        #endregion
    }
}
