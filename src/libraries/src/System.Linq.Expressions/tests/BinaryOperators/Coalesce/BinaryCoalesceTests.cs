// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryCoalesceTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolCoalesceTest(bool useInterpreter)
        {
            bool?[] array1 = new bool?[] { null, true, false };
            bool[] array2 = new bool[] { true, false };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyBoolCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteCoalesceTest(bool useInterpreter)
        {
            byte?[] array1 = new byte?[] { null, 0, 1, byte.MaxValue };
            byte[] array2 = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyByteCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomCoalesceTest(bool useInterpreter)
        {
            C[] array1 = new C[] { null, new C(), new D(), new D(0), new D(5) };
            C[] array2 = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyCustomCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharCoalesceTest(bool useInterpreter)
        {
            char?[] array1 = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            char[] array2 = new char[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyCharCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2CoalesceTest(bool useInterpreter)
        {
            D[] array1 = new D[] { null, new D(), new D(0), new D(5) };
            D[] array2 = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyCustom2Coalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalCoalesceTest(bool useInterpreter)
        {
            decimal?[] array1 = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            decimal[] array2 = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyDecimalCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateCoalesceTest(bool useInterpreter)
        {
            Delegate[] array1 = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            Delegate[] array2 = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyDelegateCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleCoalesceTest(bool useInterpreter)
        {
            double?[] array1 = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            double[] array2 = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyDoubleCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumCoalesceTest(bool useInterpreter)
        {
            E?[] array1 = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            E[] array2 = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyEnumCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumLongCoalesceTest(bool useInterpreter)
        {
            El?[] array1 = new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            El[] array2 = new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyEnumLongCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatCoalesceTest(bool useInterpreter)
        {
            float?[] array1 = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            float[] array2 = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyFloatCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncCoalesceTest(bool useInterpreter)
        {
            Func<object>[] array1 = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            Func<object>[] array2 = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyFuncCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceCoalesceTest(bool useInterpreter)
        {
            I[] array1 = new I[] { null, new C(), new D(), new D(0), new D(5) };
            I[] array2 = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyInterfaceCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustomCoalesceTest(bool useInterpreter)
        {
            IEquatable<C>[] array1 = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            IEquatable<C>[] array2 = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyIEquatableCustomCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableCustom2CoalesceTest(bool useInterpreter)
        {
            IEquatable<D>[] array1 = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            IEquatable<D>[] array2 = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyIEquatableCustom2Coalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntCoalesceTest(bool useInterpreter)
        {
            int?[] array1 = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            int[] array2 = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyIntCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongCoalesceTest(bool useInterpreter)
        {
            long?[] array1 = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            long[] array2 = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyLongCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectCoalesceTest(bool useInterpreter)
        {
            object[] array1 = new object[] { null, new object(), new C(), new D(3) };
            object[] array2 = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyObjectCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructCoalesceTest(bool useInterpreter)
        {
            S?[] array1 = new S?[] { null, default(S), new S() };
            S[] array2 = new S[] { default(S), new S() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStructCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteCoalesceTest(bool useInterpreter)
        {
            sbyte?[] array1 = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            sbyte[] array2 = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifySByteCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringCoalesceTest(bool useInterpreter)
        {
            Sc?[] array1 = new Sc?[] { null, default(Sc), new Sc(), new Sc(null) };
            Sc[] array2 = new Sc[] { default(Sc), new Sc(), new Sc(null) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStructWithStringCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndFieldCoalesceTest(bool useInterpreter)
        {
            Scs?[] array1 = new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) };
            Scs[] array2 = new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStructWithStringAndFieldCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortCoalesceTest(bool useInterpreter)
        {
            short?[] array1 = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            short[] array2 = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyShortCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesCoalesceTest(bool useInterpreter)
        {
            Sp?[] array1 = new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) };
            Sp[] array2 = new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStructWithTwoValuesCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueCoalesceTest(bool useInterpreter)
        {
            Ss?[] array1 = new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) };
            Ss[] array2 = new Ss[] { default(Ss), new Ss(), new Ss(new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStructWithValueCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringCoalesceTest(bool useInterpreter)
        {
            string[] array1 = new string[] { null, "", "a", "foo" };
            string[] array2 = new string[] { null, "", "a", "foo" };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStringCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntCoalesceTest(bool useInterpreter)
        {
            uint?[] array1 = new uint?[] { null, 0, 1, uint.MaxValue };
            uint[] array2 = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyUIntCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongCoalesceTest(bool useInterpreter)
        {
            ulong?[] array1 = new ulong?[] { null, 0, 1, ulong.MaxValue };
            ulong[] array2 = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyULongCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortCoalesceTest(bool useInterpreter)
        {
            ushort?[] array1 = new ushort?[] { null, 0, 1, ushort.MaxValue };
            ushort[] array2 = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyUShortCoalesce(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithClassRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionCoalesceHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectWithClassRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionCoalesceHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithSubClassRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithSubClassRestrictionCoalesceHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithClassAndNewRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionCoalesceHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericObjectWithClassAndNewRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionCoalesceHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericCustomWithSubClassAndNewRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithSubClassAndNewRestrictionCoalesceHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericEnumWithStructRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCoalesceHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStructRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCoalesceHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStringAndFieldWithStructRestrictionCoalesceTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCoalesceHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckGenericWithClassRestrictionCoalesceHelper<Tc>(bool useInterpreter) where Tc : class
        {
            Tc[] array1 = new Tc[] { null, default(Tc) };
            Tc[] array2 = new Tc[] { null, default(Tc) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithClassRestrictionCoalesce<Tc>(array1[i], array2[j], useInterpreter);
                }
            }
        }

        private static void CheckGenericWithSubClassRestrictionCoalesceHelper<TC>(bool useInterpreter) where TC : C
        {
            TC[] array1 = new TC[] { null, default(TC), (TC)new C() };
            TC[] array2 = new TC[] { null, default(TC), (TC)new C() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithSubClassRestrictionCoalesce<TC>(array1[i], array2[j], useInterpreter);
                }
            }
        }

        private static void CheckGenericWithClassAndNewRestrictionCoalesceHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            Tcn[] array1 = new Tcn[] { null, default(Tcn), new Tcn() };
            Tcn[] array2 = new Tcn[] { null, default(Tcn), new Tcn() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithClassAndNewRestrictionCoalesce<Tcn>(array1[i], array2[j], useInterpreter);
                }
            }
        }

        private static void CheckGenericWithSubClassAndNewRestrictionCoalesceHelper<TCn>(bool useInterpreter) where TCn : C, new()
        {
            TCn[] array1 = new TCn[] { null, default(TCn), new TCn(), (TCn)new C() };
            TCn[] array2 = new TCn[] { null, default(TCn), new TCn(), (TCn)new C() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithSubClassAndNewRestrictionCoalesce<TCn>(array1[i], array2[j], useInterpreter);
                }
            }
        }

        private static void CheckGenericWithStructRestrictionCoalesceHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts?[] array1 = new Ts?[] { null, default(Ts), new Ts() };
            Ts[] array2 = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithStructRestrictionCoalesce<Ts>(array1[i], array2[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBoolCoalesce(bool? a, bool b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(bool?)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyByteCoalesce(byte? a, byte b, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyCustomCoalesce(C a, C b, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(C)),
                        Expression.Constant(b, typeof(C))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyCharCoalesce(char? a, char b, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyCustom2Coalesce(D a, D b, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(D)),
                        Expression.Constant(b, typeof(D))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyDecimalCoalesce(decimal? a, decimal b, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyDelegateCoalesce(Delegate a, Delegate b, bool useInterpreter)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Delegate)),
                        Expression.Constant(b, typeof(Delegate))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyDoubleCoalesce(double? a, double b, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyEnumCoalesce(E? a, E b, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(E?)),
                        Expression.Constant(b, typeof(E))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyEnumLongCoalesce(El? a, El b, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(El?)),
                        Expression.Constant(b, typeof(El))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyFloatCoalesce(float? a, float b, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyFuncCoalesce(Func<object> a, Func<object> b, bool useInterpreter)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Func<object>)),
                        Expression.Constant(b, typeof(Func<object>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyInterfaceCoalesce(I a, I b, bool useInterpreter)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(I)),
                        Expression.Constant(b, typeof(I))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyIEquatableCustomCoalesce(IEquatable<C> a, IEquatable<C> b, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(IEquatable<C>)),
                        Expression.Constant(b, typeof(IEquatable<C>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyIEquatableCustom2Coalesce(IEquatable<D> a, IEquatable<D> b, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(IEquatable<D>)),
                        Expression.Constant(b, typeof(IEquatable<D>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyIntCoalesce(int? a, int b, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyLongCoalesce(long? a, long b, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyObjectCoalesce(object a, object b, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(object)),
                        Expression.Constant(b, typeof(object))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyStructCoalesce(S? a, S b, bool useInterpreter)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(S?)),
                        Expression.Constant(b, typeof(S))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifySByteCoalesce(sbyte? a, sbyte b, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyStructWithStringCoalesce(Sc? a, Sc b, bool useInterpreter)
        {
            Expression<Func<Sc>> e =
                Expression.Lambda<Func<Sc>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Sc?)),
                        Expression.Constant(b, typeof(Sc))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyStructWithStringAndFieldCoalesce(Scs? a, Scs b, bool useInterpreter)
        {
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Scs?)),
                        Expression.Constant(b, typeof(Scs))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyShortCoalesce(short? a, short b, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyStructWithTwoValuesCoalesce(Sp? a, Sp b, bool useInterpreter)
        {
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Sp?)),
                        Expression.Constant(b, typeof(Sp))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyStructWithValueCoalesce(Ss? a, Ss b, bool useInterpreter)
        {
            Expression<Func<Ss>> e =
                Expression.Lambda<Func<Ss>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Ss?)),
                        Expression.Constant(b, typeof(Ss))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyStringCoalesce(string a, string b, bool useInterpreter)
        {
            Expression<Func<string>> e =
                Expression.Lambda<Func<string>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(string)),
                        Expression.Constant(b, typeof(string))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyUIntCoalesce(uint? a, uint b, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyULongCoalesce(ulong? a, ulong b, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyUShortCoalesce(ushort? a, ushort b, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyGenericWithClassRestrictionCoalesce<Tc>(Tc a, Tc b, bool useInterpreter) where Tc : class
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Tc)),
                        Expression.Constant(b, typeof(Tc))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyGenericWithSubClassRestrictionCoalesce<TC>(TC a, TC b, bool useInterpreter) where TC : C
        {
            Expression<Func<TC>> e =
                Expression.Lambda<Func<TC>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(TC)),
                        Expression.Constant(b, typeof(TC))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyGenericWithClassAndNewRestrictionCoalesce<Tcn>(Tcn a, Tcn b, bool useInterpreter) where Tcn : class, new()
        {
            Expression<Func<Tcn>> e =
                Expression.Lambda<Func<Tcn>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Tcn)),
                        Expression.Constant(b, typeof(Tcn))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionCoalesce<TCn>(TCn a, TCn b, bool useInterpreter) where TCn : C, new()
        {
            Expression<Func<TCn>> e =
                Expression.Lambda<Func<TCn>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(TCn)),
                        Expression.Constant(b, typeof(TCn))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        private static void VerifyGenericWithStructRestrictionCoalesce<Ts>(Ts? a, Ts b, bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Ts?)),
                        Expression.Constant(b, typeof(Ts))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile(useInterpreter);

            Assert.Equal(a ?? b, f());
        }

        #endregion

        [Fact]
        public static void BasicCoalesceExpressionTest()
        {
            int? i = 0;
            double? d = 0;
            var left = Expression.Constant(d, typeof(double?));
            var right = Expression.Constant(i, typeof(int?));
            Expression<Func<double?, int?>> conversion = x => 1 + (int?)x;

            BinaryExpression actual = Expression.Coalesce(left, right, conversion);

            Assert.Equal(conversion, actual.Conversion);
            Assert.Equal(actual.Right.Type, actual.Type);
            Assert.Equal(ExpressionType.Coalesce, actual.NodeType);

            // Compile and evaluate with interpretation flag and without
            // in case there are bugs in the compiler/interpreter. 
            Assert.Equal(2, conversion.Compile(false).Invoke(1.1));
            Assert.Equal(2, conversion.Compile(true).Invoke(1.1));
        }

        [Fact]
        public static void CannotReduce()
        {
            Expression exp = Expression.Coalesce(Expression.Constant(0, typeof(int?)), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void ThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.Coalesce(null, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.Coalesce(Expression.Constant(""), null));
        }

        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        [Fact]
        public static void ThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<string>), "WriteOnly");
            Assert.Throws<ArgumentException>("left", () => Expression.Coalesce(value, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<string>), "WriteOnly");
            Assert.Throws<ArgumentException>("right", () => Expression.Coalesce(Expression.Constant(""), value));
        }
    }
}
