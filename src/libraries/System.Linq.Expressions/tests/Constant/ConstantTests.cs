// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ConstantTests
    {

        private class PrivateGenericClass<T>
        {
        }

#region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolConstantTest(bool useInterpreter)
        {
            foreach (bool value in new bool[] { true, false })
            {
                VerifyBoolConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteConstantTest(bool useInterpreter)
        {
            foreach (byte value in new byte[] { 0, 1, byte.MaxValue })
            {
                VerifyByteConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustomConstantTest(bool useInterpreter)
        {
            foreach (C value in new C[] { null, new C(), new D(), new D(0), new D(5) })
            {
                VerifyCustomConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharConstantTest(bool useInterpreter)
        {
            foreach (char value in new char[] { '\0', '\b', 'A', '\uffff' })
            {
                VerifyCharConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCustom2ConstantTest(bool useInterpreter)
        {
            foreach (D value in new D[] { null, new D(), new D(0), new D(5) })
            {
                VerifyCustom2Constant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalConstantTest(bool useInterpreter)
        {
            foreach (decimal value in new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue, int.MinValue, int.MaxValue, int.MinValue - 1L, int.MaxValue + 1L, long.MinValue, long.MaxValue, long.MaxValue + 1m, ulong.MaxValue, ulong.MaxValue + 1m })
            {
                VerifyDecimalConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDelegateConstantTest(bool useInterpreter)
        {
            foreach (Delegate value in new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } })
            {
                VerifyDelegateConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleConstantTest(bool useInterpreter)
        {
            foreach (double value in new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN })
            {
                VerifyDoubleConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumConstantTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyEnumConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumLongConstantTest(bool useInterpreter)
        {
            foreach (El value in new El[] { (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue })
            {
                VerifyEnumLongConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatConstantTest(bool useInterpreter)
        {
            foreach (float value in new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN })
            {
                VerifyFloatConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFuncOfObjectConstantTest(bool useInterpreter)
        {
            foreach (Func<object> value in new Func<object>[] { null, (Func<object>)delegate () { return null; } })
            {
                VerifyFuncOfObjectConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckInterfaceConstantTest(bool useInterpreter)
        {
            foreach (I value in new I[] { null, new C(), new D(), new D(0), new D(5) })
            {
                VerifyInterfaceConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustomConstantTest(bool useInterpreter)
        {
            foreach (IEquatable<C> value in new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) })
            {
                VerifyIEquatableOfCustomConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIEquatableOfCustom2ConstantTest(bool useInterpreter)
        {
            foreach (IEquatable<D> value in new IEquatable<D>[] { null, new D(), new D(0), new D(5) })
            {
                VerifyIEquatableOfCustom2Constant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntConstantTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyIntConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongConstantTest(bool useInterpreter)
        {
            foreach (long value in new long[] { 0, 1, -1, long.MinValue, long.MaxValue })
            {
                VerifyLongConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckObjectConstantTest(bool useInterpreter)
        {
            foreach (object value in new object[] { null, new object(), new C(), new D(3) })
            {
                VerifyObjectConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructConstantTest(bool useInterpreter)
        {
            foreach (S value in new S[] { default(S), new S() })
            {
                VerifyStructConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteConstantTest(bool useInterpreter)
        {
            foreach (sbyte value in new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue })
            {
                VerifySByteConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringConstantTest(bool useInterpreter)
        {
            foreach (Sc value in new Sc[] { default(Sc), new Sc(), new Sc(null) })
            {
                VerifyStructWithStringConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithStringAndFieldConstantTest(bool useInterpreter)
        {
            foreach (Scs value in new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) })
            {
                VerifyStructWithStringAndFieldConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortConstantTest(bool useInterpreter)
        {
            foreach (short value in new short[] { 0, 1, -1, short.MinValue, short.MaxValue })
            {
                VerifyShortConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithTwoValuesConstantTest(bool useInterpreter)
        {
            foreach (Sp value in new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) })
            {
                VerifyStructWithTwoValuesConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStructWithValueConstantTest(bool useInterpreter)
        {
            foreach (Ss value in new Ss[] { default(Ss), new Ss(), new Ss(new S()) })
            {
                VerifyStructWithValueConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckStringConstantTest(bool useInterpreter)
        {
            foreach (string value in new string[] { null, "", "a", "foo" })
            {
                VerifyStringConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntConstantTest(bool useInterpreter)
        {
            foreach (uint value in new uint[] { 0, 1, uint.MaxValue })
            {
                VerifyUIntConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongConstantTest(bool useInterpreter)
        {
            foreach (ulong value in new ulong[] { 0, 1, ulong.MaxValue })
            {
                VerifyULongConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortConstantTest(bool useInterpreter)
        {
            foreach (ushort value in new ushort[] { 0, 1, ushort.MaxValue })
            {
                VerifyUShortConstant(value, useInterpreter);
            }
        }

#if FEATURE_COMPILE
        private static TypeBuilder GetTypeBuilder()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            return module.DefineType("Type");
        }
#endif

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckTypeConstantTest(bool useInterpreter)
        {
            foreach (Type value in new[]
            {
                null,
                typeof(int),
                typeof(Func<string>),
                typeof(List<>).GetGenericArguments()[0],
#if FEATURE_COMPILE
                GetTypeBuilder(),
#endif
                typeof(PrivateGenericClass<>).GetGenericArguments()[0],
                typeof(PrivateGenericClass<>),
                typeof(PrivateGenericClass<int>)
            })
            {
                VerifyTypeConstant(value, useInterpreter);
            }
        }

#if FEATURE_COMPILE

        private static MethodInfo GlobalMethod(params Type[] parameterTypes)
        {
            ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("Module");
            MethodBuilder globalMethod = module.DefineGlobalMethod("GlobalMethod", MethodAttributes.Public | MethodAttributes.Static, typeof(void), parameterTypes);
            globalMethod.GetILGenerator().Emit(OpCodes.Ret);
            module.CreateGlobalFunctions();
            return module.GetMethod(globalMethod.Name);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckMethodInfoConstantTest(bool useInterpreter)
        {
            foreach (MethodInfo value in new MethodInfo[]
            {
                null,
                typeof(SomePublicMethodsForLdToken).GetMethod(nameof(SomePublicMethodsForLdToken.Bar), BindingFlags.Public | BindingFlags.Static),
                typeof(SomePublicMethodsForLdToken).GetMethod(nameof(SomePublicMethodsForLdToken.Qux), BindingFlags.Public | BindingFlags.Static),
                typeof(SomePublicMethodsForLdToken).GetMethod(nameof(SomePublicMethodsForLdToken.Qux), BindingFlags.Public | BindingFlags.Static).MakeGenericMethod(typeof(int)),
                typeof(List<>).GetMethod(nameof(List<int>.Add)),
                typeof(List<int>).GetMethod(nameof(List<int>.Add)),
                GlobalMethod(Type.EmptyTypes),
                GlobalMethod(typeof(PrivateGenericClass<int>)),
                GlobalMethod(typeof(PrivateGenericClass<>))
            })
            {
                VerifyMethodInfoConstant(value, useInterpreter);
            }
        }
#endif

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckConstructorInfoConstantTest(bool useInterpreter)
        {
            foreach (
                ConstructorInfo value in
                typeof(SomePublicMethodsForLdToken).GetConstructors()
                    .Concat(typeof(string).GetConstructors())
                    .Concat(typeof(List<>).GetConstructors())
                    .Append(null))
            {
                VerifyConstructorInfoConstant(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionWithEnumConstantTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionConstantHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionWithStructConstantTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionConstantHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructRestrictionWithStructWithStringAndValueConstantTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionConstantHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithCustomTest(bool useInterpreter)
        {
            CheckGenericHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithEnumTest(bool useInterpreter)
        {
            CheckGenericHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithObjectTest(bool useInterpreter)
        {
            CheckGenericHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructTest(bool useInterpreter)
        {
            CheckGenericHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithStructWithStringAndValueTest(bool useInterpreter)
        {
            CheckGenericHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionWithCustomTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassRestrictionWithObjectTest(bool useInterpreter)
        {
            CheckGenericWithClassRestrictionHelper<object>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionWithCustomTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionHelper<C>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericWithClassAndNewRestrictionWithObjectTest(bool useInterpreter)
        {
            CheckGenericWithClassAndNewRestrictionHelper<object>(useInterpreter);
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

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void BoundConstantCaching1(bool useInterpreter)
        {
            ConstantExpression c = Expression.Constant(new Bar());

            BinaryExpression e =
                Expression.Add(
                    Expression.Field(c, "Foo"),
                    Expression.Subtract(
                        Expression.Field(c, "Baz"),
                        Expression.Field(c, "Qux")
                    )
                );

            Assert.Equal(42, Expression.Lambda<Func<int>>(e).Compile(useInterpreter)());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void BoundConstantCaching2(bool useInterpreter)
        {
            var b = new Bar();
            ConstantExpression c1 = Expression.Constant(b);
            ConstantExpression c2 = Expression.Constant(b);
            ConstantExpression c3 = Expression.Constant(b);

            BinaryExpression e =
                Expression.Add(
                    Expression.Field(c1, "Foo"),
                    Expression.Subtract(
                        Expression.Field(c2, "Baz"),
                        Expression.Field(c3, "Qux")
                    )
                );

            Assert.Equal(42, Expression.Lambda<Func<int>>(e).Compile(useInterpreter)());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void BoundConstantCaching3(bool useInterpreter)
        {
            var b = new Bar() { Foo = 1 };

            for (var i = 1; i <= 10; i++)
            {
                var e = (Expression)Expression.Constant(0);

                for (var j = 1; j <= i; j++)
                {
                    e = Expression.Add(e, Expression.Field(Expression.Constant(b), "Foo"));
                }

                Assert.Equal(i, Expression.Lambda<Func<int>>(e).Compile(useInterpreter)());
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void BoundConstantCaching4(bool useInterpreter)
        {
            Bar[] bs = new[]
            {
                new Bar() { Foo = 1 },
                new Bar() { Foo = 1 },
            };

            for (var i = 1; i <= 10; i++)
            {
                var e = (Expression)Expression.Constant(0);

                for (var j = 1; j <= i; j++)
                {
                    e = Expression.Add(e, Expression.Field(Expression.Constant(bs[j % 2]), "Foo"));
                }

                Assert.Equal(i, Expression.Lambda<Func<int>>(e).Compile(useInterpreter)());
            }
        }

#endregion

#region Generic helpers

        public static void CheckGenericWithStructRestrictionConstantHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            foreach (Ts value in new Ts[] { default(Ts), new Ts() })
            {
                VerifyGenericWithStructRestriction<Ts>(value, useInterpreter);
            }
        }

        public static void CheckGenericHelper<T>(bool useInterpreter)
        {
            foreach (T value in new T[] { default(T) })
            {
                VerifyGeneric<T>(value, useInterpreter);
            }
        }

        public static void CheckGenericWithClassRestrictionHelper<Tc>(bool useInterpreter) where Tc : class
        {
            foreach (Tc value in new Tc[] { null, default(Tc) })
            {
                VerifyGenericWithClassRestriction<Tc>(value, useInterpreter);
            }
        }

        public static void CheckGenericWithClassAndNewRestrictionHelper<Tcn>(bool useInterpreter) where Tcn : class, new()
        {
            foreach (Tcn value in new Tcn[] { null, default(Tcn), new Tcn() })
            {
                VerifyGenericWithClassAndNewRestriction<Tcn>(value, useInterpreter);
            }
        }

        public static void CheckGenericWithSubClassRestrictionHelper<TC>(bool useInterpreter) where TC : C
        {
            foreach (TC value in new TC[] { null, default(TC), (TC)new C() })
            {
                VerifyGenericWithSubClassRestriction<TC>(value, useInterpreter);
            }
        }

        public static void CheckGenericWithSubClassAndNewRestrictionHelper<TCn>(bool useInterpreter) where TCn : C, new()
        {
            foreach (TCn value in new TCn[] { null, default(TCn), new TCn(), (TCn)new C() })
            {
                VerifyGenericWithSubClassAndNewRestriction<TCn>(value, useInterpreter);
            }
        }

#endregion

#region Test verifiers

        private static void VerifyBoolConstant(bool value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Constant(value, typeof(bool)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyByteConstant(byte value, bool useInterpreter)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Constant(value, typeof(byte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyCustomConstant(C value, bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.Constant(value, typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyCharConstant(char value, bool useInterpreter)
        {
            Expression<Func<char>> e =
                Expression.Lambda<Func<char>>(
                    Expression.Constant(value, typeof(char)),
                    Enumerable.Empty<ParameterExpression>());
            Func<char> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyCustom2Constant(D value, bool useInterpreter)
        {
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.Constant(value, typeof(D)),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyDecimalConstant(decimal value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Constant(value, typeof(decimal)),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyDelegateConstant(Delegate value, bool useInterpreter)
        {
            Expression<Func<Delegate>> e =
                Expression.Lambda<Func<Delegate>>(
                    Expression.Constant(value, typeof(Delegate)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Delegate> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyDoubleConstant(double value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Constant(value, typeof(double)),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyEnumConstant(E value, bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.Constant(value, typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyEnumLongConstant(El value, bool useInterpreter)
        {
            Expression<Func<El>> e =
                Expression.Lambda<Func<El>>(
                    Expression.Constant(value, typeof(El)),
                    Enumerable.Empty<ParameterExpression>());
            Func<El> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyFloatConstant(float value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Constant(value, typeof(float)),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyFuncOfObjectConstant(Func<object> value, bool useInterpreter)
        {
            Expression<Func<Func<object>>> e =
                Expression.Lambda<Func<Func<object>>>(
                    Expression.Constant(value, typeof(Func<object>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<object>> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyInterfaceConstant(I value, bool useInterpreter)
        {
            Expression<Func<I>> e =
                Expression.Lambda<Func<I>>(
                    Expression.Constant(value, typeof(I)),
                    Enumerable.Empty<ParameterExpression>());
            Func<I> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyIEquatableOfCustomConstant(IEquatable<C> value, bool useInterpreter)
        {
            Expression<Func<IEquatable<C>>> e =
                Expression.Lambda<Func<IEquatable<C>>>(
                    Expression.Constant(value, typeof(IEquatable<C>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<C>> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyIEquatableOfCustom2Constant(IEquatable<D> value, bool useInterpreter)
        {
            Expression<Func<IEquatable<D>>> e =
                Expression.Lambda<Func<IEquatable<D>>>(
                    Expression.Constant(value, typeof(IEquatable<D>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<D>> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyIntConstant(int value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Constant(value, typeof(int)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyLongConstant(long value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Constant(value, typeof(long)),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyObjectConstant(object value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Constant(value, typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStructConstant(S value, bool useInterpreter)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.Constant(value, typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifySByteConstant(sbyte value, bool useInterpreter)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Constant(value, typeof(sbyte)),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithStringConstant(Sc value, bool useInterpreter)
        {
            Expression<Func<Sc>> e =
                Expression.Lambda<Func<Sc>>(
                    Expression.Constant(value, typeof(Sc)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithStringAndFieldConstant(Scs value, bool useInterpreter)
        {
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.Constant(value, typeof(Scs)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyShortConstant(short value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Constant(value, typeof(short)),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithTwoValuesConstant(Sp value, bool useInterpreter)
        {
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.Constant(value, typeof(Sp)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStructWithValueConstant(Ss value, bool useInterpreter)
        {
            Expression<Func<Ss>> e =
                Expression.Lambda<Func<Ss>>(
                    Expression.Constant(value, typeof(Ss)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyStringConstant(string value, bool useInterpreter)
        {
            Expression<Func<string>> e =
                Expression.Lambda<Func<string>>(
                    Expression.Constant(value, typeof(string)),
                    Enumerable.Empty<ParameterExpression>());
            Func<string> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyUIntConstant(uint value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Constant(value, typeof(uint)),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyULongConstant(ulong value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Constant(value, typeof(ulong)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyUShortConstant(ushort value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Constant(value, typeof(ushort)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyTypeConstant(Type value, bool useInterpreter)
        {
            Expression<Func<Type>> e =
                Expression.Lambda<Func<Type>>(
                    Expression.Constant(value, typeof(Type)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Type> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyMethodInfoConstant(MethodInfo value, bool useInterpreter)
        {
            Expression<Func<MethodInfo>> e =
                Expression.Lambda<Func<MethodInfo>>(
                    Expression.Constant(value, typeof(MethodInfo)),
                    Enumerable.Empty<ParameterExpression>());
            Func<MethodInfo> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyConstructorInfoConstant(ConstructorInfo value, bool useInterpreter)
        {
            Expression<Func<ConstructorInfo>> e =
                Expression.Lambda<Func<ConstructorInfo>>(Expression.Constant(value, typeof(ConstructorInfo)));
            Func<ConstructorInfo> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithStructRestriction<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.Constant(value, typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGeneric<T>(T value, bool useInterpreter)
        {
            Expression<Func<T>> e =
                Expression.Lambda<Func<T>>(
                    Expression.Constant(value, typeof(T)),
                    Enumerable.Empty<ParameterExpression>());
            Func<T> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithClassRestriction<Tc>(Tc value, bool useInterpreter) where Tc : class
        {
            Expression<Func<Tc>> e =
                Expression.Lambda<Func<Tc>>(
                    Expression.Constant(value, typeof(Tc)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tc> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithClassAndNewRestriction<Tcn>(Tcn value, bool useInterpreter) where Tcn : class, new()
        {
            Expression<Func<Tcn>> e =
                Expression.Lambda<Func<Tcn>>(
                    Expression.Constant(value, typeof(Tcn)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Tcn> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithSubClassRestriction<TC>(TC value, bool useInterpreter) where TC : C
        {
            Expression<Func<TC>> e =
                Expression.Lambda<Func<TC>>(
                    Expression.Constant(value, typeof(TC)),
                    Enumerable.Empty<ParameterExpression>());
            Func<TC> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithSubClassAndNewRestriction<TCn>(TCn value, bool useInterpreter) where TCn : C, new()
        {
            Expression<Func<TCn>> e =
                Expression.Lambda<Func<TCn>>(
                    Expression.Constant(value, typeof(TCn)),
                    Enumerable.Empty<ParameterExpression>());
            Func<TCn> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

#endregion

        [Fact]
        public static void InvalidTypeValueType()
        {
            // implicit cast, but not reference assignable.
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Constant(0, typeof(long)));
        }

        [Fact]
        public static void InvalidTypeReferenceType()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Constant("hello", typeof(Expression)));
        }

        [Fact]
        public static void NullType()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Constant("foo", null));
        }

        [Fact]
        public static void ByRefType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Constant(null, typeof(string).MakeByRefType()));
        }

        [Fact]
        public static void PointerType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Constant(null, typeof(string).MakePointerType()));
        }

        [Fact]
        public static void GenericType()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Constant(null, typeof(List<>)));
        }

        [Fact]
        public static void TypeContainsGenericParameters()
        {
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Constant(null, typeof(List<>.Enumerator)));
            AssertExtensions.Throws<ArgumentException>("type", () => Expression.Constant(null, typeof(List<>).MakeGenericType(typeof(List<>))));
        }

        [Fact]
        public static void ToStringTest()
        {
            ConstantExpression e1 = Expression.Constant(1);
            Assert.Equal("1", e1.ToString());

            ConstantExpression e2 = Expression.Constant("bar");
            Assert.Equal("\"bar\"", e2.ToString());

            ConstantExpression e3 = Expression.Constant(null, typeof(object));
            Assert.Equal("null", e3.ToString());

            var b = new Bar();
            ConstantExpression e4 = Expression.Constant(b);
            Assert.Equal($"value({b.ToString()})", e4.ToString());

            var f = new Foo();
            ConstantExpression e5 = Expression.Constant(f);
            Assert.Equal(f.ToString(), e5.ToString());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void DecimalConstantRetainsScaleAnd(bool useInterpreter)
        {
            var lambda = Expression.Lambda<Func<decimal>>(Expression.Constant(-0.000m));
            var func = lambda.Compile(useInterpreter);
            var bits = decimal.GetBits(func());
            Assert.Equal(unchecked((int)0x80030000), bits[3]);
        }


        class Bar
        {
            public int Foo = 41;
            public int Qux = 43;
            public int Baz = 44;
        }

        class Foo
        {
            public override string ToString()
            {
                return "Bar";
            }
        }
    }

    // NB: Should be public in order for ILGen to emit ldtoken
    public class SomePublicMethodsForLdToken
    {
        public static void Bar() { }
        public static void Qux<T>() { }
    }
}
