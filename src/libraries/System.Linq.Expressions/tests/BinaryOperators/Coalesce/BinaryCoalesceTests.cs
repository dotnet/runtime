// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryCoalesceTests
    {
        public static IEnumerable<object[]> TestData()
        {
            foreach (bool useInterpreter in new bool[] { true, false })
            {
                yield return new object[] { new bool?[] { null, true, false }, new bool[] { true, false }, useInterpreter };
                yield return new object[] { new byte?[] { null, 0, 1, byte.MaxValue }, new byte[] { 0, 1, byte.MaxValue }, useInterpreter };
                yield return new object[] { new C[] { null, new C(), new D(), new D(0), new D(5) }, new C[] { null, new C(), new D(), new D(0), new D(5) }, useInterpreter };
                yield return new object[] { new char?[] { null, '\0', '\b', 'A', '\uffff' }, new char[] { '\0', '\b', 'A', '\uffff' }, useInterpreter };
                yield return new object[] { new D[] { null, new D(), new D(0), new D(5) }, new D[] { null, new D(), new D(0), new D(5) }, useInterpreter };
                yield return new object[] { new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }, new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }, useInterpreter };
                yield return new object[] { new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } }, new Delegate[] { null, (Func<object>)delegate () { return null; }, (Func<int, int>)delegate (int i) { return i + 1; }, (Action<object>)delegate { } }, useInterpreter };
                yield return new object[] { new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN }, new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN }, useInterpreter};
                yield return new object[] { new E?[] { null, 0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue }, new E[] { 0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue }, useInterpreter };
                yield return new object[] { new El?[] { null, 0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue }, new El[] { 0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue }, useInterpreter };
                yield return new object[] { new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN }, new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN }, useInterpreter };
                yield return new object[] { new Func<object>[] { null, delegate () { return null; } }, new Func<object>[] { null, delegate () { return null; } }, useInterpreter };
                yield return new object[] { new I[] { null, new C(), new D(), new D(0), new D(5) }, new I[] { null, new C(), new D(), new D(0), new D(5) }, useInterpreter };
                yield return new object[] { new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) }, new IEquatable<C>[] { null, new C(), new D(), new D(0), new D(5) }, useInterpreter };
                yield return new object[] { new IEquatable<D>[] { null, new D(), new D(0), new D(5) }, new IEquatable<D>[] { null, new D(), new D(0), new D(5) }, useInterpreter };
                yield return new object[] { new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue }, new int[] { 0, 1, -1, int.MinValue, int.MaxValue }, useInterpreter };
                yield return new object[] { new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue }, new long[] { 0, 1, -1, long.MinValue, long.MaxValue }, useInterpreter };
                yield return new object[] { new object[] { null, new object(), new C(), new D(3) }, new object[] { null, new object(), new C(), new D(3) }, useInterpreter };
                yield return new object[] { new S?[] { null, default(S), new S() }, new S[] { default(S), new S() }, useInterpreter };
                yield return new object[] { new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }, new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }, useInterpreter };
                yield return new object[] { new Sc?[] { null, default(Sc), new Sc(), new Sc(null) }, new Sc[] { default(Sc), new Sc(), new Sc(null) }, useInterpreter };
                yield return new object[] { new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) }, new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) }, useInterpreter };
                yield return new object[] { new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue }, new short[] { 0, 1, -1, short.MinValue, short.MaxValue }, useInterpreter };
                yield return new object[] { new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) }, new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) }, useInterpreter };
                yield return new object[] { new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) }, new Ss[] { default(Ss), new Ss(), new Ss(new S()) }, useInterpreter };
                yield return new object[] { new string[] { null, "", "a", "foo" }, new string[] { null, "", "a", "foo" }, useInterpreter };
                yield return new object[] { new uint?[] { null, 0, 1, uint.MaxValue }, new uint[] { 0, 1, uint.MaxValue }, useInterpreter };
                yield return new object[] { new ulong?[] { null, 0, 1, ulong.MaxValue }, new ulong[] { 0, 1, ulong.MaxValue }, useInterpreter };
                yield return new object[] { new ushort?[] { null, 0, 1, ushort.MaxValue }, new ushort[] { 0, 1, ushort.MaxValue }, useInterpreter };
                yield return new object[] { new string[] { null, "", "a", "foo" }, new string[] { null, "", "a", "foo" }, useInterpreter };

                yield return new object[] { new bool?[] { null, true, false }, new bool?[] { null, true, false }, useInterpreter };
                yield return new object[] { new byte?[] { null, 0, 1, byte.MaxValue }, new byte?[] { null, 0, 1, byte.MaxValue }, useInterpreter };
                yield return new object[] { new char?[] { null, '\0', '\b', 'A', '\uffff' }, new char?[] { null, '\0', '\b', 'A', '\uffff' }, useInterpreter };
                yield return new object[] { new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }, new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue }, useInterpreter };
                yield return new object[] { new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN }, new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN }, useInterpreter };
                yield return new object[] { new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue }, new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue }, useInterpreter };
                yield return new object[] { new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue }, new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue }, useInterpreter };
                yield return new object[] { new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN }, new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN }, useInterpreter };
                yield return new object[] { new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue }, new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue }, useInterpreter };
                yield return new object[] { new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue }, new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue }, useInterpreter };
                yield return new object[] { new S?[] { null, default(S), new S() }, new S?[] { null, default(S), new S() }, useInterpreter };
                yield return new object[] { new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }, new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue }, useInterpreter };
                yield return new object[] { new Sc?[] { null, default(Sc), new Sc(), new Sc(null) }, new Sc?[] { null, default(Sc), new Sc(), new Sc(null) }, useInterpreter };
                yield return new object[] { new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) }, new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) }, useInterpreter };
                yield return new object[] { new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue }, new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue }, useInterpreter };
                yield return new object[] { new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) }, new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) }, useInterpreter };
                yield return new object[] { new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) }, new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) }, useInterpreter };
                yield return new object[] { new uint?[] { null, 0, 1, uint.MaxValue }, new uint?[] { null, 0, 1, uint.MaxValue }, useInterpreter };
                yield return new object[] { new ulong?[] { null, 0, 1, ulong.MaxValue }, new ulong?[] { null, 0, 1, ulong.MaxValue }, useInterpreter };
                yield return new object[] { new ushort?[] { null, 0, 1, ushort.MaxValue }, new ushort?[] { null, 0, 1, ushort.MaxValue }, useInterpreter };
            }
        }

        public static IEnumerable<object[]> ImplicitNumericConversionData()
        {
            foreach (bool useInterpreter in new bool[] { true, false })
            {
                yield return new object[] { new byte?[] { null, 1 }, new object[] { (byte)2, (short)3, (ushort)3, (int)4, (uint)4, (long)5, (ulong)5, (float)3.14, (double)3.14, (decimal)49.95 }, useInterpreter };
                yield return new object[] { new sbyte?[] { null, 1 }, new object[] { (sbyte)2, (short)3, (int)4, (long)5, (float)3.14, (double)3.14, (decimal)49.95 }, useInterpreter };
                yield return new object[] { new ushort?[] { null, 1 }, new object[] { (ushort)3, (int)4, (uint)4, (long)5, (ulong)5, (float)3.14, (double)3.14, (decimal)49.95 }, useInterpreter };
                yield return new object[] { new short?[] { null, 1 }, new object[] { (short)3, (int)4, (long)5, (float)3.14, (double)3.14, (decimal)49.95 }, useInterpreter };
                yield return new object[] { new uint?[] { null, 1 }, new object[] { (uint)4, (long)5, (ulong)5, (float)3.14, (double)3.14, (decimal)49.95 }, useInterpreter };
                yield return new object[] { new int?[] { null, 1 }, new object[] { (int)4, (long)5, (float)3.14, (double)3.14, (decimal)49.95 }, useInterpreter };
                yield return new object[] { new ulong?[] { null, 1 }, new object[] { (ulong)5, (float)3.14, (double)3.14, (decimal)49.95 }, useInterpreter };
                yield return new object[] { new long?[] { null, 1 }, new object[] { (long)5, (float)3.14, (double)3.14, (decimal)49.95 }, useInterpreter };
                yield return new object[] { new char?[] { null, 'a' }, new object[] { (ushort)3, (int)4, (uint)4, (long)5, (ulong)5, (float)3.14, (double)3.14, (decimal)49.95 }, useInterpreter };
                yield return new object[] { new float?[] { null, 1F }, new object[] { (float)3.14, (double)3.14 }, useInterpreter };
                yield return new object[] { new double?[] { null, 1D }, new object[] { (double)3.14 }, useInterpreter };
                yield return new object[] { new decimal?[] { null, 1M }, new object[] { (decimal)49.95 }, useInterpreter };
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public static void Coalesce(Array array1, Array array2, bool useInterpreter)
        {
            Type type1 = array1.GetType().GetElementType();
            Type type2 = array2.GetType().GetElementType();
            for (int i = 0; i < array1.Length; i++)
            {
                object value1 = array1.GetValue(i);
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyCoalesce(value1, type1, array2.GetValue(j), type2, useInterpreter);
                }
            }
        }

        [Theory]
        [MemberData(nameof(ImplicitNumericConversionData))]
        public static void ImplicitNumericConversions(Array array1, Array array2, bool useInterpreter)
        {
            Type type1 = array1.GetType().GetElementType();
            for (int i = 0; i < array1.Length; i++)
            {
                object value1 = array1.GetValue(i);
                for (int j = 0; j < array2.Length; j++)
                {
                    object value2 = array2.GetValue(j);
                    Type type2 = value2.GetType();

                    object result = value1 != null ? value1 : value2;
                    if (result.GetType() == typeof(char))
                    {
                        // ChangeType does not support conversion of char to float, double, or decimal,
                        // although these are classified as implicit numeric conversions, so we widen
                        // the value to Int32 first as a workaround

                        result = Convert.ChangeType(result, typeof(int));
                    }

                    object expected = Convert.ChangeType(result, type2);

                    VerifyCoalesce(value1, type1, value2, type2, useInterpreter, expected);
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

        private static void CheckGenericWithClassRestrictionCoalesceHelper<Tc>(bool useInterpreter) where Tc : class
        {
            Tc[] array1 = new Tc[] { null, default(Tc) };
            Tc[] array2 = new Tc[] { null, default(Tc) };

            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyCoalesce(array1[i], typeof(Tc), array2[j], typeof(Tc), useInterpreter);
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
                    VerifyCoalesce(array1[i], typeof(TC), array2[j], typeof(TC), useInterpreter);
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
                    VerifyCoalesce(array1[i], typeof(Tcn), array2[j], typeof(Tcn), useInterpreter);
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
                    VerifyCoalesce(array1[i], typeof(TCn), array2[j], typeof(TCn), useInterpreter);
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
                    VerifyCoalesce(array1[i], typeof(Ts?), array2[j], typeof(Ts), useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericEnumWithStructRestrictionCoalesce_NullableTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCoalesce_NullableHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStructRestrictionCoalesce_NullableTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCoalesce_NullableHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckGenericStructWithStringAndFieldWithStructRestrictionCoalesce_NullableTest(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCoalesce_NullableHelper<Scs>(useInterpreter);
        }

        private static void CheckGenericWithStructRestrictionCoalesce_NullableHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts?[] array = new Ts?[] { null, default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyCoalesce(array[i], typeof(Ts?), array[j], typeof(Ts?), useInterpreter);
                }
            }
        }

        private static void VerifyCoalesce(object obj1, Type type1, object obj2, Type type2, bool useInterpreter, object expected = null)
        {
            BinaryExpression expression = Expression.Coalesce(Expression.Constant(obj1, type1), Expression.Constant(obj2, type2));
            Delegate lambda = Expression.Lambda(expression).Compile(useInterpreter);

            expected = expected ?? (obj1 == null ? obj2 : obj1);
            Assert.Equal(expected, lambda.DynamicInvoke());
        }

        [Fact]
        public static void BasicCoalesceExpressionTest()
        {
            int? i = 0;
            double? d = 0;
            ConstantExpression left = Expression.Constant(d, typeof(double?));
            ConstantExpression right = Expression.Constant(i, typeof(int?));
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
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void ThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.Coalesce(null, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.Coalesce(Expression.Constant(""), null));
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
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.Coalesce(value, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<string>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.Coalesce(Expression.Constant(""), value));
        }

        [Theory]
        [InlinePerCompilationType(null, "YY")]
        [InlinePerCompilationType("abc", "abcdef")]
        public static void Conversion_String(string parameter, string expected, bool useInterpreter)
        {
            Expression<Func<string, string>> conversion = x => x + "def";
            ParameterExpression parameterExpression = Expression.Parameter(typeof(string));
            BinaryExpression coalescion = Expression.Coalesce(parameterExpression, Expression.Constant("YY"), conversion);

            Func<string, string> result = Expression.Lambda<Func<string, string>>(coalescion, parameterExpression).Compile(useInterpreter);
            Assert.Equal(expected, result(parameter));
        }

        [Theory]
        [InlinePerCompilationType(null, 5)]
        [InlinePerCompilationType(5, 10)]
        public static void Conversion_NullableInt(int? parameter, int? expected, bool useInterpreter)
        {
            Expression<Func<int?, int?>> conversion = x => x * 2;
            ParameterExpression parameterExpression = Expression.Parameter(typeof(int?));
            BinaryExpression coalescion = Expression.Coalesce(parameterExpression, Expression.Constant(5, typeof(int?)), conversion);

            Func<int?, int?> result = Expression.Lambda<Func<int?, int?>>(coalescion, parameterExpression).Compile(useInterpreter);
            Assert.Equal(expected, result(parameter));
        }

        [Fact]
        public static void Left_NonNullValueType_ThrowsInvalidOperationException()
        {
            Expression<Func<int, int>> conversion = x => x * 2;

            Assert.Throws<InvalidOperationException>(() => Expression.Coalesce(Expression.Constant(5), Expression.Constant(5)));
            Assert.Throws<InvalidOperationException>(() => Expression.Coalesce(Expression.Constant(5), Expression.Constant(5), conversion));
        }

        [Fact]
        public static void RightLeft_NonEquivalentTypes_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Coalesce(Expression.Constant("abc"), Expression.Constant(5)));
        }

        public delegate void VoidDelegate();

        [Fact]
        public static void Conversion_VoidReturnType_ThrowsArgumentException()
        {
            LambdaExpression conversion = Expression.Lambda(typeof(VoidDelegate), Expression.Constant(""));

            AssertExtensions.Throws<ArgumentException>("conversion", () => Expression.Coalesce(Expression.Constant(""), Expression.Constant(""), conversion));
        }

        [Fact]
        public static void Conversion_NumberOfParameters_NotOne_ThrowsArgumentException()
        {
            Expression<Func<int, int, int>> moreThanOne = (x, y) => x * 2;
            Expression<Func<int>> lessThanOne = () => 2;

            AssertExtensions.Throws<ArgumentException>("conversion", () => Expression.Coalesce(Expression.Constant(""), Expression.Constant(""), moreThanOne));
            AssertExtensions.Throws<ArgumentException>("conversion", () => Expression.Coalesce(Expression.Constant(""), Expression.Constant(""), lessThanOne));
        }

        [Fact]
        public static void Conversion_ReturnTypeNotEquivilientToRightType_ThrowsInvalidOperationException()
        {
            Expression<Func<int?, int>> nullableNotEquivalent = x => x ?? 5;
            Assert.Throws<InvalidOperationException>(() => Expression.Coalesce(Expression.Constant(5, typeof(int?)), Expression.Constant(5, typeof(int?)), nullableNotEquivalent));

            Expression<Func<string, bool>> stringNotEquivalent = x => x == "";
            Assert.Throws<InvalidOperationException>(() => Expression.Coalesce(Expression.Constant(""), Expression.Constant(""), stringNotEquivalent));
        }

        [Fact]
        public static void Conversion_ParameterTypeNotEquivalentToLeftType_ThrowsInvalidOperationException()
        {
            Expression<Func<bool, string>> boolNotEquivalent = x => x.ToString();
            Assert.Throws<InvalidOperationException>(() => Expression.Coalesce(Expression.Constant(""), Expression.Constant(""), boolNotEquivalent));
            Assert.Throws<InvalidOperationException>(() => Expression.Coalesce(Expression.Constant(0, typeof(int?)), Expression.Constant(""), boolNotEquivalent));
        }

        [Fact]
        public static void ToStringTest()
        {
            BinaryExpression e = Expression.Coalesce(Expression.Parameter(typeof(string), "a"), Expression.Parameter(typeof(string), "b"));
            Assert.Equal("(a ?? b)", e.ToString());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CoalesceToWiderReference(bool useInterpreter)
        {
            Func<object> func = Expression.Lambda<Func<object>>(
                Expression.Coalesce(
                    Expression.Constant("abc"),
                    Expression.Constant("def", typeof(object))
                    )).Compile(useInterpreter);
            Assert.Equal("abc", func());

            func = Expression.Lambda<Func<object>>(
                Expression.Coalesce(
                    Expression.Constant(null, typeof(string)),
                    Expression.Constant("def", typeof(object))
                )).Compile(useInterpreter);
            Assert.Equal("def", func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CoalesceToNarrowerReference(bool useInterpreter)
        {
            Func<object> func = Expression.Lambda<Func<object>>(
                Expression.Coalesce(
                    Expression.Constant("abc", typeof(object)),
                    Expression.Constant("def")
                )).Compile(useInterpreter);
            Assert.Equal("abc", func());

            func = Expression.Lambda<Func<object>>(
                Expression.Coalesce(
                    Expression.Constant(null),
                    Expression.Constant("def")
                )).Compile(useInterpreter);
            Assert.Equal("def", func());
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CoalesceReferenceToValueType(bool useInterpreter)
        {
            Func<object> func = Expression.Lambda<Func<object>>(
                Expression.Coalesce(
                    Expression.Constant(2, typeof(object)),
                    Expression.Constant(1)
                )).Compile(useInterpreter);
            Assert.Equal(2, func());

            func = Expression.Lambda<Func<object>>(
                Expression.Coalesce(
                    Expression.Constant(null),
                    Expression.Constant(1)
                )).Compile(useInterpreter);
            Assert.Equal(1, func());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotLinqExpressionsBuiltWithIsInterpretingOnly))]
        [ActiveIssue("https://github.com/mono/mono/issues/14919", TestRuntimes.Mono)]
        public static void VerifyIL_NullableIntCoalesceToNullableInt()
        {
            ParameterExpression x = Expression.Parameter(typeof(int?));
            ParameterExpression y = Expression.Parameter(typeof(int?));
            Expression<Func<int?, int?, int?>> f =
                Expression.Lambda<Func<int?, int?, int?>>(Expression.Coalesce(x, y), x, y);

            f.VerifyIL(
                @".method valuetype [System.Private.CoreLib]System.Nullable`1<int32> ::lambda_method(class [System.Linq.Expressions]System.Runtime.CompilerServices.Closure,valuetype [System.Private.CoreLib]System.Nullable`1<int32>,valuetype [System.Private.CoreLib]System.Nullable`1<int32>)
                {
                    .maxstack 1
                    .locals init (
                        [0] valuetype [System.Private.CoreLib]System.Nullable`1<int32>
                    )

                    IL_0000: ldarg.1
                    IL_0001: stloc.0
                    IL_0002: ldloca.s   V_0
                    IL_0004: call       instance bool valuetype [System.Private.CoreLib]System.Nullable`1<int32>::get_HasValue()
                    IL_0009: brfalse    IL_0014
                    IL_000e: ldloc.0
                    IL_000f: br         IL_0015
                    IL_0014: ldarg.2
                    IL_0015: ret
                }");
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CoalesceWideningLeft(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(int?));
            ParameterExpression y = Expression.Parameter(typeof(long));
            Func<int?, long, long> func = Expression.Lambda<Func<int?, long, long>>(Expression.Coalesce(x, y), x, y).Compile(useInterpreter);
            Assert.Equal(2, func(null, 2));
            Assert.Equal(2, func(2, 1));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CoalesceWideningLeftNullableRight(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(int?));
            ParameterExpression y = Expression.Parameter(typeof(long?));
            Func<int?, long?, long?> func = Expression.Lambda<Func<int?, long?, long?>>(Expression.Coalesce(x, y), x, y).Compile(useInterpreter);
            Assert.Equal(2, func(null, 2));
            Assert.Equal(2, func(2, 1));
            Assert.Equal(2, func(2, null));
            Assert.Null(func(null, null));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CoalesceWideningRight(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(long?));
            ParameterExpression y = Expression.Parameter(typeof(int));
            Func<long?, int, long> func = Expression.Lambda<Func<long?, int, long>>(Expression.Coalesce(x, y), x, y).Compile(useInterpreter);
            Assert.Equal(2, func(null, 2));
            Assert.Equal(2, func(2, 1));
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CoalesceWideningRightNullable(bool useInterpreter)
        {
            ParameterExpression x = Expression.Parameter(typeof(long?));
            ParameterExpression y = Expression.Parameter(typeof(int?));
            Func<long?, int?, long?> func = Expression.Lambda<Func<long?, int?, long?>>(Expression.Coalesce(x, y), x, y).Compile(useInterpreter);
            Assert.Equal(2, func(null, 2));
            Assert.Equal(2, func(2, 1));
            Assert.Equal(2, func(2, null));
            Assert.Null(func(null, null));
        }
    }
}
