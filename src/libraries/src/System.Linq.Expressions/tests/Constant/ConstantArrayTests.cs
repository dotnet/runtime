// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ConstantArrayTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolArrayConstantTest(bool useInterpreter)
        {
            foreach (bool[] value in new bool[][] { null, new bool[0], new bool[] { true, false }, new bool[100] })
            {
                VerifyBoolArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteArrayConstantTest(bool useInterpreter)
        {
            foreach (byte[] value in new byte[][] { null, new byte[0], new byte[] { 0, 1, byte.MaxValue }, new byte[100] })
            {
                VerifyByteArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomArrayConstantTest(bool useInterpreter)
        {
            foreach (C[] value in new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] })
            {
                VerifyCustomArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharArrayConstantTest(bool useInterpreter)
        {
            foreach (char[] value in new char[][] { null, new char[0], new char[] { '\0', '\b', 'A', '\uffff' }, new char[100] })
            {
                VerifyCharArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ArrayConstantTest(bool useInterpreter)
        {
            foreach (D[] value in new D[][] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10] })
            {
                VerifyCustom2ArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalArrayConstantTest(bool useInterpreter)
        {
            foreach (decimal[] value in new decimal[][] { null, new decimal[0], new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }, new decimal[100] })
            {
                VerifyDecimalArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateArrayConstantTest(bool useInterpreter)
        {
            foreach (Delegate[] value in new Delegate[][] { null, new Delegate[0], new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } }, new Delegate[100] })
            {
                VerifyDelegateArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleArrayConstantTest(bool useInterpreter)
        {
            foreach (double[] value in new double[][] { null, new double[0], new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN }, new double[100] })
            {
                VerifyDoubleArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumArrayConstantTest(bool useInterpreter)
        {
            foreach (E[] value in new E[][] { null, new E[0], new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue }, new E[100] })
            {
                VerifyEnumArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumLongArrayConstantTest(bool useInterpreter)
        {
            foreach (El[] value in new El[][] { null, new El[0], new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue }, new El[100] })
            {
                VerifyEnumLongArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatArrayConstantTest(bool useInterpreter)
        {
            foreach (float[] value in new float[][] { null, new float[0], new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN }, new float[100] })
            {
                VerifyFloatArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncOfObjectConstantTest(bool useInterpreter)
        {
            foreach (Func<object>[] value in new Func<object>[][] { null, new Func<object>[0], new Func<object>[] { null, (Func<object>)delegate () { return null; } }, new Func<object>[100] })
            {
                VerifyFuncOfObjectConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceArrayConstantTest(bool useInterpreter)
        {
            foreach (I[] value in new I[][] { null, new I[0], new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[100] })
            {
                VerifyInterfaceArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustomConstantTest(bool useInterpreter)
        {
            foreach (IEquatable<C>[] value in new IEquatable<C>[][] { null, new IEquatable<C>[0], new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) }, new IEquatable<C>[100] })
            {
                VerifyIEquatableOfCustomConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustom2ConstantTest(bool useInterpreter)
        {
            foreach (IEquatable<D>[] value in new IEquatable<D>[][] { null, new IEquatable<D>[0], new IEquatable<D>[] { null, new D(), new D(0), new D(5) }, new IEquatable<D>[100] })
            {
                VerifyIEquatableOfCustom2Constant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntArrayConstantTest(bool useInterpreter)
        {
            foreach (int[] value in new int[][] { null, new int[0], new int[] { 0, 1, -1, int.MinValue, int.MaxValue }, new int[100] })
            {
                VerifyIntArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongArrayConstantTest(bool useInterpreter)
        {
            foreach (long[] value in new long[][] { null, new long[0], new long[] { 0, 1, -1, long.MinValue, long.MaxValue }, new long[100] })
            {
                VerifyLongArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectArrayConstantTest(bool useInterpreter)
        {
            foreach (object[] value in new object[][] { null, new object[0], new object[] { null, new object(), new C(), new D(3) }, new object[100] })
            {
                VerifyObjectArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructArrayConstantTest(bool useInterpreter)
        {
            foreach (S[] value in new S[][] { null, new S[] { default(S), new S() }, new S[10] })
            {
                VerifyStructArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteArrayConstantTest(bool useInterpreter)
        {
            foreach (sbyte[] value in new sbyte[][] { null, new sbyte[0], new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }, new sbyte[100] })
            {
                VerifySByteArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringArrayConstantTest(bool useInterpreter)
        {
            foreach (Sc[] value in new Sc[][] { null, new Sc[0], new Sc[] { default(Sc), new Sc(), new Sc(null) }, new Sc[100] })
            {
                VerifyStructWithStringArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndFieldArrayConstantTest(bool useInterpreter)
        {
            foreach (Scs[] value in new Scs[][] { null, new Scs[0], new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) }, new Scs[100] })
            {
                VerifyStructWithStringAndFieldArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortArrayConstantTest(bool useInterpreter)
        {
            foreach (short[] value in new short[][] { null, new short[0], new short[] { 0, 1, -1, short.MinValue, short.MaxValue }, new short[100] })
            {
                VerifyShortArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesArrayConstantTest(bool useInterpreter)
        {
            foreach (Sp[] value in new Sp[][] { null, new Sp[0], new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) }, new Sp[100] })
            {
                VerifyStructWithTwoValuesArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueArrayConstantTest(bool useInterpreter)
        {
            foreach (Ss[] value in new Ss[][] { null, new Ss[0], new Ss[] { default(Ss), new Ss(), new Ss(new S()) }, new Ss[100] })
            {
                VerifyStructWithValueArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringArrayConstantTest(bool useInterpreter)
        {
            foreach (string[] value in new string[][] { null, new string[0], new string[] { null, "", "a", "foo" }, new string[100] })
            {
                VerifyStringArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntArrayConstantTest(bool useInterpreter)
        {
            foreach (uint[] value in new uint[][] { null, new uint[0], new uint[] { 0, 1, uint.MaxValue }, new uint[100] })
            {
                VerifyUIntArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongArrayConstantTest(bool useInterpreter)
        {
            foreach (ulong[] value in new ulong[][] { null, new ulong[0], new ulong[] { 0, 1, ulong.MaxValue }, new ulong[100] })
            {
                VerifyULongArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortArrayConstantTest(bool useInterpreter)
        {
            foreach (ushort[] value in new ushort[][] { null, new ushort[0], new ushort[] { 0, 1, ushort.MaxValue }, new ushort[100] })
            {
                VerifyUShortArrayConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionWithEnumArrayConstantTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayConstantHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionWithStructArrayConstantTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayConstantHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionWithStructWithStringAndValueArrayConstantTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionArrayConstantHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithCustomArrayTest(bool useInterpreter)
        {
            CheckGenericArrayHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithEnumArrayTest(bool useInterpreter)
        {
            CheckGenericArrayHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithObjectArrayTest(bool useInterpreter)
        {
            CheckGenericArrayHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructArrayTest(bool useInterpreter)
        {
            CheckGenericArrayHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructWithStringAndValueArrayTest(bool useInterpreter)
        {
            CheckGenericArrayHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionWithCustomTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionArrayHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionWithObjectTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionArrayHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionWithCustomTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionArrayHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionWithObjectTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionArrayHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassRestrictionTest(bool useInterpreter)
        {
            CheckGenericWithSubClassRestrictionHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithSubClassAndNewRestrictionTest(bool useInterpreter)
        {
            CheckGenericWithSubClassAndNewRestrictionHelper<C>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        public static void CheckGenericWithStructRestrictionArrayConstantHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            foreach (Ts[] value in new Ts[][] { null, new Ts[0], new Ts[] { default(Ts), new Ts() }, new Ts[100] })
            {
                VerifyGenericArrayWithStructRestriction<Ts>(value, useInterpreter);
            }
        }

        public static void CheckGenericArrayHelper<T>(bool useInterpreter)
        {
            foreach (T[] value in new T[][] { null, new T[0], new T[] { default(T) }, new T[100] })
            {
                VerifyGenericArray<T>(value, useInterpreter);
            }
        }

        public static void CheckGenericWithClassRestrictionArrayHelper<Tc>(bool useInterpreter) where Tc : class
        {
            foreach (Tc[] value in new Tc[][] { null, new Tc[0], new Tc[] { null, default(Tc) }, new Tc[100] })
            {
                VerifyGenericWithClassRestrictionArray<Tc>(value, useInterpreter);
            }
        }

        public static void CheckGenericWithClassAndNewRestrictionArrayHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            foreach (Tcn[] value in new Tcn[][] { null, new Tcn[0], new Tcn[] { null, default(Tcn), new Tcn() }, new Tcn[100] })
            {
                VerifyGenericWithClassAndNewRestrictionArray<Tcn>(value, useInterpreter);
            }
        }

        public static void CheckGenericWithSubClassRestrictionHelper<TC>(bool useInterpreter) where TC : C
        {
            foreach (TC[] value in new TC[][] { null, new TC[0], new TC[] { null, default(TC), (TC)new C() }, new TC[100] })
            {
                VerifyGenericWithSubClassRestrictionArray<TC>(value, useInterpreter);
            }
        }

        public static void CheckGenericWithSubClassAndNewRestrictionHelper<TCn>(bool useInterpreter) where TCn : C, new()
        {
            foreach (TCn[] value in new TCn[][] { null, new TCn[0], new TCn[] { null, default(TCn), new TCn(), (TCn)new C() }, new TCn[100] })
            {
                VerifyGenericWithSubClassAndNewRestrictionArray<TCn>(value, useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBoolArrayConstant(bool[] value, bool useInterpreter)
        {
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.Constant(value, typeof(bool[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyByteArrayConstant(byte[] value, bool useInterpreter)
        {
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.Constant(value, typeof(byte[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyCustomArrayConstant(C[] value, bool useInterpreter)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Constant(value, typeof(C[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyCharArrayConstant(char[] value, bool useInterpreter)
        {
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.Constant(value, typeof(char[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyCustom2ArrayConstant(D[] value, bool useInterpreter)
        {
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.Constant(value, typeof(D[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyDecimalArrayConstant(decimal[] value, bool useInterpreter)
        {
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.Constant(value, typeof(decimal[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyDelegateArrayConstant(Delegate[] value, bool useInterpreter)
        {
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.Constant(value, typeof(Delegate[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyDoubleArrayConstant(double[] value, bool useInterpreter)
        {
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.Constant(value, typeof(double[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyEnumArrayConstant(E[] value, bool useInterpreter)
        {
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.Constant(value, typeof(E[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyEnumLongArrayConstant(El[] value, bool useInterpreter)
        {
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.Constant(value, typeof(El[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyFloatArrayConstant(float[] value, bool useInterpreter)
        {
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.Constant(value, typeof(float[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyFuncOfObjectConstant(Func<object>[] value, bool useInterpreter)
        {
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.Constant(value, typeof(Func<object>[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyInterfaceArrayConstant(I[] value, bool useInterpreter)
        {
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.Constant(value, typeof(I[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyIEquatableOfCustomConstant(IEquatable<C>[] value, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.Constant(value, typeof(IEquatable<C>[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyIEquatableOfCustom2Constant(IEquatable<D>[] value, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.Constant(value, typeof(IEquatable<D>[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyIntArrayConstant(int[] value, bool useInterpreter)
        {
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.Constant(value, typeof(int[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyLongArrayConstant(long[] value, bool useInterpreter)
        {
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.Constant(value, typeof(long[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyObjectArrayConstant(object[] value, bool useInterpreter)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Constant(value, typeof(object[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStructArrayConstant(S[] value, bool useInterpreter)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.Constant(value, typeof(S[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifySByteArrayConstant(sbyte[] value, bool useInterpreter)
        {
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.Constant(value, typeof(sbyte[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithStringArrayConstant(Sc[] value, bool useInterpreter)
        {
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.Constant(value, typeof(Sc[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithStringAndFieldArrayConstant(Scs[] value, bool useInterpreter)
        {
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.Constant(value, typeof(Scs[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyShortArrayConstant(short[] value, bool useInterpreter)
        {
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.Constant(value, typeof(short[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithTwoValuesArrayConstant(Sp[] value, bool useInterpreter)
        {
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.Constant(value, typeof(Sp[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithValueArrayConstant(Ss[] value, bool useInterpreter)
        {
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.Constant(value, typeof(Ss[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStringArrayConstant(string[] value, bool useInterpreter)
        {
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.Constant(value, typeof(string[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyUIntArrayConstant(uint[] value, bool useInterpreter)
        {
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.Constant(value, typeof(uint[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyULongArrayConstant(ulong[] value, bool useInterpreter)
        {
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.Constant(value, typeof(ulong[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyUShortArrayConstant(ushort[] value, bool useInterpreter)
        {
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.Constant(value, typeof(ushort[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGenericArrayWithStructRestriction<Ts>(Ts[] value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.Constant(value, typeof(Ts[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGenericArray<T>(T[] value, bool useInterpreter)
        {
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.Constant(value, typeof(T[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithClassRestrictionArray<Tc>(Tc[] value, bool useInterpreter) where Tc : class
        {
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.Constant(value, typeof(Tc[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithClassAndNewRestrictionArray<Tcn>(Tcn[] value, bool useInterpreter) where Tcn : class, new()
        {
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.Constant(value, typeof(Tcn[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithSubClassRestrictionArray<TC>(TC[] value, bool useInterpreter) where TC : C
        {
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.Constant(value, typeof(TC[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionArray<TCn>(TCn[] value, bool useInterpreter) where TCn : C, new()
        {
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.Constant(value, typeof(TCn[])),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        #endregion
    }
}
