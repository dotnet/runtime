// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryLogicalTests
    {
        //TODO: Need tests on the short-circuit and non-short-circuit nature of the two forms.

        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolAndTest(bool useInterpreter)
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolAnd(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolAndAlsoTest(bool useInterpreter)
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolAndAlso(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolOrTest(bool useInterpreter)
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolOr(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckBoolOrElseTest(bool useInterpreter)
        {
            bool[] array = new bool[] { true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyBoolOrElse(array[i], array[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBoolAnd(bool a, bool b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.And(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a & b, f());
        }

        private static void VerifyBoolAndAlso(bool a, bool b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.AndAlso(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a && b, f());
        }

        private static void VerifyBoolOr(bool a, bool b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Or(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a | b, f());
        }

        private static void VerifyBoolOrElse(bool a, bool b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.OrElse(
                        Expression.Constant(a, typeof(bool)),
                        Expression.Constant(b, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a || b, f());
        }

        #endregion

        [Fact]
        public static void CannotReduceAndAlso()
        {
            Expression exp = Expression.AndAlso(Expression.Constant(true), Expression.Constant(false));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void CannotReduceOrElse()
        {
            Expression exp = Expression.OrElse(Expression.Constant(true), Expression.Constant(false));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void AndAlso_LeftNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.AndAlso(null, Expression.Constant(true)));
            Assert.Throws<ArgumentNullException>("left", () => Expression.AndAlso(null, Expression.Constant(true), null));
        }

        [Fact]
        public static void AndAlso_RightNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.AndAlso(Expression.Constant(true), null));
            Assert.Throws<ArgumentNullException>("right", () => Expression.AndAlso(Expression.Constant(true), null, null));
        }

        [Fact]
        public static void AndAlso_BinaryOperatorNotDefined_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(5), Expression.Constant("hello")));
            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(5), Expression.Constant("hello"), null));
        }
        
        [Theory]
        [InlineData(typeof(GenericClass<>), nameof(GenericClass<string>.NonGenericMethod))]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.GenericMethod))]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.InstanceMethod))]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.StaticVoidMethod))]
        public static void AndAlso_InvalidMethod_ThrowsArgumentException(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName);
            Assert.Throws<ArgumentException>("method", () => Expression.AndAlso(Expression.Constant(5), Expression.Constant(5), method));
        }

        [Theory]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.StaticIntMethod0))]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.StaticIntMethod1))]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.StaticIntMethod3))]
        public static void AndAlso_MethodDoesNotHaveTwoParameters_ThrowsArgumentException(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName);
            Assert.Throws<ArgumentException>("method", () => Expression.AndAlso(Expression.Constant(5), Expression.Constant(5), method));
        }

        [Fact]
        public static void AndAlso_ExpressionDoesntMatchMethodParameter_ThrowsInvalidOperationException()
        {
            MethodInfo method = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticIntMethod2Valid));
            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant("abc"), Expression.Constant(5), method));
            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(5), Expression.Constant("abc"), method));
        }

        [Fact]
        public static void AndAlso_MethodParametersNotEqual_ThrowsArgumentException()
        {
            MethodInfo method = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticIntMethod2Invalid1));
            Assert.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(5), Expression.Constant("abc"), method));
        }

        [Fact]
        public static void AndAlso_MethodReturnTypeNotEqualToParameterTypes_ThrowsArgumentException()
        {
            MethodInfo method = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticIntMethod2Invalid2));
            Assert.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(5), Expression.Constant(5), method));
        }

        [Fact]
        public static void AndAlso_MethodDeclaringTypeHasNoTrueFalseOperator_ThrowsArgumentException()
        {
            MethodInfo method = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticIntMethod2Valid));
            Assert.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(5), Expression.Constant(5), method));
        }

        public static IEnumerable<object[]> Operator_IncorrectMethod_TestData()
        {
            // Does not return bool
            TypeBuilder typeBuilder1 = GetTypeBuilder();
            yield return new object[] { typeBuilder1, typeof(void), new Type[] { typeBuilder1.AsType() } };
            
            // Parameter is not assignable from left
            yield return new object[] { GetTypeBuilder(), typeof(bool), new Type[] { typeof(int) } };

            // Has two parameters
            TypeBuilder typeBuilder2 = GetTypeBuilder();
            yield return new object[] { typeBuilder2, typeof(bool), new Type[] { typeBuilder2.AsType(), typeBuilder2.AsType() } };

            // Has no parameters
            yield return new object[] { GetTypeBuilder(), typeof(bool), new Type[0] };
        }

        [Theory]
        [MemberData(nameof(Operator_IncorrectMethod_TestData))]
        public static void AndAlso_TrueOperatorIncorrectMethod_ThrowsArgumentException(TypeBuilder builder, Type returnType, Type[] parameterTypes)
        {
            MethodBuilder opTrue = builder.DefineMethod("op_True", MethodAttributes.SpecialName | MethodAttributes.Static, returnType, parameterTypes);
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder opFalse = builder.DefineMethod("op_False", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder.AsType() });
            opFalse.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, builder.AsType(), new Type[] { builder.AsType(), builder.AsType() });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType.AsType());
            MethodInfo createdMethod = createdType.GetMethod("Method");

            Assert.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj), createdMethod));
        }

        [Theory]
        [MemberData(nameof(Operator_IncorrectMethod_TestData))]
        public static void AndAlso_FalseOperatorIncorrectMethod_ThrowsArgumentException(TypeBuilder builder, Type returnType, Type[] parameterTypes)
        {
            MethodBuilder opTrue = builder.DefineMethod("op_True", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder.AsType() });
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder opFalse = builder.DefineMethod("op_False", MethodAttributes.SpecialName | MethodAttributes.Static, returnType, parameterTypes);
            opFalse.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, builder.AsType(), new Type[] { builder.AsType(), builder.AsType() });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType.AsType());
            MethodInfo createdMethod = createdType.GetMethod("Method");

            Assert.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj), createdMethod));
        }

        [Theory]
        [InlineData("op_True")]
        [InlineData("op_False")]
        public static void AndAlso_NoOperator_ThrowsArgumentException(string name)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            TypeBuilder builder = module.DefineType("Type");

            MethodBuilder opTrue = builder.DefineMethod(name, MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder.AsType() });
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, builder.AsType(), new Type[] { builder.AsType(), builder.AsType() });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType.AsType());
            MethodInfo createdMethod = createdType.GetMethod("Method");

            Assert.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj), createdMethod));
        }

        [Fact]
        public static void AndAlso_MethodParamsDontMatchOperator_ThrowsInvalidOperationException()
        {
            TypeBuilder builder = GetTypeBuilder();

            MethodBuilder opTrue = builder.DefineMethod("op_True", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder.AsType() });
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder opFalse = builder.DefineMethod("op_False", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder.AsType() });
            opFalse.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { typeof(int), typeof(int) });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            MethodInfo createdMethod = createdType.GetMethod("Method");

            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(5), Expression.Constant(5), createdMethod));
        }

        [Fact]
        public static void AndAlso_ImplicitConversionToBool_ThrowsArgumentException()
        {
            MethodInfo method = typeof(ClassWithImplicitBoolOperator).GetMethod(nameof(ClassWithImplicitBoolOperator.ConversionMethod));
            Assert.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(new ClassWithImplicitBoolOperator()), Expression.Constant(new ClassWithImplicitBoolOperator()), method));
        }

        [Fact]
        public static void OrElseThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.OrElse(null, Expression.Constant(true)));
        }

        [Fact]
        public static void OrElseThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.OrElse(Expression.Constant(true), null));
        }

        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        [Fact]
        public static void AndAlso_LeftIsWriteOnly_ThrowsArgumentException()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<bool>), "WriteOnly");
            Assert.Throws<ArgumentException>("left", () => Expression.AndAlso(value, Expression.Constant(true)));
        }

        [Fact]
        public static void AndAlso_RightIsWriteOnly_ThrowsArgumentException()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<bool>), "WriteOnly");
            Assert.Throws<ArgumentException>("right", () => Expression.AndAlso(Expression.Constant(true), value));
        }

        [Fact]
        public static void OrElseThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<bool>), "WriteOnly");
            Assert.Throws<ArgumentException>("left", () => Expression.OrElse(value, Expression.Constant(true)));
        }

        [Fact]
        public static void OrElseThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<bool>), "WriteOnly");
            Assert.Throws<ArgumentException>("right", () => Expression.OrElse(Expression.Constant(false), value));
        }

        [Fact]
        public static void ToStringTest()
        {
            // NB: These were && and || in .NET 3.5 but shipped as AndAlso and OrElse in .NET 4.0; we kept the latter.

            var e1 = Expression.AndAlso(Expression.Parameter(typeof(bool), "a"), Expression.Parameter(typeof(bool), "b"));
            Assert.Equal("(a AndAlso b)", e1.ToString());

            var e2 = Expression.OrElse(Expression.Parameter(typeof(bool), "a"), Expression.Parameter(typeof(bool), "b"));
            Assert.Equal("(a OrElse b)", e2.ToString());
        }
        
        private static TypeBuilder GetTypeBuilder()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            return module.DefineType("Type");
        }

        public class GenericClass<T>
        {
            public void NonGenericMethod() { }
        }

        public class NonGenericClass
        {
            public void GenericMethod<T>() { }
            public void InstanceMethod() { }
            public static void StaticVoidMethod() { }

            public static int StaticIntMethod0() => 0;
            public static int StaticIntMethod1(int i) => 0;
            public static int StaticIntMethod3(int i1, int i2, int i3) => 0;

            public static int StaticIntMethod2Valid(int i1, int i2) => 0;

            public static int StaticIntMethod2Invalid1(int i1, string i2) => 0;
            public static string StaticIntMethod2Invalid2(int i1, int i2) => "abc";
        }

        public class ClassWithNoTrueFalseOperator
        {
            public static int Method(int i1, int i2) => i1 & i2;
        }

        public class ClassWithTrueFalseOperator
        {
            public static int Method(int i1, int i2) => i1 & i2;

            public static bool operator true(ClassWithTrueFalseOperator obj) => obj != null;
            public static bool operator false(ClassWithTrueFalseOperator obj) => obj == null;
        }

        public class ClassWithImplicitBoolOperator
        {
            public static ClassWithImplicitBoolOperator ConversionMethod(ClassWithImplicitBoolOperator bool1, ClassWithImplicitBoolOperator bool2)
            {
                return bool1;
            }

            public static implicit operator bool(ClassWithImplicitBoolOperator boolClass) => true;
        }
    }
}
