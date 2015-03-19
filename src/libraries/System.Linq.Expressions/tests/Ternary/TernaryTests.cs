// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Ternary
{
    public static unsafe class TernaryTests
    {
        #region Test methods

        [Fact]
        public static void CheckTernaryBoolTest()
        {
            bool[] array1 = new bool[] { false, true };
            bool[] array2 = new bool[] { true, false };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyBool(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryByteTest()
        {
            bool[] array1 = new bool[] { false, true };
            byte[] array2 = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyByte(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryCustomTest()
        {
            bool[] array1 = new bool[] { false, true };
            C[] array2 = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyCustom(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryCharTest()
        {
            bool[] array1 = new bool[] { false, true };
            char[] array2 = new char[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyChar(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryCustom2Test()
        {
            bool[] array1 = new bool[] { false, true };
            D[] array2 = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyCustom2(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryDecimalTest()
        {
            bool[] array1 = new bool[] { false, true };
            decimal[] array2 = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyDecimal(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryDelegateTest()
        {
            bool[] array1 = new bool[] { false, true };
            Delegate[] array2 = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyDelegate(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryDoubleTest()
        {
            bool[] array1 = new bool[] { false, true };
            double[] array2 = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyDouble(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryEnumTest()
        {
            bool[] array1 = new bool[] { false, true };
            E[] array2 = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyEnum(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryEnumLongTest()
        {
            bool[] array1 = new bool[] { false, true };
            El[] array2 = new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyEnumLong(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryFloatTest()
        {
            bool[] array1 = new bool[] { false, true };
            float[] array2 = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyFloat(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryFuncOfObjectTest()
        {
            bool[] array1 = new bool[] { false, true };
            Func<object>[] array2 = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyFuncOfObject(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryInterfaceTest()
        {
            bool[] array1 = new bool[] { false, true };
            I[] array2 = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyInterface(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryIEquatableOfCustomTest()
        {
            bool[] array1 = new bool[] { false, true };
            IEquatable<C>[] array2 = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyIEquatableOfCustom(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryIEquatableOfCustom2Test()
        {
            bool[] array1 = new bool[] { false, true };
            IEquatable<D>[] array2 = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyIEquatableOfCustom2(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryIntTest()
        {
            bool[] array1 = new bool[] { false, true };
            int[] array2 = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyInt(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryLongTest()
        {
            bool[] array1 = new bool[] { false, true };
            long[] array2 = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyLong(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryObjectTest()
        {
            bool[] array1 = new bool[] { false, true };
            object[] array2 = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyObject(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryStructTest()
        {
            bool[] array1 = new bool[] { false, true };
            S[] array2 = new S[] { default(S), new S() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyStruct(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernarySByteTest()
        {
            bool[] array1 = new bool[] { false, true };
            sbyte[] array2 = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifySByte(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryStructWithStringTest()
        {
            bool[] array1 = new bool[] { false, true };
            Sc[] array2 = new Sc[] { default(Sc), new Sc(), new Sc(null) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyStructWithString(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryStructWithStringAndFieldTest()
        {
            bool[] array1 = new bool[] { false, true };
            Scs[] array2 = new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyStructWithStringAndField(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryShortTest()
        {
            bool[] array1 = new bool[] { false, true };
            short[] array2 = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyShort(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryStructWithTwoValuesTest()
        {
            bool[] array1 = new bool[] { false, true };
            Sp[] array2 = new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyStructWithTwoValues(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryStructWithValueTest()
        {
            bool[] array1 = new bool[] { false, true };
            Ss[] array2 = new Ss[] { default(Ss), new Ss(), new Ss(new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyStructWithValue(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryStringTest()
        {
            bool[] array1 = new bool[] { false, true };
            string[] array2 = new string[] { null, "", "a", "foo" };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyString(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryUIntTest()
        {
            bool[] array1 = new bool[] { false, true };
            uint[] array2 = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyUInt(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryULongTest()
        {
            bool[] array1 = new bool[] { false, true };
            ulong[] array2 = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyULong(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryUShortTest()
        {
            bool[] array1 = new bool[] { false, true };
            ushort[] array2 = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyUShort(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryGenericWithCustomTest()
        {
            CheckTernaryGenericHelper<C>();
        }

        [Fact]
        public static void CheckTernaryGenericWithEnumTest()
        {
            CheckTernaryGenericHelper<E>();
        }

        [Fact]
        public static void CheckTernaryGenericWithObjectTest()
        {
            CheckTernaryGenericHelper<object>();
        }

        [Fact]
        public static void CheckTernaryGenericWithStructTest()
        {
            CheckTernaryGenericHelper<S>();
        }

        [Fact]
        public static void CheckTernaryGenericWithStructWithStringAndFieldTest()
        {
            CheckTernaryGenericHelper<Scs>();
        }

        [Fact]
        public static void CheckTernaryGenericWithClassRestrictionWithCustomTest()
        {
            CheckTernaryGenericWithClassRestrictionHelper<C>();
        }

        [Fact]
        public static void CheckTernaryGenericWithClassRestrictionWithObjectTest()
        {
            CheckTernaryGenericWithClassRestrictionHelper<object>();
        }

        [Fact]
        public static void CheckTernaryGenericWithSubClassRestrictionWithCustomTest()
        {
            CheckTernaryGenericWithSubClassRestrictionHelper<C>();
        }

        [Fact]
        public static void CheckTernaryGenericWithClassAndNewRestrictionWithCustomTest()
        {
            CheckTernaryGenericWithClassAndNewRestrictionHelper<C>();
        }

        [Fact]
        public static void CheckTernaryGenericWithClassAndNewRestrictionWithObjectTest()
        {
            CheckTernaryGenericWithClassAndNewRestrictionHelper<object>();
        }

        [Fact]
        public static void CheckTernaryGenericWithSubClassAndNewRestrictionWithCustomTest()
        {
            CheckTernaryGenericWithSubClassAndNewRestrictionHelper<C>();
        }

        [Fact]
        public static void CheckTernaryGenericWithStructRestrictionWithEnumTest()
        {
            CheckTernaryGenericWithStructRestrictionHelper<E>();
        }

        [Fact]
        public static void CheckTernaryGenericWithStructRestrictionWithStructTest()
        {
            CheckTernaryGenericWithStructRestrictionHelper<S>();
        }

        [Fact]
        public static void CheckTernaryGenericWithStructRestrictionWithStructWithStringAndFieldTest()
        {
            CheckTernaryGenericWithStructRestrictionHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckTernaryGenericHelper<T>()
        {
            bool[] array1 = new bool[] { false, true };
            T[] array2 = new T[] { default(T) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGeneric<T>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        private static void CheckTernaryGenericWithClassRestrictionHelper<Tc>() where Tc : class
        {
            bool[] array1 = new bool[] { false, true };
            Tc[] array2 = new Tc[] { null, default(Tc) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGenericWithClassRestriction<Tc>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        private static void CheckTernaryGenericWithSubClassRestrictionHelper<TC>() where TC : C
        {
            bool[] array1 = new bool[] { false, true };
            TC[] array2 = new TC[] { null, default(TC), (TC)new C() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGenericWithSubClassRestriction<TC>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        private static void CheckTernaryGenericWithClassAndNewRestrictionHelper<Tcn>() where Tcn : class, new()
        {
            bool[] array1 = new bool[] { false, true };
            Tcn[] array2 = new Tcn[] { null, default(Tcn), new Tcn() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGenericWithClassAndNewRestriction<Tcn>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        private static void CheckTernaryGenericWithSubClassAndNewRestrictionHelper<TCn>() where TCn : C, new()
        {
            bool[] array1 = new bool[] { false, true };
            TCn[] array2 = new TCn[] { null, default(TCn), new TCn(), (TCn)new C() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGenericWithSubClassAndNewRestriction<TCn>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        private static void CheckTernaryGenericWithStructRestrictionHelper<Ts>() where Ts : struct
        {
            bool[] array1 = new bool[] { false, true };
            Ts[] array2 = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyGenericWithStructRestriction<Ts>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBool(bool condition, bool a, bool b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            bool result = default(bool);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            bool expected = default(bool);
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

        private static void VerifyByte(bool condition, byte a, byte b)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(byte)),
                        Expression.Constant(b, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile();

            byte result = default(byte);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            byte expected = default(byte);
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

        private static void VerifyCustom(bool condition, C a, C b)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(C)),
                        Expression.Constant(b, typeof(C))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile();

            C result = default(C);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            C expected = default(C);
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

        private static void VerifyChar(bool condition, char a, char b)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(char)),
                        Expression.Constant(b, typeof(char))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile();

            char result = default(char);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            char expected = default(char);
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

        private static void VerifyCustom2(bool condition, D a, D b)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(D)),
                        Expression.Constant(b, typeof(D))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile();

            D result = default(D);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            D expected = default(D);
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

        private static void VerifyDecimal(bool condition, decimal a, decimal b)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(decimal)),
                        Expression.Constant(b, typeof(decimal))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile();

            decimal result = default(decimal);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            decimal expected = default(decimal);
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

        private static void VerifyDelegate(bool condition, Delegate a, Delegate b)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Delegate)),
                        Expression.Constant(b, typeof(Delegate))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile();

            Delegate result = default(Delegate);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Delegate expected = default(Delegate);
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

        private static void VerifyDouble(bool condition, double a, double b)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(double)),
                        Expression.Constant(b, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile();

            double result = default(double);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            double expected = default(double);
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

        private static void VerifyEnum(bool condition, E a, E b)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(E)),
                        Expression.Constant(b, typeof(E))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile();

            E result = default(E);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            E expected = default(E);
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

        private static void VerifyEnumLong(bool condition, El a, El b)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(El)),
                        Expression.Constant(b, typeof(El))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile();

            El result = default(El);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            El expected = default(El);
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

        private static void VerifyFloat(bool condition, float a, float b)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(float)),
                        Expression.Constant(b, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile();

            float result = default(float);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            float expected = default(float);
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

        private static void VerifyFuncOfObject(bool condition, Func<object> a, Func<object> b)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Func<object>)),
                        Expression.Constant(b, typeof(Func<object>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>> f = e.Compile();

            Func<object> result = default(Func<object>);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Func<object> expected = default(Func<object>);
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

        private static void VerifyInterface(bool condition, I a, I b)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(I)),
                        Expression.Constant(b, typeof(I))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile();

            I result = default(I);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            I expected = default(I);
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

        private static void VerifyIEquatableOfCustom(bool condition, IEquatable<C> a, IEquatable<C> b)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(IEquatable<C>)),
                        Expression.Constant(b, typeof(IEquatable<C>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile();

            IEquatable<C> result = default(IEquatable<C>);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            IEquatable<C> expected = default(IEquatable<C>);
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

        private static void VerifyIEquatableOfCustom2(bool condition, IEquatable<D> a, IEquatable<D> b)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(IEquatable<D>)),
                        Expression.Constant(b, typeof(IEquatable<D>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile();

            IEquatable<D> result = default(IEquatable<D>);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            IEquatable<D> expected = default(IEquatable<D>);
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

        private static void VerifyInt(bool condition, int a, int b)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            int result = default(int);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            int expected = default(int);
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

        private static void VerifyLong(bool condition, long a, long b)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile();

            long result = default(long);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            long expected = default(long);
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

        private static void VerifyObject(bool condition, object a, object b)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(object)),
                        Expression.Constant(b, typeof(object))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            object result = default(object);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            object expected = default(object);
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

        private static void VerifyStruct(bool condition, S a, S b)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(S)),
                        Expression.Constant(b, typeof(S))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile();

            S result = default(S);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            S expected = default(S);
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

        private static void VerifySByte(bool condition, sbyte a, sbyte b)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(sbyte)),
                        Expression.Constant(b, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile();

            sbyte result = default(sbyte);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            sbyte expected = default(sbyte);
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

        private static void VerifyStructWithString(bool condition, Sc a, Sc b)
        {
            Expression<Func<Sc>> e =
                Expression.Lambda<Func<Sc>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Sc)),
                        Expression.Constant(b, typeof(Sc))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc> f = e.Compile();

            Sc result = default(Sc);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Sc expected = default(Sc);
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

        private static void VerifyStructWithStringAndField(bool condition, Scs a, Scs b)
        {
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Scs)),
                        Expression.Constant(b, typeof(Scs))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile();

            Scs result = default(Scs);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Scs expected = default(Scs);
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

        private static void VerifyShort(bool condition, short a, short b)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile();

            short result = default(short);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            short expected = default(short);
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

        private static void VerifyStructWithTwoValues(bool condition, Sp a, Sp b)
        {
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Sp)),
                        Expression.Constant(b, typeof(Sp))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile();

            Sp result = default(Sp);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Sp expected = default(Sp);
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

        private static void VerifyStructWithValue(bool condition, Ss a, Ss b)
        {
            Expression<Func<Ss>> e =
                Expression.Lambda<Func<Ss>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Ss)),
                        Expression.Constant(b, typeof(Ss))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss> f = e.Compile();

            Ss result = default(Ss);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Ss expected = default(Ss);
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

        private static void VerifyString(bool condition, string a, string b)
        {
            Expression<Func<string>> e =
                Expression.Lambda<Func<string>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(string)),
                        Expression.Constant(b, typeof(string))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string> f = e.Compile();

            string result = default(string);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            string expected = default(string);
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

        private static void VerifyUInt(bool condition, uint a, uint b)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile();

            uint result = default(uint);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            uint expected = default(uint);
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

        private static void VerifyULong(bool condition, ulong a, ulong b)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile();

            ulong result = default(ulong);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ulong expected = default(ulong);
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

        private static void VerifyUShort(bool condition, ushort a, ushort b)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile();

            ushort result = default(ushort);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ushort expected = default(ushort);
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

        private static void VerifyGeneric<T>(bool condition, T a, T b)
        {
            Expression<Func<T>> e =
                Expression.Lambda<Func<T>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(T)),
                        Expression.Constant(b, typeof(T))),
                    Enumerable.Empty<ParameterExpression>());
            Func<T> f = e.Compile();

            T result = default(T);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            T expected = default(T);
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

        private static void VerifyGenericWithClassRestriction<Tc>(bool condition, Tc a, Tc b)
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Tc)),
                        Expression.Constant(b, typeof(Tc))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile();

            Tc result = default(Tc);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Tc expected = default(Tc);
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

        private static void VerifyGenericWithSubClassRestriction<TC>(bool condition, TC a, TC b)
        {
            Expression<Func<TC>> e =
                Expression.Lambda<Func<TC>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(TC)),
                        Expression.Constant(b, typeof(TC))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC> f = e.Compile();

            TC result = default(TC);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            TC expected = default(TC);
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

        private static void VerifyGenericWithClassAndNewRestriction<Tcn>(bool condition, Tcn a, Tcn b)
        {
            Expression<Func<Tcn>> e =
                Expression.Lambda<Func<Tcn>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Tcn)),
                        Expression.Constant(b, typeof(Tcn))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn> f = e.Compile();

            Tcn result = default(Tcn);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Tcn expected = default(Tcn);
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

        private static void VerifyGenericWithSubClassAndNewRestriction<TCn>(bool condition, TCn a, TCn b)
        {
            Expression<Func<TCn>> e =
                Expression.Lambda<Func<TCn>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(TCn)),
                        Expression.Constant(b, typeof(TCn))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn> f = e.Compile();

            TCn result = default(TCn);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            TCn expected = default(TCn);
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

        private static void VerifyGenericWithStructRestriction<Ts>(bool condition, Ts a, Ts b)
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Ts)),
                        Expression.Constant(b, typeof(Ts))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile();

            Ts result = default(Ts);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Ts expected = default(Ts);
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
