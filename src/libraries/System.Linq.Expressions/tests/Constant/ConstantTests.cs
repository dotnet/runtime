// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Constant
{
    public static unsafe class ConstantTests
    {
        #region Test methods

        [Fact]
        public static void CheckBoolConstantTest()
        {
            foreach (bool value in new bool[] { true, false })
            {
                VerifyBoolConstant(value);
            }
        }

        [Fact]
        public static void CheckByteConstantTest()
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteConstant(value);
            }
        }

        [Fact]
        public static void CheckCustomConstantTest()
        {
            foreach (C value in new C[] { null, new C(), new D(), new D(0), new D(5) })
            {
                VerifyCustomConstant(value);
            }
        }

        [Fact]
        public static void CheckCharConstantTest()
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharConstant(value);
            }
        }

        [Fact]
        public static void CheckCustom2ConstantTest()
        {
            foreach (D value in new D[] { null, new D(), new D(0), new D(5) })
            {
                VerifyCustom2Constant(value);
            }
        }

        [Fact]
        public static void CheckDecimalConstantTest()
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue })
            {
                VerifyDecimalConstant(value);
            }
        }

        [Fact]
        public static void CheckDelegateConstantTest()
        {
            foreach (Delegate value in new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } })
            {
                VerifyDelegateConstant(value);
            }
        }

        [Fact]
        public static void CheckDoubleConstantTest()
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleConstant(value);
            }
        }

        [Fact]
        public static void CheckEnumConstantTest()
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumConstant(value);
            }
        }

        [Fact]
        public static void CheckEnumLongConstantTest()
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongConstant(value);
            }
        }

        [Fact]
        public static void CheckFloatConstantTest()
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatConstant(value);
            }
        }

        [Fact]
        public static void CheckFuncOfObjectConstantTest()
        {
            foreach (Func<object> value in new Func<object>[] { null, (Func<object>)delegate () { return null; } })
            {
                VerifyFuncOfObjectConstant(value);
            }
        }

        [Fact]
        public static void CheckInterfaceConstantTest()
        {
            foreach (I value in new I[] { null, new C(), new D(), new D(0), new D(5) })
            {
                VerifyInterfaceConstant(value);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustomConstantTest()
        {
            foreach (IEquatable<C> value in new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) })
            {
                VerifyIEquatableOfCustomConstant(value);
            }
        }

        [Fact]
        public static void CheckIEquatableOfCustom2ConstantTest()
        {
            foreach (IEquatable<D> value in new IEquatable<D>[] { null, new D(), new D(0), new D(5) })
            {
                VerifyIEquatableOfCustom2Constant(value);
            }
        }

        [Fact]
        public static void CheckIntConstantTest()
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntConstant(value);
            }
        }

        [Fact]
        public static void CheckLongConstantTest()
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongConstant(value);
            }
        }

        [Fact]
        public static void CheckObjectConstantTest()
        {
            foreach (object value in new object[] { null, new object(), new C(), new D(3) })
            {
                VerifyObjectConstant(value);
            }
        }

        [Fact]
        public static void CheckStructConstantTest()
        {
            foreach (S value in new S[] { default(S), new S() })
            {
                VerifyStructConstant(value);
            }
        }

        [Fact]
        public static void CheckSByteConstantTest()
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteConstant(value);
            }
        }

        [Fact]
        public static void CheckStructWithStringConstantTest()
        {
            foreach (Sc value in new Sc[] { default(Sc), new Sc(), new Sc(null) })
            {
                VerifyStructWithStringConstant(value);
            }
        }

        [Fact]
        public static void CheckStructWithStringAndFieldConstantTest()
        {
            foreach (Scs value in new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) })
            {
                VerifyStructWithStringAndFieldConstant(value);
            }
        }

        [Fact]
        public static void CheckShortConstantTest()
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortConstant(value);
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesConstantTest()
        {
            foreach (Sp value in new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) })
            {
                VerifyStructWithTwoValuesConstant(value);
            }
        }

        [Fact]
        public static void CheckStructWithValueConstantTest()
        {
            foreach (Ss value in new Ss[] { default(Ss), new Ss(), new Ss(new S()) })
            {
                VerifyStructWithValueConstant(value);
            }
        }

        [Fact]
        public static void CheckStringConstantTest()
        {
            foreach (string value in new string[] { null, "", "a", "foo" })
            {
                VerifyStringConstant(value);
            }
        }

        [Fact]
        public static void CheckUIntConstantTest()
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntConstant(value);
            }
        }

        [Fact]
        public static void CheckULongConstantTest()
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongConstant(value);
            }
        }

        [Fact]
        public static void CheckUShortConstantTest()
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortConstant(value);
            }
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionWithEnumConstantTest()
        {
            CheckGenericWithStructRestrictionConstantHelper<E>();
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionWithStructConstantTest()
        {
            CheckGenericWithStructRestrictionConstantHelper<S>();
        }

        [Fact]
        public static void CheckGenericWithStructRestrictionWithStructWithStringAndValueConstantTest()
        {
            CheckGenericWithStructRestrictionConstantHelper<Scs>();
        }

        [Fact]
        public static void CheckGenericWithCustomTest()
        {
            CheckGenericHelper<C>();
        }

        [Fact]
        public static void CheckGenericWithEnumTest()
        {
            CheckGenericHelper<E>();
        }

        [Fact]
        public static void CheckGenericWithObjectTest()
        {
            CheckGenericHelper<object>();
        }

        [Fact]
        public static void CheckGenericWithStructTest()
        {
            CheckGenericHelper<S>();
        }

        [Fact]
        public static void CheckGenericWithStructWithStringAndValueTest()
        {
            CheckGenericHelper<Scs>();
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionWithCustomTest()
        {
            CheckGenericWithClassRestrictionHelper<C>();
        }

        [Fact]
        public static void CheckGenericWithClassRestrictionWithObjectTest()
        {
            CheckGenericWithClassRestrictionHelper<object>();
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionWithCustomTest()
        {
            CheckGenericWithClassAndNewRestrictionHelper<C>();
        }

        [Fact]
        public static void CheckGenericWithClassAndNewRestrictionWithObjectTest()
        {
            CheckGenericWithClassAndNewRestrictionHelper<object>();
        }

        [Fact]
        public static void CheckGenericWithSubClassRestrictionTest()
        {
            CheckGenericWithSubClassRestrictionHelper<C>();
        }

        [Fact]
        public static void CheckGenericWithSubClassAndNewRestrictionTest()
        {
            CheckGenericWithSubClassAndNewRestrictionHelper<C>();
        }

        #endregion

        #region Generic helpers

        public static void CheckGenericWithStructRestrictionConstantHelper<Ts>() where Ts : struct
        {
            foreach (Ts value in new Ts[] { default(Ts), new Ts() })
            {
                VerifyGenericWithStructRestriction<Ts>(value);
            }
        }

        public static void CheckGenericHelper<T>()
        {
            foreach (T value in new T[] { default(T) })
            {
                VerifyGeneric<T>(value);
            }
        }

        public static void CheckGenericWithClassRestrictionHelper<Tc>() where Tc : class
        {
            foreach (Tc value in new Tc[] { null, default(Tc) })
            {
                VerifyGenericWithClassRestriction<Tc>(value);
            }
        }

        public static void CheckGenericWithClassAndNewRestrictionHelper<Tcn>() where Tcn : class, new()
        {
            foreach (Tcn value in new Tcn[] { null, default(Tcn), new Tcn() })
            {
                VerifyGenericWithClassAndNewRestriction<Tcn>(value);
            }
        }

        public static void CheckGenericWithSubClassRestrictionHelper<TC>() where TC : C
        {
            foreach (TC value in new TC[] { null, default(TC), (TC)new C() })
            {
                VerifyGenericWithSubClassRestriction<TC>(value);
            }
        }

        public static void CheckGenericWithSubClassAndNewRestrictionHelper<TCn>() where TCn : C, new()
        {
            foreach (TCn value in new TCn[] { null, default(TCn), new TCn(), (TCn)new C() })
            {
                VerifyGenericWithSubClassAndNewRestriction<TCn>(value);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBoolConstant(bool value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Constant(value, typeof(bool)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyByteConstant(byte value)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Constant(value, typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyCustomConstant(C value)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Constant(value, typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyCharConstant(char value)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Constant(value, typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyCustom2Constant(D value)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Constant(value, typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyDecimalConstant(decimal value)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Constant(value, typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyDelegateConstant(Delegate value)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.Constant(value, typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyDoubleConstant(double value)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Constant(value, typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyEnumConstant(E value)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Constant(value, typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyEnumLongConstant(El value)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Constant(value, typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyFloatConstant(float value)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Constant(value, typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyFuncOfObjectConstant(Func<object> value)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.Constant(value, typeof(Func<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyInterfaceConstant(I value)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Constant(value, typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyIEquatableOfCustomConstant(IEquatable<C> value)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Constant(value, typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyIEquatableOfCustom2Constant(IEquatable<D> value)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Constant(value, typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyIntConstant(int value)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Constant(value, typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyLongConstant(long value)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Constant(value, typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyObjectConstant(object value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Constant(value, typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyStructConstant(S value)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Constant(value, typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifySByteConstant(sbyte value)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Constant(value, typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithStringConstant(Sc value)
        {
            Expression<Func<Sc>> e =
                Expression.Lambda<Func<Sc>>(
                    Expression.Constant(value, typeof(Sc)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithStringAndFieldConstant(Scs value)
        {
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.Constant(value, typeof(Scs)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyShortConstant(short value)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Constant(value, typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithTwoValuesConstant(Sp value)
        {
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.Constant(value, typeof(Sp)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithValueConstant(Ss value)
        {
            Expression<Func<Ss>> e =
                Expression.Lambda<Func<Ss>>(
                    Expression.Constant(value, typeof(Ss)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyStringConstant(string value)
        {
            Expression<Func<string>> e =
                Expression.Lambda<Func<string>>(
                    Expression.Constant(value, typeof(string)),
                    Enumerable.Empty<ParameterExpression>());
            Func<string> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyUIntConstant(uint value)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Constant(value, typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyULongConstant(ulong value)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Constant(value, typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyUShortConstant(ushort value)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Constant(value, typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithStructRestriction<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.Constant(value, typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyGeneric<T>(T value)
        {
            Expression<Func<T>> e =
                Expression.Lambda<Func<T>>(
                    Expression.Constant(value, typeof(T)),
                    Enumerable.Empty<ParameterExpression>());
            Func<T> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithClassRestriction<Tc>(Tc value) where Tc : class
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.Constant(value, typeof(Tc)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithClassAndNewRestriction<Tcn>(Tcn value) where Tcn : class, new()
        {
            Expression<Func<Tcn>> e =
                Expression.Lambda<Func<Tcn>>(
                    Expression.Constant(value, typeof(Tcn)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithSubClassRestriction<TC>(TC value) where TC : C
        {
            Expression<Func<TC>> e =
                Expression.Lambda<Func<TC>>(
                    Expression.Constant(value, typeof(TC)),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC> f = e.Compile();
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithSubClassAndNewRestriction<TCn>(TCn value) where TCn : C, new()
        {
            Expression<Func<TCn>> e =
                Expression.Lambda<Func<TCn>>(
                    Expression.Constant(value, typeof(TCn)),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn> f = e.Compile();
            Assert.Equal(value, f());
        }

        #endregion
    }
}
