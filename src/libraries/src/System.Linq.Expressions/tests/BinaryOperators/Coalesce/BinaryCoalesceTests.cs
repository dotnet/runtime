// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Binary
{
    public static unsafe class BinaryCoalesceTests
    {
        #region Test methods

        [Fact]
        public static void CheckBoolCoalesceTest()
        {
            bool?[] array1 = new bool?[] { null, true, false };
            bool[] array2 = new bool[] { true, false };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyBoolCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckByteCoalesceTest()
        {
            byte?[] array1 = new byte?[] { null, 0, 1, byte.MaxValue };
            byte[] array2 = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyByteCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckCustomCoalesceTest()
        {
            C[] array1 = new C[] { null, new C(), new D(), new D(0), new D(5) };
            C[] array2 = new C[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyCustomCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckCharCoalesceTest()
        {
            char?[] array1 = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            char[] array2 = new char[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyCharCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckCustom2CoalesceTest()
        {
            D[] array1 = new D[] { null, new D(), new D(0), new D(5) };
            D[] array2 = new D[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyCustom2Coalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckDecimalCoalesceTest()
        {
            decimal?[] array1 = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            decimal[] array2 = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyDecimalCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckDelegateCoalesceTest()
        {
            Delegate[] array1 = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            Delegate[] array2 = new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyDelegateCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckDoubleCoalesceTest()
        {
            double?[] array1 = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            double[] array2 = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyDoubleCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckEnumCoalesceTest()
        {
            E?[] array1 = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            E[] array2 = new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyEnumCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckEnumLongCoalesceTest()
        {
            El?[] array1 = new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            El[] array2 = new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyEnumLongCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckFloatCoalesceTest()
        {
            float?[] array1 = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            float[] array2 = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyFloatCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckFuncCoalesceTest()
        {
            Func<object>[] array1 = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            Func<object>[] array2 = new Func<object>[] { null, (Func<object>)delegate () { return null; } };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyFuncCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckInterfaceCoalesceTest()
        {
            I[] array1 = new I[] { null, new C(), new D(), new D(0), new D(5) };
            I[] array2 = new I[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyInterfaceCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckIEquatableCustomCoalesceTest()
        {
            IEquatable<C>[] array1 = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            IEquatable<C>[] array2 = new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyIEquatableCustomCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckIEquatableCustom2CoalesceTest()
        {
            IEquatable<D>[] array1 = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            IEquatable<D>[] array2 = new IEquatable<D>[] { null, new D(), new D(0), new D(5) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyIEquatableCustom2Coalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckIntCoalesceTest()
        {
            int?[] array1 = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            int[] array2 = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyIntCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckLongCoalesceTest()
        {
            long?[] array1 = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            long[] array2 = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyLongCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckObjectCoalesceTest()
        {
            object[] array1 = new object[] { null, new object(), new C(), new D(3) };
            object[] array2 = new object[] { null, new object(), new C(), new D(3) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyObjectCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckStructCoalesceTest()
        {
            S?[] array1 = new S?[] { null, default(S), new S() };
            S[] array2 = new S[] { default(S), new S() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStructCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckSByteCoalesceTest()
        {
            sbyte?[] array1 = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            sbyte[] array2 = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifySByteCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckStructWithStringCoalesceTest()
        {
            Sc?[] array1 = new Sc?[] { null, default(Sc), new Sc(), new Sc(null) };
            Sc[] array2 = new Sc[] { default(Sc), new Sc(), new Sc(null) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStructWithStringCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckStructWithStringAndFieldCoalesceTest()
        {
            Scs?[] array1 = new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) };
            Scs[] array2 = new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStructWithStringAndFieldCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckShortCoalesceTest()
        {
            short?[] array1 = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            short[] array2 = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyShortCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckStructWithTwoValuesCoalesceTest()
        {
            Sp?[] array1 = new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) };
            Sp[] array2 = new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStructWithTwoValuesCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckStructWithValueCoalesceTest()
        {
            Ss?[] array1 = new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) };
            Ss[] array2 = new Ss[] { default(Ss), new Ss(), new Ss(new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStructWithValueCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckStringCoalesceTest()
        {
            string[] array1 = new string[] { null, "", "a", "foo" };
            string[] array2 = new string[] { null, "", "a", "foo" };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyStringCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckUIntCoalesceTest()
        {
            uint?[] array1 = new uint?[] { null, 0, 1, uint.MaxValue };
            uint[] array2 = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyUIntCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckULongCoalesceTest()
        {
            ulong?[] array1 = new ulong?[] { null, 0, 1, ulong.MaxValue };
            ulong[] array2 = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyULongCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckUShortCoalesceTest()
        {
            ushort?[] array1 = new ushort?[] { null, 0, 1, ushort.MaxValue };
            ushort[] array2 = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyUShortCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckGenericCustomWithClassRestrictionCoalesceTest()
        {
            CheckGenericWithClassRestrictionCoalesceHelper<C>();
        }

        [Fact]
        public static void CheckGenericObjectWithClassRestrictionCoalesceTest()
        {
            CheckGenericWithClassRestrictionCoalesceHelper<object>();
        }

        [Fact]
        public static void CheckGenericCustomWithSubClassRestrictionCoalesceTest()
        {
            CheckGenericWithSubClassRestrictionCoalesceHelper<C>();
        }

        [Fact]
        public static void CheckGenericCustomWithClassAndNewRestrictionCoalesceTest()
        {
            CheckGenericWithClassAndNewRestrictionCoalesceHelper<C>();
        }

        [Fact]
        public static void CheckGenericObjectWithClassAndNewRestrictionCoalesceTest()
        {
            CheckGenericWithClassAndNewRestrictionCoalesceHelper<object>();
        }

        [Fact]
        public static void CheckGenericCustomWithSubClassAndNewRestrictionCoalesceTest()
        {
            CheckGenericWithSubClassAndNewRestrictionCoalesceHelper<C>();
        }

        [Fact]
        public static void CheckGenericEnumWithStructRestrictionCoalesceTest()
        {
            CheckGenericWithStructRestrictionCoalesceHelper<E>();
        }

        [Fact]
        public static void CheckGenericStructWithStructRestrictionCoalesceTest()
        {
            CheckGenericWithStructRestrictionCoalesceHelper<S>();
        }

        [Fact]
        public static void CheckGenericStructWithStringAndFieldWithStructRestrictionCoalesceTest()
        {
            CheckGenericWithStructRestrictionCoalesceHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckGenericWithClassRestrictionCoalesceHelper<Tc>() where Tc : class
        {
            Tc[] array1 = new Tc[] { null, default(Tc) };
            Tc[] array2 = new Tc[] { null, default(Tc) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithClassRestrictionCoalesce<Tc>(array1[i], array2[j]);
                }
            }
        }

        private static void CheckGenericWithSubClassRestrictionCoalesceHelper<TC>() where TC : C
        {
            TC[] array1 = new TC[] { null, default(TC), (TC)new C() };
            TC[] array2 = new TC[] { null, default(TC), (TC)new C() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithSubClassRestrictionCoalesce<TC>(array1[i], array2[j]);
                }
            }
        }

        private static void CheckGenericWithClassAndNewRestrictionCoalesceHelper<Tcn>() where Tcn : class, new()
        {
            Tcn[] array1 = new Tcn[] { null, default(Tcn), new Tcn() };
            Tcn[] array2 = new Tcn[] { null, default(Tcn), new Tcn() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithClassAndNewRestrictionCoalesce<Tcn>(array1[i], array2[j]);
                }
            }
        }

        private static void CheckGenericWithSubClassAndNewRestrictionCoalesceHelper<TCn>() where TCn : C, new()
        {
            TCn[] array1 = new TCn[] { null, default(TCn), new TCn(), (TCn)new C() };
            TCn[] array2 = new TCn[] { null, default(TCn), new TCn(), (TCn)new C() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithSubClassAndNewRestrictionCoalesce<TCn>(array1[i], array2[j]);
                }
            }
        }

        private static void CheckGenericWithStructRestrictionCoalesceHelper<Ts>() where Ts : struct
        {
            Ts?[] array1 = new Ts?[] { null, default(Ts), new Ts() };
            Ts[] array2 = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithStructRestrictionCoalesce<Ts>(array1[i], array2[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBoolCoalesce(bool? a, bool b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(bool?)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyByteCoalesce(byte? a, byte b)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile();

            // compute with expression tree
            byte etResult = default(byte);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            byte csResult = default(byte);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustomCoalesce(C a, C b)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(C)),
                        Expression.Constant(b, typeof(C))),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile();

            // compute with expression tree
            C etResult = default(C);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            C csResult = default(C);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCharCoalesce(char? a, char b)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile();

            // compute with expression tree
            char etResult = default(char);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            char csResult = default(char);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyCustom2Coalesce(D a, D b)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(D)),
                        Expression.Constant(b, typeof(D))),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile();

            // compute with expression tree
            D etResult = default(D);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            D csResult = default(D);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyDecimalCoalesce(decimal? a, decimal b)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile();

            // compute with expression tree
            decimal etResult = default(decimal);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            decimal csResult = default(decimal);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyDelegateCoalesce(Delegate a, Delegate b)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Delegate)),
                        Expression.Constant(b, typeof(Delegate))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile();

            // compute with expression tree
            Delegate etResult = default(Delegate);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            Delegate csResult = default(Delegate);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyDoubleCoalesce(double? a, double b)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile();

            // compute with expression tree
            double etResult = default(double);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            double csResult = default(double);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyEnumCoalesce(E? a, E b)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(E?)),
                        Expression.Constant(b, typeof(E))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile();

            // compute with expression tree
            E etResult = default(E);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            E csResult = default(E);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyEnumLongCoalesce(El? a, El b)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(El?)),
                        Expression.Constant(b, typeof(El))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile();

            // compute with expression tree
            El etResult = default(El);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            El csResult = default(El);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyFloatCoalesce(float? a, float b)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile();

            // compute with expression tree
            float etResult = default(float);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            float csResult = default(float);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyFuncCoalesce(Func<object> a, Func<object> b)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Func<object>)),
                        Expression.Constant(b, typeof(Func<object>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>> f = e.Compile();

            // compute with expression tree
            Func<object> etResult = default(Func<object>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            Func<object> csResult = default(Func<object>);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyInterfaceCoalesce(I a, I b)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(I)),
                        Expression.Constant(b, typeof(I))),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile();

            // compute with expression tree
            I etResult = default(I);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            I csResult = default(I);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEquatableCustomCoalesce(IEquatable<C> a, IEquatable<C> b)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(IEquatable<C>)),
                        Expression.Constant(b, typeof(IEquatable<C>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile();

            // compute with expression tree
            IEquatable<C> etResult = default(IEquatable<C>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            IEquatable<C> csResult = default(IEquatable<C>);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIEquatableCustom2Coalesce(IEquatable<D> a, IEquatable<D> b)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(IEquatable<D>)),
                        Expression.Constant(b, typeof(IEquatable<D>))),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile();

            // compute with expression tree
            IEquatable<D> etResult = default(IEquatable<D>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            IEquatable<D> csResult = default(IEquatable<D>);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyIntCoalesce(int? a, int b)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();

            // compute with expression tree
            int etResult = default(int);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            int csResult = default(int);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyLongCoalesce(long? a, long b)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile();

            // compute with expression tree
            long etResult = default(long);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            long csResult = default(long);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyObjectCoalesce(object a, object b)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(object)),
                        Expression.Constant(b, typeof(object))),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute with expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyStructCoalesce(S? a, S b)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(S?)),
                        Expression.Constant(b, typeof(S))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile();

            // compute with expression tree
            S etResult = default(S);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            S csResult = default(S);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifySByteCoalesce(sbyte? a, sbyte b)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile();

            // compute with expression tree
            sbyte etResult = default(sbyte);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            sbyte csResult = default(sbyte);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyStructWithStringCoalesce(Sc? a, Sc b)
        {
            Expression<Func<Sc>> e =
                Expression.Lambda<Func<Sc>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Sc?)),
                        Expression.Constant(b, typeof(Sc))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc> f = e.Compile();

            // compute with expression tree
            Sc etResult = default(Sc);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            Sc csResult = default(Sc);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyStructWithStringAndFieldCoalesce(Scs? a, Scs b)
        {
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Scs?)),
                        Expression.Constant(b, typeof(Scs))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile();

            // compute with expression tree
            Scs etResult = default(Scs);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            Scs csResult = default(Scs);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyShortCoalesce(short? a, short b)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile();

            // compute with expression tree
            short etResult = default(short);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            short csResult = default(short);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyStructWithTwoValuesCoalesce(Sp? a, Sp b)
        {
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Sp?)),
                        Expression.Constant(b, typeof(Sp))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile();

            // compute with expression tree
            Sp etResult = default(Sp);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            Sp csResult = default(Sp);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyStructWithValueCoalesce(Ss? a, Ss b)
        {
            Expression<Func<Ss>> e =
                Expression.Lambda<Func<Ss>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Ss?)),
                        Expression.Constant(b, typeof(Ss))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss> f = e.Compile();

            // compute with expression tree
            Ss etResult = default(Ss);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            Ss csResult = default(Ss);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyStringCoalesce(string a, string b)
        {
            Expression<Func<string>> e =
                Expression.Lambda<Func<string>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(string)),
                        Expression.Constant(b, typeof(string))),
                    Enumerable.Empty<ParameterExpression>());
            Func<string> f = e.Compile();

            // compute with expression tree
            string etResult = default(string);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            string csResult = default(string);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyUIntCoalesce(uint? a, uint b)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile();

            // compute with expression tree
            uint etResult = default(uint);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            uint csResult = default(uint);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyULongCoalesce(ulong? a, ulong b)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile();

            // compute with expression tree
            ulong etResult = default(ulong);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            ulong csResult = default(ulong);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyUShortCoalesce(ushort? a, ushort b)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile();

            // compute with expression tree
            ushort etResult = default(ushort);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            ushort csResult = default(ushort);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyGenericWithClassRestrictionCoalesce<Tc>(Tc a, Tc b) where Tc : class
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Tc)),
                        Expression.Constant(b, typeof(Tc))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile();

            // compute with expression tree
            Tc etResult = default(Tc);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            Tc csResult = default(Tc);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyGenericWithSubClassRestrictionCoalesce<TC>(TC a, TC b) where TC : C
        {
            Expression<Func<TC>> e =
                Expression.Lambda<Func<TC>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(TC)),
                        Expression.Constant(b, typeof(TC))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC> f = e.Compile();

            // compute with expression tree
            TC etResult = default(TC);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            TC csResult = default(TC);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyGenericWithClassAndNewRestrictionCoalesce<Tcn>(Tcn a, Tcn b) where Tcn : class, new()
        {
            Expression<Func<Tcn>> e =
                Expression.Lambda<Func<Tcn>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Tcn)),
                        Expression.Constant(b, typeof(Tcn))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn> f = e.Compile();

            // compute with expression tree
            Tcn etResult = default(Tcn);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            Tcn csResult = default(Tcn);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyGenericWithSubClassAndNewRestrictionCoalesce<TCn>(TCn a, TCn b) where TCn : C, new()
        {
            Expression<Func<TCn>> e =
                Expression.Lambda<Func<TCn>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(TCn)),
                        Expression.Constant(b, typeof(TCn))),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn> f = e.Compile();

            // compute with expression tree
            TCn etResult = default(TCn);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            TCn csResult = default(TCn);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyGenericWithStructRestrictionCoalesce<Ts>(Ts? a, Ts b) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Ts?)),
                        Expression.Constant(b, typeof(Ts))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile();

            // compute with expression tree
            Ts etResult = default(Ts);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            Ts csResult = default(Ts);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        #endregion
    }
}
