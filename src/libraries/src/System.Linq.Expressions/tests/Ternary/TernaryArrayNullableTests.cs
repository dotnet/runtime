// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class TernaryArrayNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableBoolArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            bool?[][] array2 = new bool?[][] { null, new bool?[0], new bool?[] { true, false }, new bool?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableBoolArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableByteArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            byte?[][] array2 = new byte?[][] { null, new byte?[0], new byte?[] { 0, 1, byte.MaxValue }, new byte?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableByteArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableCharArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            char?[][] array2 = new char?[][] { null, new char?[0], new char?[] { '\0', '\b', 'A', '\uffff' }, new char?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableCharArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableDecimalArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            decimal?[][] array2 = new decimal?[][] { null, new decimal?[0], new decimal?[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }, new decimal?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableDecimalArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableDoubleArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            double?[][] array2 = new double?[][] { null, new double?[0], new double?[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity }, new double?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableDoubleArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableFloatArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            float?[][] array2 = new float?[][] { null, new float?[0], new float?[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity }, new float?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableFloatArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableIntArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            int?[][] array2 = new int?[][] { null, new int?[0], new int?[] { 0, 1, -1, int.MinValue, int.MaxValue }, new int?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableIntArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableLongArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            long?[][] array2 = new long?[][] { null, new long?[0], new long?[] { 0, 1, -1, long.MinValue, long.MaxValue }, new long?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableLongArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableStructArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            S?[][] array2 = new S?[][] { null, new S?[0], new S?[] { default(S), new S() }, new S?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableStructArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableSByteArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            sbyte?[][] array2 = new sbyte?[][] { null, new sbyte?[0], new sbyte?[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }, new sbyte?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableSByteArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableStructWithStringArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            Sc?[][] array2 = new Sc?[][] { null, new Sc?[0], new Sc?[] { default(Sc), new Sc(), new Sc(null) }, new Sc?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableStructWithStringArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableStructWithStringAndFieldArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            Scs?[][] array2 = new Scs?[][] { null, new Scs?[0], new Scs?[] { default(Scs), new Scs(), new Scs(null, new S()) }, new Scs?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableStructWithStringAndFieldArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableShortArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            short?[][] array2 = new short?[][] { null, new short?[0], new short?[] { 0, 1, -1, short.MinValue, short.MaxValue }, new short?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableShortArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableStructWithTwoValuesArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            Sp?[][] array2 = new Sp?[][] { null, new Sp?[0], new Sp?[] { default(Sp), new Sp(), new Sp(5, 5.0) }, new Sp?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableStructWithTwoValuesArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableStructWithValueArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            Ss?[][] array2 = new Ss?[][] { null, new Ss?[0], new Ss?[] { default(Ss), new Ss(), new Ss(new S()) }, new Ss?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableStructWithValueArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableUIntArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            uint?[][] array2 = new uint?[][] { null, new uint?[0], new uint?[] { 0, 1, uint.MaxValue }, new uint?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableUIntArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableULongArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            ulong?[][] array2 = new ulong?[][] { null, new ulong?[0], new ulong?[] { 0, 1, ulong.MaxValue }, new ulong?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableULongArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableUShortArrayTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            ushort?[][] array2 = new ushort?[][] { null, new ushort?[0], new ushort?[] { 0, 1, ushort.MaxValue }, new ushort?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableUShortArray(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableGenericWithStructRestrictionArrayWithEnumTest(bool useInterpreter)
        {
            CheckTernaryArrayNullableGenericWithStructRestrictionArrayHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableGenericWithStructRestrictionArrayWithStructTest(bool useInterpreter)
        {
            CheckTernaryArrayNullableGenericWithStructRestrictionArrayHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryArrayNullableGenericWithStructRestrictionArrayWithStructWithStringAndFieldTest(bool useInterpreter)
        {
            CheckTernaryArrayNullableGenericWithStructRestrictionArrayHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckTernaryArrayNullableGenericWithStructRestrictionArrayHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            bool[] array1 = new bool[] { false, true };
            Ts?[][] array2 = new Ts?[][] { null, new Ts?[0], new Ts?[] { default(Ts), new Ts() }, new Ts?[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayNullableGenericWithStructRestrictionArray<Ts>(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyArrayNullableBoolArray(bool condition, bool?[] a, bool?[] b, bool useInterpreter)
        {
            Expression<Func<bool?[]>> e =
                Expression.Lambda<Func<bool?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(bool?[])),
                        Expression.Constant(b, typeof(bool?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?[]> f = e.Compile(useInterpreter);

            bool?[] result = default(bool?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            bool?[] expected = default(bool?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableByteArray(bool condition, byte?[] a, byte?[] b, bool useInterpreter)
        {
            Expression<Func<byte?[]>> e =
                Expression.Lambda<Func<byte?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(byte?[])),
                        Expression.Constant(b, typeof(byte?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?[]> f = e.Compile(useInterpreter);

            byte?[] result = default(byte?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            byte?[] expected = default(byte?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableCharArray(bool condition, char?[] a, char?[] b, bool useInterpreter)
        {
            Expression<Func<char?[]>> e =
                Expression.Lambda<Func<char?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(char?[])),
                        Expression.Constant(b, typeof(char?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?[]> f = e.Compile(useInterpreter);

            char?[] result = default(char?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            char?[] expected = default(char?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableDecimalArray(bool condition, decimal?[] a, decimal?[] b, bool useInterpreter)
        {
            Expression<Func<decimal?[]>> e =
                Expression.Lambda<Func<decimal?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(decimal?[])),
                        Expression.Constant(b, typeof(decimal?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?[]> f = e.Compile(useInterpreter);

            decimal?[] result = default(decimal?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            decimal?[] expected = default(decimal?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableDoubleArray(bool condition, double?[] a, double?[] b, bool useInterpreter)
        {
            Expression<Func<double?[]>> e =
                Expression.Lambda<Func<double?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(double?[])),
                        Expression.Constant(b, typeof(double?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?[]> f = e.Compile(useInterpreter);

            double?[] result = default(double?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            double?[] expected = default(double?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableFloatArray(bool condition, float?[] a, float?[] b, bool useInterpreter)
        {
            Expression<Func<float?[]>> e =
                Expression.Lambda<Func<float?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(float?[])),
                        Expression.Constant(b, typeof(float?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?[]> f = e.Compile(useInterpreter);

            float?[] result = default(float?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            float?[] expected = default(float?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableIntArray(bool condition, int?[] a, int?[] b, bool useInterpreter)
        {
            Expression<Func<int?[]>> e =
                Expression.Lambda<Func<int?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(int?[])),
                        Expression.Constant(b, typeof(int?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?[]> f = e.Compile(useInterpreter);

            int?[] result = default(int?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            int?[] expected = default(int?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableLongArray(bool condition, long?[] a, long?[] b, bool useInterpreter)
        {
            Expression<Func<long?[]>> e =
                Expression.Lambda<Func<long?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(long?[])),
                        Expression.Constant(b, typeof(long?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?[]> f = e.Compile(useInterpreter);

            long?[] result = default(long?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            long?[] expected = default(long?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableStructArray(bool condition, S?[] a, S?[] b, bool useInterpreter)
        {
            Expression<Func<S?[]>> e =
                Expression.Lambda<Func<S?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(S?[])),
                        Expression.Constant(b, typeof(S?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?[]> f = e.Compile(useInterpreter);

            S?[] result = default(S?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            S?[] expected = default(S?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableSByteArray(bool condition, sbyte?[] a, sbyte?[] b, bool useInterpreter)
        {
            Expression<Func<sbyte?[]>> e =
                Expression.Lambda<Func<sbyte?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(sbyte?[])),
                        Expression.Constant(b, typeof(sbyte?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?[]> f = e.Compile(useInterpreter);

            sbyte?[] result = default(sbyte?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            sbyte?[] expected = default(sbyte?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableStructWithStringArray(bool condition, Sc?[] a, Sc?[] b, bool useInterpreter)
        {
            Expression<Func<Sc?[]>> e =
                Expression.Lambda<Func<Sc?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Sc?[])),
                        Expression.Constant(b, typeof(Sc?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?[]> f = e.Compile(useInterpreter);

            Sc?[] result = default(Sc?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Sc?[] expected = default(Sc?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableStructWithStringAndFieldArray(bool condition, Scs?[] a, Scs?[] b, bool useInterpreter)
        {
            Expression<Func<Scs?[]>> e =
                Expression.Lambda<Func<Scs?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Scs?[])),
                        Expression.Constant(b, typeof(Scs?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?[]> f = e.Compile(useInterpreter);

            Scs?[] result = default(Scs?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Scs?[] expected = default(Scs?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableShortArray(bool condition, short?[] a, short?[] b, bool useInterpreter)
        {
            Expression<Func<short?[]>> e =
                Expression.Lambda<Func<short?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(short?[])),
                        Expression.Constant(b, typeof(short?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?[]> f = e.Compile(useInterpreter);

            short?[] result = default(short?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            short?[] expected = default(short?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableStructWithTwoValuesArray(bool condition, Sp?[] a, Sp?[] b, bool useInterpreter)
        {
            Expression<Func<Sp?[]>> e =
                Expression.Lambda<Func<Sp?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Sp?[])),
                        Expression.Constant(b, typeof(Sp?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?[]> f = e.Compile(useInterpreter);

            Sp?[] result = default(Sp?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Sp?[] expected = default(Sp?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableStructWithValueArray(bool condition, Ss?[] a, Ss?[] b, bool useInterpreter)
        {
            Expression<Func<Ss?[]>> e =
                Expression.Lambda<Func<Ss?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Ss?[])),
                        Expression.Constant(b, typeof(Ss?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss?[]> f = e.Compile(useInterpreter);

            Ss?[] result = default(Ss?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Ss?[] expected = default(Ss?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableUIntArray(bool condition, uint?[] a, uint?[] b, bool useInterpreter)
        {
            Expression<Func<uint?[]>> e =
                Expression.Lambda<Func<uint?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(uint?[])),
                        Expression.Constant(b, typeof(uint?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?[]> f = e.Compile(useInterpreter);

            uint?[] result = default(uint?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            uint?[] expected = default(uint?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableULongArray(bool condition, ulong?[] a, ulong?[] b, bool useInterpreter)
        {
            Expression<Func<ulong?[]>> e =
                Expression.Lambda<Func<ulong?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(ulong?[])),
                        Expression.Constant(b, typeof(ulong?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?[]> f = e.Compile(useInterpreter);

            ulong?[] result = default(ulong?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ulong?[] expected = default(ulong?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableUShortArray(bool condition, ushort?[] a, ushort?[] b, bool useInterpreter)
        {
            Expression<Func<ushort?[]>> e =
                Expression.Lambda<Func<ushort?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(ushort?[])),
                        Expression.Constant(b, typeof(ushort?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?[]> f = e.Compile(useInterpreter);

            ushort?[] result = default(ushort?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ushort?[] expected = default(ushort?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyArrayNullableGenericWithStructRestrictionArray<Ts>(bool condition, Ts?[] a, Ts?[] b, bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts?[]>> e =
                Expression.Lambda<Func<Ts?[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Ts?[])),
                        Expression.Constant(b, typeof(Ts?[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?[]> f = e.Compile(useInterpreter);

            Ts?[] result = default(Ts?[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Ts?[] expected = default(Ts?[]);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        #endregion
    }
}
