// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Ternary
{
    public static unsafe class TernaryArrayTests
    {
        #region Test methods

        [Fact]
        public static void CheckTernaryArrayBoolArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            bool[][] array2 = new bool[][] { null, new bool[0], new bool[] { true, false }, new bool[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayBoolArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayByteArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            byte[][] array2 = new byte[][] { null, new byte[0], new byte[] { 0, 1, byte.MaxValue }, new byte[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayByteArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayCustomArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            C[][] array2 = new C[][] { null, new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[10] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayCustomArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayCharArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            char[][] array2 = new char[][] { null, new char[0], new char[] { '\0', '\b', 'A', '\uffff' }, new char[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayCharArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayCustom2ArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            D[][] array2 = new D[][] { null, new D[] { null, new D(), new D(0), new D(5) }, new D[10] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayCustom2Array(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayDecimalArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            decimal[][] array2 = new decimal[][] { null, new decimal[0], new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }, new decimal[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayDecimalArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayDelegateArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            Delegate[][] array2 = new Delegate[][] { null, new Delegate[0], new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } }, new Delegate[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayDelegateArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayDoubleArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            double[][] array2 = new double[][] { null, new double[0], new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN }, new double[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayDoubleArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayEnumArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            E[][] array2 = new E[][] { null, new E[0], new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue }, new E[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayEnumArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayEnumLongArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            El[][] array2 = new El[][] { null, new El[0], new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue }, new El[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayEnumLongArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayFloatArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            float[][] array2 = new float[][] { null, new float[0], new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN }, new float[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayFloatArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayFuncOfObjectTest()
        {
            bool[] array1 = new bool[] { false, true };
            Func<object>[][] array2 = new Func<object>[][] { null, new Func<object>[0], new Func<object>[] { null, (Func<object>)delegate () { return null; } }, new Func<object>[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayFuncOfObject(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayInterfaceArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            I[][] array2 = new I[][] { null, new I[0], new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayInterfaceArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayIEquatableOfCustomTest()
        {
            bool[] array1 = new bool[] { false, true };
            IEquatable<C>[][] array2 = new IEquatable<C>[][] { null, new IEquatable<C>[0], new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) }, new IEquatable<C>[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayIEquatableOfCustom(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayIEquatableOfCustom2Test()
        {
            bool[] array1 = new bool[] { false, true };
            IEquatable<D>[][] array2 = new IEquatable<D>[][] { null, new IEquatable<D>[0], new IEquatable<D>[] { null, new D(), new D(0), new D(5) }, new IEquatable<D>[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayIEquatableOfCustom2(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayIntArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            int[][] array2 = new int[][] { null, new int[0], new int[] { 0, 1, -1, int.MinValue, int.MaxValue }, new int[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayIntArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayLongArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            long[][] array2 = new long[][] { null, new long[0], new long[] { 0, 1, -1, long.MinValue, long.MaxValue }, new long[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayLongArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayObjectArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            object[][] array2 = new object[][] { null, new object[0], new object[] { null, new object(), new C(), new D(3) }, new object[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayObjectArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayStructArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            S[][] array2 = new S[][] { null, new S[] { default(S), new S() }, new S[10] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayStructArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArraySByteArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            sbyte[][] array2 = new sbyte[][] { null, new sbyte[0], new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }, new sbyte[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArraySByteArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayStructWithStringArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            Sc[][] array2 = new Sc[][] { null, new Sc[0], new Sc[] { default(Sc), new Sc(), new Sc(null) }, new Sc[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayStructWithStringArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayStructWithStringAndFieldArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            Scs[][] array2 = new Scs[][] { null, new Scs[0], new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) }, new Scs[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayStructWithStringAndFieldArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayShortArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            short[][] array2 = new short[][] { null, new short[0], new short[] { 0, 1, -1, short.MinValue, short.MaxValue }, new short[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayShortArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayStructWithTwoValuesArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            Sp[][] array2 = new Sp[][] { null, new Sp[0], new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) }, new Sp[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayStructWithTwoValuesArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayStructWithValueArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            Ss[][] array2 = new Ss[][] { null, new Ss[0], new Ss[] { default(Ss), new Ss(), new Ss(new S()) }, new Ss[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayStructWithValueArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayStringArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            string[][] array2 = new string[][] { null, new string[0], new string[] { null, "", "a", "foo" }, new string[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayStringArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayUIntArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            uint[][] array2 = new uint[][] { null, new uint[0], new uint[] { 0, 1, uint.MaxValue }, new uint[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayUIntArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayULongArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            ulong[][] array2 = new ulong[][] { null, new ulong[0], new ulong[] { 0, 1, ulong.MaxValue }, new ulong[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayULongArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayUShortArrayTest()
        {
            bool[] array1 = new bool[] { false, true };
            ushort[][] array2 = new ushort[][] { null, new ushort[0], new ushort[] { 0, 1, ushort.MaxValue }, new ushort[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayUShortArray(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryArrayGenericArrayWithCustomTest()
        {
            CheckTernaryArrayGenericArrayHelper<C>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericArrayWithEnumTest()
        {
            CheckTernaryArrayGenericArrayHelper<E>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericArrayWithObjectTest()
        {
            CheckTernaryArrayGenericArrayHelper<object>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericArrayWithStructTest()
        {
            CheckTernaryArrayGenericArrayHelper<S>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericArrayWithStructWithStringAndFieldTest()
        {
            CheckTernaryArrayGenericArrayHelper<Scs>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericWithClassRestrictionArrayWithCustomTest()
        {
            CheckTernaryArrayGenericWithClassRestrictionArrayHelper<C>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericWithClassRestrictionArrayWithObjectTest()
        {
            CheckTernaryArrayGenericWithClassRestrictionArrayHelper<object>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericWithSubClassRestrictionArrayWithCustomTest()
        {
            CheckTernaryArrayGenericWithSubClassRestrictionArrayHelper<C>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericWithClassAndNewRestrictionArrayWithCustomTest()
        {
            CheckTernaryArrayGenericWithClassAndNewRestrictionArrayHelper<C>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericWithClassAndNewRestrictionArrayWithObjectTest()
        {
            CheckTernaryArrayGenericWithClassAndNewRestrictionArrayHelper<object>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericWithSubClassAndNewRestrictionArrayWithCustomTest()
        {
            CheckTernaryArrayGenericWithSubClassAndNewRestrictionArrayHelper<C>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericWithStructRestrictionArrayWithEnumTest()
        {
            CheckTernaryArrayGenericWithStructRestrictionArrayHelper<E>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericWithStructRestrictionArrayWithStructTest()
        {
            CheckTernaryArrayGenericWithStructRestrictionArrayHelper<S>();
        }

        [Fact]
        public static void CheckTernaryArrayGenericWithStructRestrictionArrayWithStructWithStringAndFieldTest()
        {
            CheckTernaryArrayGenericWithStructRestrictionArrayHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckTernaryArrayGenericArrayHelper<T>()
        {
            bool[] array1 = new bool[] { false, true };
            T[][] array2 = new T[][] { null, new T[0], new T[] { default(T) }, new T[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayGenericArray<T>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        private static void CheckTernaryArrayGenericWithClassRestrictionArrayHelper<Tc>() where Tc : class
        {
            bool[] array1 = new bool[] { false, true };
            Tc[][] array2 = new Tc[][] { null, new Tc[0], new Tc[] { null, default(Tc) }, new Tc[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayGenericWithClassRestrictionArray<Tc>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        private static void CheckTernaryArrayGenericWithSubClassRestrictionArrayHelper<TC>() where TC : C
        {
            bool[] array1 = new bool[] { false, true };
            TC[][] array2 = new TC[][] { null, new TC[0], new TC[] { null, default(TC), (TC)new C() }, new TC[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayGenericWithSubClassRestrictionArray<TC>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        private static void CheckTernaryArrayGenericWithClassAndNewRestrictionArrayHelper<Tcn>() where Tcn : class, new()
        {
            bool[] array1 = new bool[] { false, true };
            Tcn[][] array2 = new Tcn[][] { null, new Tcn[0], new Tcn[] { null, default(Tcn), new Tcn() }, new Tcn[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayGenericWithClassAndNewRestrictionArray<Tcn>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        private static void CheckTernaryArrayGenericWithSubClassAndNewRestrictionArrayHelper<TCn>() where TCn : C, new()
        {
            bool[] array1 = new bool[] { false, true };
            TCn[][] array2 = new TCn[][] { null, new TCn[0], new TCn[] { null, default(TCn), new TCn(), (TCn)new C() }, new TCn[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayGenericWithSubClassAndNewRestrictionArray<TCn>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        private static void CheckTernaryArrayGenericWithStructRestrictionArrayHelper<Ts>() where Ts : struct
        {
            bool[] array1 = new bool[] { false, true };
            Ts[][] array2 = new Ts[][] { null, new Ts[0], new Ts[] { default(Ts), new Ts() }, new Ts[100] };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyArrayGenericWithStructRestrictionArray<Ts>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyArrayBoolArray(bool condition, bool[] a, bool[] b)
        {
            Expression<Func<bool[]>> e =
                Expression.Lambda<Func<bool[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(bool[])),
                        Expression.Constant(b, typeof(bool[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool[]> f = e.Compile();

            bool[] result = default(bool[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            bool[] expected = default(bool[]);
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

        private static void VerifyArrayByteArray(bool condition, byte[] a, byte[] b)
        {
            Expression<Func<byte[]>> e =
                Expression.Lambda<Func<byte[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(byte[])),
                        Expression.Constant(b, typeof(byte[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte[]> f = e.Compile();

            byte[] result = default(byte[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            byte[] expected = default(byte[]);
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

        private static void VerifyArrayCustomArray(bool condition, C[] a, C[] b)
        {
            Expression<Func<C[]>> e =
                Expression.Lambda<Func<C[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(C[])),
                        Expression.Constant(b, typeof(C[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C[]> f = e.Compile();

            C[] result = default(C[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            C[] expected = default(C[]);
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

        private static void VerifyArrayCharArray(bool condition, char[] a, char[] b)
        {
            Expression<Func<char[]>> e =
                Expression.Lambda<Func<char[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(char[])),
                        Expression.Constant(b, typeof(char[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char[]> f = e.Compile();

            char[] result = default(char[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            char[] expected = default(char[]);
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

        private static void VerifyArrayCustom2Array(bool condition, D[] a, D[] b)
        {
            Expression<Func<D[]>> e =
                Expression.Lambda<Func<D[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(D[])),
                        Expression.Constant(b, typeof(D[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D[]> f = e.Compile();

            D[] result = default(D[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            D[] expected = default(D[]);
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

        private static void VerifyArrayDecimalArray(bool condition, decimal[] a, decimal[] b)
        {
            Expression<Func<decimal[]>> e =
                Expression.Lambda<Func<decimal[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(decimal[])),
                        Expression.Constant(b, typeof(decimal[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal[]> f = e.Compile();

            decimal[] result = default(decimal[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            decimal[] expected = default(decimal[]);
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

        private static void VerifyArrayDelegateArray(bool condition, Delegate[] a, Delegate[] b)
        {
            Expression<Func<Delegate[]>> e =
                Expression.Lambda<Func<Delegate[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Delegate[])),
                        Expression.Constant(b, typeof(Delegate[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate[]> f = e.Compile();

            Delegate[] result = default(Delegate[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Delegate[] expected = default(Delegate[]);
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

        private static void VerifyArrayDoubleArray(bool condition, double[] a, double[] b)
        {
            Expression<Func<double[]>> e =
                Expression.Lambda<Func<double[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(double[])),
                        Expression.Constant(b, typeof(double[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double[]> f = e.Compile();

            double[] result = default(double[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            double[] expected = default(double[]);
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

        private static void VerifyArrayEnumArray(bool condition, E[] a, E[] b)
        {
            Expression<Func<E[]>> e =
                Expression.Lambda<Func<E[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(E[])),
                        Expression.Constant(b, typeof(E[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E[]> f = e.Compile();

            E[] result = default(E[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            E[] expected = default(E[]);
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

        private static void VerifyArrayEnumLongArray(bool condition, El[] a, El[] b)
        {
            Expression<Func<El[]>> e =
                Expression.Lambda<Func<El[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(El[])),
                        Expression.Constant(b, typeof(El[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El[]> f = e.Compile();

            El[] result = default(El[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            El[] expected = default(El[]);
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

        private static void VerifyArrayFloatArray(bool condition, float[] a, float[] b)
        {
            Expression<Func<float[]>> e =
                Expression.Lambda<Func<float[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(float[])),
                        Expression.Constant(b, typeof(float[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float[]> f = e.Compile();

            float[] result = default(float[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            float[] expected = default(float[]);
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

        private static void VerifyArrayFuncOfObject(bool condition, Func<object>[] a, Func<object>[] b)
        {
            Expression<Func<Func<object>[]>> e =
                Expression.Lambda<Func<Func<object>[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Func<object>[])),
                        Expression.Constant(b, typeof(Func<object>[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>[]> f = e.Compile();

            Func<object>[] result = default(Func<object>[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Func<object>[] expected = default(Func<object>[]);
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

        private static void VerifyArrayInterfaceArray(bool condition, I[] a, I[] b)
        {
            Expression<Func<I[]>> e =
                Expression.Lambda<Func<I[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(I[])),
                        Expression.Constant(b, typeof(I[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I[]> f = e.Compile();

            I[] result = default(I[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            I[] expected = default(I[]);
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

        private static void VerifyArrayIEquatableOfCustom(bool condition, IEquatable<C>[] a, IEquatable<C>[] b)
        {
            Expression<Func<IEquatable<C>[]>> e =
                Expression.Lambda<Func<IEquatable<C>[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(IEquatable<C>[])),
                        Expression.Constant(b, typeof(IEquatable<C>[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>[]> f = e.Compile();

            IEquatable<C>[] result = default(IEquatable<C>[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            IEquatable<C>[] expected = default(IEquatable<C>[]);
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

        private static void VerifyArrayIEquatableOfCustom2(bool condition, IEquatable<D>[] a, IEquatable<D>[] b)
        {
            Expression<Func<IEquatable<D>[]>> e =
                Expression.Lambda<Func<IEquatable<D>[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(IEquatable<D>[])),
                        Expression.Constant(b, typeof(IEquatable<D>[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>[]> f = e.Compile();

            IEquatable<D>[] result = default(IEquatable<D>[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            IEquatable<D>[] expected = default(IEquatable<D>[]);
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

        private static void VerifyArrayIntArray(bool condition, int[] a, int[] b)
        {
            Expression<Func<int[]>> e =
                Expression.Lambda<Func<int[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(int[])),
                        Expression.Constant(b, typeof(int[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int[]> f = e.Compile();

            int[] result = default(int[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            int[] expected = default(int[]);
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

        private static void VerifyArrayLongArray(bool condition, long[] a, long[] b)
        {
            Expression<Func<long[]>> e =
                Expression.Lambda<Func<long[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(long[])),
                        Expression.Constant(b, typeof(long[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long[]> f = e.Compile();

            long[] result = default(long[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            long[] expected = default(long[]);
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

        private static void VerifyArrayObjectArray(bool condition, object[] a, object[] b)
        {
            Expression<Func<object[]>> e =
                Expression.Lambda<Func<object[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(object[])),
                        Expression.Constant(b, typeof(object[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object[]> f = e.Compile();

            object[] result = default(object[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            object[] expected = default(object[]);
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

        private static void VerifyArrayStructArray(bool condition, S[] a, S[] b)
        {
            Expression<Func<S[]>> e =
                Expression.Lambda<Func<S[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(S[])),
                        Expression.Constant(b, typeof(S[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S[]> f = e.Compile();

            S[] result = default(S[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            S[] expected = default(S[]);
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

        private static void VerifyArraySByteArray(bool condition, sbyte[] a, sbyte[] b)
        {
            Expression<Func<sbyte[]>> e =
                Expression.Lambda<Func<sbyte[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(sbyte[])),
                        Expression.Constant(b, typeof(sbyte[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte[]> f = e.Compile();

            sbyte[] result = default(sbyte[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            sbyte[] expected = default(sbyte[]);
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

        private static void VerifyArrayStructWithStringArray(bool condition, Sc[] a, Sc[] b)
        {
            Expression<Func<Sc[]>> e =
                Expression.Lambda<Func<Sc[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Sc[])),
                        Expression.Constant(b, typeof(Sc[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc[]> f = e.Compile();

            Sc[] result = default(Sc[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Sc[] expected = default(Sc[]);
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

        private static void VerifyArrayStructWithStringAndFieldArray(bool condition, Scs[] a, Scs[] b)
        {
            Expression<Func<Scs[]>> e =
                Expression.Lambda<Func<Scs[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Scs[])),
                        Expression.Constant(b, typeof(Scs[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs[]> f = e.Compile();

            Scs[] result = default(Scs[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Scs[] expected = default(Scs[]);
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

        private static void VerifyArrayShortArray(bool condition, short[] a, short[] b)
        {
            Expression<Func<short[]>> e =
                Expression.Lambda<Func<short[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(short[])),
                        Expression.Constant(b, typeof(short[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short[]> f = e.Compile();

            short[] result = default(short[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            short[] expected = default(short[]);
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

        private static void VerifyArrayStructWithTwoValuesArray(bool condition, Sp[] a, Sp[] b)
        {
            Expression<Func<Sp[]>> e =
                Expression.Lambda<Func<Sp[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Sp[])),
                        Expression.Constant(b, typeof(Sp[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp[]> f = e.Compile();

            Sp[] result = default(Sp[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Sp[] expected = default(Sp[]);
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

        private static void VerifyArrayStructWithValueArray(bool condition, Ss[] a, Ss[] b)
        {
            Expression<Func<Ss[]>> e =
                Expression.Lambda<Func<Ss[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Ss[])),
                        Expression.Constant(b, typeof(Ss[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss[]> f = e.Compile();

            Ss[] result = default(Ss[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Ss[] expected = default(Ss[]);
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

        private static void VerifyArrayStringArray(bool condition, string[] a, string[] b)
        {
            Expression<Func<string[]>> e =
                Expression.Lambda<Func<string[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(string[])),
                        Expression.Constant(b, typeof(string[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string[]> f = e.Compile();

            string[] result = default(string[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            string[] expected = default(string[]);
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

        private static void VerifyArrayUIntArray(bool condition, uint[] a, uint[] b)
        {
            Expression<Func<uint[]>> e =
                Expression.Lambda<Func<uint[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(uint[])),
                        Expression.Constant(b, typeof(uint[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint[]> f = e.Compile();

            uint[] result = default(uint[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            uint[] expected = default(uint[]);
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

        private static void VerifyArrayULongArray(bool condition, ulong[] a, ulong[] b)
        {
            Expression<Func<ulong[]>> e =
                Expression.Lambda<Func<ulong[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(ulong[])),
                        Expression.Constant(b, typeof(ulong[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong[]> f = e.Compile();

            ulong[] result = default(ulong[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ulong[] expected = default(ulong[]);
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

        private static void VerifyArrayUShortArray(bool condition, ushort[] a, ushort[] b)
        {
            Expression<Func<ushort[]>> e =
                Expression.Lambda<Func<ushort[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(ushort[])),
                        Expression.Constant(b, typeof(ushort[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort[]> f = e.Compile();

            ushort[] result = default(ushort[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ushort[] expected = default(ushort[]);
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

        private static void VerifyArrayGenericArray<T>(bool condition, T[] a, T[] b)
        {
            Expression<Func<T[]>> e =
                Expression.Lambda<Func<T[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(T[])),
                        Expression.Constant(b, typeof(T[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T[]> f = e.Compile();

            T[] result = default(T[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            T[] expected = default(T[]);
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

        private static void VerifyArrayGenericWithClassRestrictionArray<Tc>(bool condition, Tc[] a, Tc[] b) where Tc : class
        {
            Expression<Func<Tc[]>> e =
                Expression.Lambda<Func<Tc[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Tc[])),
                        Expression.Constant(b, typeof(Tc[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc[]> f = e.Compile();

            Tc[] result = default(Tc[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Tc[] expected = default(Tc[]);
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

        private static void VerifyArrayGenericWithSubClassRestrictionArray<TC>(bool condition, TC[] a, TC[] b) where TC : C
        {
            Expression<Func<TC[]>> e =
                Expression.Lambda<Func<TC[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(TC[])),
                        Expression.Constant(b, typeof(TC[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC[]> f = e.Compile();

            TC[] result = default(TC[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            TC[] expected = default(TC[]);
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

        private static void VerifyArrayGenericWithClassAndNewRestrictionArray<Tcn>(bool condition, Tcn[] a, Tcn[] b) where Tcn : class, new()
        {
            Expression<Func<Tcn[]>> e =
                Expression.Lambda<Func<Tcn[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Tcn[])),
                        Expression.Constant(b, typeof(Tcn[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn[]> f = e.Compile();

            Tcn[] result = default(Tcn[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Tcn[] expected = default(Tcn[]);
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

        private static void VerifyArrayGenericWithSubClassAndNewRestrictionArray<TCn>(bool condition, TCn[] a, TCn[] b) where TCn : C, new()
        {
            Expression<Func<TCn[]>> e =
                Expression.Lambda<Func<TCn[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(TCn[])),
                        Expression.Constant(b, typeof(TCn[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn[]> f = e.Compile();

            TCn[] result = default(TCn[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            TCn[] expected = default(TCn[]);
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

        private static void VerifyArrayGenericWithStructRestrictionArray<Ts>(bool condition, Ts[] a, Ts[] b) where Ts : struct
        {
            Expression<Func<Ts[]>> e =
                Expression.Lambda<Func<Ts[]>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Ts[])),
                        Expression.Constant(b, typeof(Ts[]))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts[]> f = e.Compile();

            Ts[] result = default(Ts[]);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Ts[] expected = default(Ts[]);
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
