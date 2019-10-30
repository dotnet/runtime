// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class UnaryConvertTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertBoxingTest(bool useInterpreter)
        {
            foreach (var e in ConvertBoxing())
            {
                VerifyUnaryConvert(e, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUnboxingTest(bool useInterpreter)
        {
            foreach (var e in ConvertUnboxing())
            {
                VerifyUnaryConvert(e, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertDelegatesTest(bool useInterpreter)
        {
            foreach (var e in ConvertDelegates())
            {
                VerifyUnaryConvert(e, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUnboxingInvalidCastTest(bool useInterpreter)
        {
            foreach (var e in ConvertUnboxingInvalidCast())
            {
                VerifyUnaryConvertThrows<InvalidCastException>(e, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryConvertBooleanToNumericTest(bool useInterpreter)
        {
            foreach (var kv in ConvertBooleanToNumeric())
            {
                VerifyUnaryConvert(kv.Key, kv.Value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullToNonNullableValueTest(bool useInterpreter)
        {
            foreach (var e in ConvertNullToNonNullableValue())
            {
                VerifyUnaryConvertThrows<NullReferenceException>(e, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertNullToNullableValueTest(bool useInterpreter)
        {
            foreach (var e in ConvertNullToNullableValue())
            {
                VerifyUnaryConvert(e, null, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUnderlyingTypeToEnumTypeTest(bool useInterpreter)
        {
            DayOfWeek enumValue = DayOfWeek.Monday;
            var value = (int)enumValue;

            foreach (var o in new[] { Expression.Constant(value, typeof(int)), Expression.Constant(value, typeof(ValueType)), Expression.Constant(value, typeof(object)) })
            {
                VerifyUnaryConvert(Expression.Convert(o, typeof(DayOfWeek)), enumValue, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertUnderlyingTypeToNullableEnumTypeTest(bool useInterpreter)
        {
            DayOfWeek enumValue = DayOfWeek.Monday;
            var value = (int)enumValue;

            ConstantExpression cInt = Expression.Constant(value, typeof(int));
            VerifyUnaryConvert(Expression.Convert(cInt, typeof(DayOfWeek?)), enumValue, useInterpreter);

            ConstantExpression cObj = Expression.Constant(value, typeof(object));
            VerifyUnaryConvertThrows<InvalidCastException>(Expression.Convert(cObj, typeof(DayOfWeek?)), useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertArrayToIncompatibleTypeTest(bool useInterpreter)
        {
            var arr = new object[] { "bar" };

            foreach (var t in new[] { typeof(string[]), typeof(IEnumerable<char>[]) })
            {
                VerifyUnaryConvertThrows<InvalidCastException>(Expression.Convert(Expression.Constant(arr), t), useInterpreter);
            }
        }

        [Fact]
        public static void ToStringTest()
        {
            // NB: Unlike TypeAs, the output does not include the type we're converting to

            UnaryExpression e1 = Expression.Convert(Expression.Parameter(typeof(object), "o"), typeof(int));
            Assert.Equal("Convert(o, Int32)", e1.ToString());

            UnaryExpression e2 = Expression.ConvertChecked(Expression.Parameter(typeof(long), "x"), typeof(int));
            Assert.Equal("ConvertChecked(x, Int32)", e2.ToString());
        }

        private static IEnumerable<KeyValuePair<Expression, object>> ConvertBooleanToNumeric()
        {
            ConstantExpression boolF = Expression.Constant(false);
            ConstantExpression boolT = Expression.Constant(true);

            var factories = new Func<Expression, Type, Expression>[] { Expression.Convert, Expression.ConvertChecked };

            foreach (var factory in factories)
            {
                foreach (var b in new[] { false, true })
                {
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(byte)), (byte)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(sbyte)), (sbyte)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(ushort)), (ushort)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(short)), (short)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(uint)), (uint)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(int)), (int)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(ulong)), (ulong)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(long)), (long)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(float)), (float)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(double)), (double)(b ? 1 : 0));
                    yield return new KeyValuePair<Expression, object>(factory(b ? boolT : boolF, typeof(char)), (char)(b ? 1 : 0));
                }
            }
        }

        private static IEnumerable<Expression> ConvertNullToNonNullableValue()
        {
            ConstantExpression nullC = Expression.Constant(null);

            var factories = new Func<Expression, Type, Expression>[] { Expression.Convert, Expression.ConvertChecked };

            foreach (var factory in factories)
            {
                foreach (var b in new[] { false, true })
                {
                    yield return factory(nullC, typeof(byte));
                    yield return factory(nullC, typeof(sbyte));
                    yield return factory(nullC, typeof(ushort));
                    yield return factory(nullC, typeof(short));
                    yield return factory(nullC, typeof(uint));
                    yield return factory(nullC, typeof(int));
                    yield return factory(nullC, typeof(ulong));
                    yield return factory(nullC, typeof(long));
                    yield return factory(nullC, typeof(float));
                    yield return factory(nullC, typeof(double));
                    yield return factory(nullC, typeof(char));
                    yield return factory(nullC, typeof(TimeSpan));
                    yield return factory(nullC, typeof(DayOfWeek));
                }
            }
        }

        private static IEnumerable<Expression> ConvertNullToNullableValue()
        {
            ConstantExpression nullC = Expression.Constant(null);

            var factories = new Func<Expression, Type, Expression>[] { Expression.Convert, Expression.ConvertChecked };

            foreach (var factory in factories)
            {
                foreach (var b in new[] { false, true })
                {
                    yield return factory(nullC, typeof(byte?));
                    yield return factory(nullC, typeof(sbyte?));
                    yield return factory(nullC, typeof(ushort?));
                    yield return factory(nullC, typeof(short?));
                    yield return factory(nullC, typeof(uint?));
                    yield return factory(nullC, typeof(int?));
                    yield return factory(nullC, typeof(ulong?));
                    yield return factory(nullC, typeof(long?));
                    yield return factory(nullC, typeof(float?));
                    yield return factory(nullC, typeof(double?));
                    yield return factory(nullC, typeof(char?));
                    yield return factory(nullC, typeof(TimeSpan?));
                    yield return factory(nullC, typeof(DayOfWeek?));
                }
            }
        }

        private static IEnumerable<Expression> ConvertBoxing()
        {
            // C# Language Specification - 4.3.1 Boxing conversions
            // ----------------------------------------------------

            var factories = new Func<Expression, Type, Expression>[] { Expression.Convert, Expression.ConvertChecked };

            foreach (var factory in factories)
            {
                // >>> From any value-type to the type object.
                // >>> From any value-type to the type System.ValueType.
                foreach (var t in new[] { typeof(object), typeof(ValueType) })
                {
                    yield return factory(Expression.Constant(1, typeof(int)), t);
                    yield return factory(Expression.Constant(DayOfWeek.Monday, typeof(DayOfWeek)), t);
                    yield return factory(Expression.Constant(new TimeSpan(3, 14, 15), typeof(TimeSpan)), t);

                    yield return factory(Expression.Constant(1, typeof(int?)), t);
                    yield return factory(Expression.Constant(DayOfWeek.Monday, typeof(DayOfWeek?)), t);
                    yield return factory(Expression.Constant(new TimeSpan(3, 14, 15), typeof(TimeSpan?)), t);

                    yield return factory(Expression.Constant(null, typeof(int?)), t);
                    yield return factory(Expression.Constant(null, typeof(DayOfWeek?)), t);
                    yield return factory(Expression.Constant(null, typeof(TimeSpan?)), t);
                }

                // >>> From any non-nullable-value-type to any interface-type implemented by the value-type.
                foreach (var o in new object[] { 1, DayOfWeek.Monday, new TimeSpan(3, 14, 15) })
                {
                    Type t = o.GetType();
                    ConstantExpression c = Expression.Constant(o, t);

                    foreach (var i in t.GetTypeInfo().ImplementedInterfaces)
                    {
                        yield return factory(c, i);
                    }
                }

                // >>> From any nullable-type to any interface-type implemented by the underlying type of the nullable-type.
                foreach (var o in new object[] { (int?)1, (DayOfWeek?)DayOfWeek.Monday, (TimeSpan?)new TimeSpan(3, 14, 15) })
                {
                    Type t = o.GetType();
                    Type n = typeof(Nullable<>).MakeGenericType(t);

                    foreach (var c in new[] { Expression.Constant(o, n), Expression.Constant(null, n) })
                    {
                        foreach (var i in t.GetTypeInfo().ImplementedInterfaces)
                        {
                            yield return factory(c, i);
                        }
                    }
                }

                // >>> From any enum-type to the type System.Enum.
                {
                    yield return factory(Expression.Constant(DayOfWeek.Monday, typeof(DayOfWeek)), typeof(Enum));
                }

                // >>> From any nullable-type with an underlying enum-type to the type System.Enum.
                {
                    yield return factory(Expression.Constant(DayOfWeek.Monday, typeof(DayOfWeek?)), typeof(Enum));
                    yield return factory(Expression.Constant(null, typeof(DayOfWeek?)), typeof(Enum));
                }
            }
        }

        private static IEnumerable<Expression> ConvertUnboxing()
        {
            // C# Language Specification - 4.3.2 Unboxing conversions
            // ------------------------------------------------------

            var factories = new Func<Expression, Type, Expression>[] { Expression.Convert, Expression.ConvertChecked };

            foreach (var factory in factories)
            {
                // >>> From the type object to any value-type.
                // >>> From the type System.ValueType to any value-type.
                foreach (var o in new object[] { 1, DayOfWeek.Monday, new TimeSpan(3, 14, 15) })
                {
                    Type t = o.GetType();
                    Type n = typeof(Nullable<>).MakeGenericType(t);

                    foreach (var f in new[] { typeof(object), typeof(ValueType) })
                    {
                        yield return factory(Expression.Constant(o, typeof(object)), t);
                        yield return factory(Expression.Constant(o, typeof(object)), n);
                    }
                }

                // >>> From any interface-type to any non-nullable-value-type that implements the interface-type.
                foreach (var o in new object[] { 1, DayOfWeek.Monday, new TimeSpan(3, 14, 15) })
                {
                    Type t = o.GetType();

                    foreach (var i in t.GetTypeInfo().ImplementedInterfaces)
                    {
                        yield return factory(Expression.Constant(o, i), t);
                    }
                }

                // >>> From any interface-type to any nullable-type whose underlying type implements the interface-type.
                foreach (var o in new object[] { 1, DayOfWeek.Monday, new TimeSpan(3, 14, 15) })
                {
                    Type t = o.GetType();
                    Type n = typeof(Nullable<>).MakeGenericType(t);

                    foreach (var i in t.GetTypeInfo().ImplementedInterfaces)
                    {
                        yield return factory(Expression.Constant(o, i), n);
                    }
                }

                // >>> From the type System.Enum to any enum-type.
                {
                    yield return factory(Expression.Constant(DayOfWeek.Monday, typeof(Enum)), typeof(DayOfWeek));
                }

                // >>> From the type System.Enum to any nullable-type with an underlying enum-type.
                {
                    yield return factory(Expression.Constant(DayOfWeek.Monday, typeof(Enum)), typeof(DayOfWeek?));
                    yield return factory(Expression.Constant(null, typeof(Enum)), typeof(DayOfWeek?));
                }
            }
        }

        private static IEnumerable<Expression> ConvertDelegates()
        {
            var factories = new Func<Expression, Type, Expression>[] { Expression.Convert, Expression.ConvertChecked };

            foreach (var factory in factories)
            {
                yield return factory(Expression.Constant((Action)(() => { })), typeof(Action));

                yield return factory(Expression.Constant((Action<int>)(x => { })), typeof(Action<int>));
                yield return factory(Expression.Constant((Action<int, object>)((x, o) => { })), typeof(Action<int, object>));
                yield return factory(Expression.Constant((Action<int, object>)((x, o) => { })), typeof(Action<int, string>)); // contravariant
                yield return factory(Expression.Constant((Action<object, int>)((o, x) => { })), typeof(Action<string, int>)); // contravariant

                yield return factory(Expression.Constant((Func<int>)(() => 42)), typeof(Func<int>));
                yield return factory(Expression.Constant((Func<string>)(() => "bar")), typeof(Func<string>));
                yield return factory(Expression.Constant((Func<string>)(() => "bar")), typeof(Func<object>)); // covariant
                yield return factory(Expression.Constant((Func<int, string>)(x => "bar")), typeof(Func<int, object>)); // covariant

                yield return factory(Expression.Constant((Func<object, string>)(o => "bar")), typeof(Func<string, object>)); // contravariant and covariant
                yield return factory(Expression.Constant((Func<object, int, string>)((o, x) => "bar")), typeof(Func<string, int, object>)); // contravariant and covariant
                yield return factory(Expression.Constant((Func<int, object, string>)((x, o) => "bar")), typeof(Func<int, string, object>)); // contravariant and covariant
            }
        }

        private static IEnumerable<Expression> ConvertUnboxingInvalidCast()
        {
            var objs = new object[] { 1, 1L, 1.0f, 1.0, true, TimeSpan.FromSeconds(1), "bar" };
            Type[] types = objs.Select(o => o.GetType()).ToArray();

            foreach (var o in objs)
            {
                ConstantExpression c = Expression.Constant(o, typeof(object));

                foreach (var t in types)
                {
                    if (t != o.GetType())
                    {
                        yield return Expression.Convert(c, t);

                        if (t.IsValueType)
                        {
                            Type n = typeof(Nullable<>).MakeGenericType(t);
                            yield return Expression.Convert(c, n);
                        }
                    }
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyUnaryConvert(Expression e, object o, bool useInterpreter)
        {
            Expression<Func<object>> f =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(e, typeof(object)));

            Func<object> c = f.Compile(useInterpreter);
            Assert.Equal(o, c());
        }

        private static void VerifyUnaryConvertThrows<T>(Expression e, bool useInterpreter)
            where T : Exception
        {
            Expression<Func<object>> f =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(e, typeof(object)));

            Func<object> c = f.Compile(useInterpreter);
            Assert.Throws<T>(() => c());
        }

        private static void VerifyUnaryConvert(Expression e, bool useInterpreter)
        {
            Expression<Func<object>> f =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(e, typeof(object)));

            Func<object> c = f.Compile(useInterpreter);
            c(); // should not throw
        }

        #endregion
    }
}
