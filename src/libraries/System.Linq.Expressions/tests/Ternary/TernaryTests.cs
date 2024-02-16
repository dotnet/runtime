// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class TernaryTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryBoolTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            bool[] array2 = new bool[] { true, false };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyBool(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryByteTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            byte[] array2 = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyByte(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryCustomTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            C[] array2 = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyCustom(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryCharTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            char[] array2 = new char[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyChar(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryCustom2Test(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            D[] array2 = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyCustom2(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryDecimalTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            decimal[] array2 = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyDecimal(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryDelegateTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            Delegate[] array2 = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyDelegate(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryDoubleTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            double[] array2 = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyDouble(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryEnumTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            E[] array2 = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyEnum(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryEnumLongTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            El[] array2 = new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyEnumLong(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryFloatTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            float[] array2 = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyFloat(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryFuncOfObjectTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            Func<object>[] array2 = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyFuncOfObject(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryInterfaceTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            I[] array2 = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyInterface(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryIEquatableOfCustomTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            IEquatable<C>[] array2 = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyIEquatableOfCustom(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryIEquatableOfCustom2Test(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            IEquatable<D>[] array2 = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyIEquatableOfCustom2(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryIntTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            int[] array2 = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyInt(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryLongTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            long[] array2 = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyLong(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryObjectTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            object[] array2 = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyObject(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryStructTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            S[] array2 = new S[] { default(S), new S() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyStruct(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernarySByteTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            sbyte[] array2 = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifySByte(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryStructWithStringTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            Sc[] array2 = new Sc[] { default(Sc), new Sc(), new Sc(null) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyStructWithString(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryStructWithStringAndFieldTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            Scs[] array2 = new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyStructWithStringAndField(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryShortTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            short[] array2 = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyShort(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryStructWithTwoValuesTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            Sp[] array2 = new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyStructWithTwoValues(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryStructWithValueTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            Ss[] array2 = new Ss[] { default(Ss), new Ss(), new Ss(new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyStructWithValue(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryStringTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            string[] array2 = new string[] { null, "", "a", "foo" };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyString(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryUIntTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            uint[] array2 = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyUInt(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryULongTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            ulong[] array2 = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyULong(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryUShortTest(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            ushort[] array2 = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyUShort(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithCustomTest(bool useInterpreter)
        {
            CheckTernaryGenericHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithEnumTest(bool useInterpreter)
        {
            CheckTernaryGenericHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithObjectTest(bool useInterpreter)
        {
            CheckTernaryGenericHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithStructTest(bool useInterpreter)
        {
            CheckTernaryGenericHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithStructWithStringAndFieldTest(bool useInterpreter)
        {
            CheckTernaryGenericHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithClassRestrictionWithCustomTest(bool useInterpreter)
        {
            CheckTernaryGenericWithClassRestrictionHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithClassRestrictionWithObjectTest(bool useInterpreter)
        {
            CheckTernaryGenericWithClassRestrictionHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithSubClassRestrictionWithCustomTest(bool useInterpreter)
        {
            CheckTernaryGenericWithSubClassRestrictionHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithClassAndNewRestrictionWithCustomTest(bool useInterpreter)
        {
            CheckTernaryGenericWithClassAndNewRestrictionHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithClassAndNewRestrictionWithObjectTest(bool useInterpreter)
        {
            CheckTernaryGenericWithClassAndNewRestrictionHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithSubClassAndNewRestrictionWithCustomTest(bool useInterpreter)
        {
            CheckTernaryGenericWithSubClassAndNewRestrictionHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithStructRestrictionWithEnumTest(bool useInterpreter)
        {
            CheckTernaryGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithStructRestrictionWithStructTest(bool useInterpreter)
        {
            CheckTernaryGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTernaryGenericWithStructRestrictionWithStructWithStringAndFieldTest(bool useInterpreter)
        {
            CheckTernaryGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckTernaryGenericHelper<T>(bool useInterpreter)
        {
            bool[] array1 = new bool[] { false, true };
            T[] array2 = new T[] { default(T) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGeneric<T>(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        private static void CheckTernaryGenericWithClassRestrictionHelper<Tc>(bool useInterpreter) where Tc : class
        {
            bool[] array1 = new bool[] { false, true };
            Tc[] array2 = new Tc[] { null, default(Tc) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGenericWithClassRestriction<Tc>(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        private static void CheckTernaryGenericWithSubClassRestrictionHelper<TC>(bool useInterpreter) where TC : C
        {
            bool[] array1 = new bool[] { false, true };
            TC[] array2 = new TC[] { null, default(TC), (TC)new C() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGenericWithSubClassRestriction<TC>(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        private static void CheckTernaryGenericWithClassAndNewRestrictionHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            bool[] array1 = new bool[] { false, true };
            Tcn[] array2 = new Tcn[] { null, default(Tcn), new Tcn() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGenericWithClassAndNewRestriction<Tcn>(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        private static void CheckTernaryGenericWithSubClassAndNewRestrictionHelper<TCn>(bool useInterpreter) where TCn : C, new()
        {
            bool[] array1 = new bool[] { false, true };
            TCn[] array2 = new TCn[] { null, default(TCn), new TCn(), (TCn)new C() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGenericWithSubClassAndNewRestriction<TCn>(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        private static void CheckTernaryGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            bool[] array1 = new bool[] { false, true };
            Ts[] array2 = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGenericWithStructRestriction<Ts>(array1[i], array2[j], array2[k], useInterpreter);
                    }
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBool(bool condition, bool a, bool b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyByte(bool condition, byte a, byte b, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(byte)),
                        Expression.Constant(b, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyCustom(bool condition, C a, C b, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(C)),
                        Expression.Constant(b, typeof(C))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyChar(bool condition, char a, char b, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(char)),
                        Expression.Constant(b, typeof(char))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyCustom2(bool condition, D a, D b, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(D)),
                        Expression.Constant(b, typeof(D))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyDecimal(bool condition, decimal a, decimal b, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(decimal)),
                        Expression.Constant(b, typeof(decimal))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyDelegate(bool condition, Delegate a, Delegate b, bool useInterpreter)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Delegate)),
                        Expression.Constant(b, typeof(Delegate))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyDouble(bool condition, double a, double b, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(double)),
                        Expression.Constant(b, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyEnum(bool condition, E a, E b, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(E)),
                        Expression.Constant(b, typeof(E))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyEnumLong(bool condition, El a, El b, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(El)),
                        Expression.Constant(b, typeof(El))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyFloat(bool condition, float a, float b, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(float)),
                        Expression.Constant(b, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyFuncOfObject(bool condition, Func<object> a, Func<object> b, bool useInterpreter)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Func<object>)),
                        Expression.Constant(b, typeof(Func<object>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyInterface(bool condition, I a, I b, bool useInterpreter)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(I)),
                        Expression.Constant(b, typeof(I))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyIEquatableOfCustom(bool condition, IEquatable<C> a, IEquatable<C> b, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(IEquatable<C>)),
                        Expression.Constant(b, typeof(IEquatable<C>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyIEquatableOfCustom2(bool condition, IEquatable<D> a, IEquatable<D> b, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(IEquatable<D>)),
                        Expression.Constant(b, typeof(IEquatable<D>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyInt(bool condition, int a, int b, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyLong(bool condition, long a, long b, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyObject(bool condition, object a, object b, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(object)),
                        Expression.Constant(b, typeof(object))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyStruct(bool condition, S a, S b, bool useInterpreter)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(S)),
                        Expression.Constant(b, typeof(S))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifySByte(bool condition, sbyte a, sbyte b, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(sbyte)),
                        Expression.Constant(b, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyStructWithString(bool condition, Sc a, Sc b, bool useInterpreter)
        {
            Expression<Func<Sc>> e =
                Expression.Lambda<Func<Sc>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Sc)),
                        Expression.Constant(b, typeof(Sc))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyStructWithStringAndField(bool condition, Scs a, Scs b, bool useInterpreter)
        {
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Scs)),
                        Expression.Constant(b, typeof(Scs))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyShort(bool condition, short a, short b, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyStructWithTwoValues(bool condition, Sp a, Sp b, bool useInterpreter)
        {
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Sp)),
                        Expression.Constant(b, typeof(Sp))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyStructWithValue(bool condition, Ss a, Ss b, bool useInterpreter)
        {
            Expression<Func<Ss>> e =
                Expression.Lambda<Func<Ss>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Ss)),
                        Expression.Constant(b, typeof(Ss))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyString(bool condition, string a, string b, bool useInterpreter)
        {
            Expression<Func<string>> e =
                Expression.Lambda<Func<string>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(string)),
                        Expression.Constant(b, typeof(string))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyUInt(bool condition, uint a, uint b, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyULong(bool condition, ulong a, ulong b, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyUShort(bool condition, ushort a, ushort b, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyGeneric<T>(bool condition, T a, T b, bool useInterpreter)
        {
            Expression<Func<T>> e =
                Expression.Lambda<Func<T>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(T)),
                        Expression.Constant(b, typeof(T))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T> f = e.Compile(useInterpreter);

            if (default(T) == null)
#pragma warning disable xUnit2005 // Do not use Assert.Same() on value type 'T'. Value types do not have identity. Use Assert.Equal instead.
                Assert.Same((object)(condition ? a : b), (object)f());
#pragma warning restore xUnit2005
            else
                Assert.Equal(condition ? a : b, f());
        }

        private static void VerifyGenericWithClassRestriction<Tc>(bool condition, Tc a, Tc b, bool useInterpreter) where Tc : class
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Tc)),
                        Expression.Constant(b, typeof(Tc))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyGenericWithSubClassRestriction<TC>(bool condition, TC a, TC b, bool useInterpreter) where TC : class
        {
            Expression<Func<TC>> e =
                Expression.Lambda<Func<TC>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(TC)),
                        Expression.Constant(b, typeof(TC))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyGenericWithClassAndNewRestriction<Tcn>(bool condition, Tcn a, Tcn b, bool useInterpreter) where Tcn : class
        {
            Expression<Func<Tcn>> e =
                Expression.Lambda<Func<Tcn>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Tcn)),
                        Expression.Constant(b, typeof(Tcn))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyGenericWithSubClassAndNewRestriction<TCn>(bool condition, TCn a, TCn b, bool useInterpreter) where TCn : class
        {
            Expression<Func<TCn>> e =
                Expression.Lambda<Func<TCn>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(TCn)),
                        Expression.Constant(b, typeof(TCn))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn> f = e.Compile(useInterpreter);

            Assert.Same(condition ? a : b, f());
        }

        private static void VerifyGenericWithStructRestriction<Ts>(bool condition, Ts a, Ts b, bool useInterpreter)
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Ts)),
                        Expression.Constant(b, typeof(Ts))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile(useInterpreter);

            Assert.Equal(condition ? a : b, f());
        }

        #endregion
    }
}
