// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public static IEnumerable<object[]> AndAlso_TestData()
        {
            yield return new object[] { 5, 3, 1, true };
            yield return new object[] { 0, 3, 0, false };
            yield return new object[] { 5, 0, 0, true };
        }

        [Theory]
        [PerCompilationType(nameof(AndAlso_TestData))]
        public static void AndAlso_UserDefinedOperator(int leftValue, int rightValue, int expectedValue, bool calledMethod, bool useInterpreter)
        {
            TrueFalseClass left = new TrueFalseClass(leftValue);
            TrueFalseClass right = new TrueFalseClass(rightValue);

            BinaryExpression expression = Expression.AndAlso(Expression.Constant(left), Expression.Constant(right));
            Func<TrueFalseClass> lambda = Expression.Lambda<Func<TrueFalseClass>>(expression).Compile(useInterpreter);
            Assert.Equal(expectedValue, lambda().Value);

            // AndAlso only evaluates the false operator of left
            Assert.Equal(0, left.TrueCallCount);
            Assert.Equal(1, left.FalseCallCount);
            Assert.Equal(0, right.TrueCallCount);
            Assert.Equal(0, right.FalseCallCount);

            // AndAlso only evaluates the operator if left is not false
            Assert.Equal(calledMethod ? 1 : 0, left.OperatorCallCount);
        }

        [Theory]
        [PerCompilationType(nameof(AndAlso_TestData))]
        public static void AndAlso_UserDefinedOperatorTailCall(int leftValue, int rightValue, int expectedValue, bool calledMethod, bool useInterpreter)
        {
            TrueFalseClass left = new TrueFalseClass(leftValue);
            TrueFalseClass right = new TrueFalseClass(rightValue);

            BinaryExpression expression = Expression.AndAlso(Expression.Constant(left), Expression.Constant(right));
            Func<TrueFalseClass> lambda = Expression.Lambda<Func<TrueFalseClass>>(expression, true).Compile(useInterpreter);
            Assert.Equal(expectedValue, lambda().Value);

            // AndAlso only evaluates the false operator of left
            Assert.Equal(0, left.TrueCallCount);
            Assert.Equal(1, left.FalseCallCount);
            Assert.Equal(0, right.TrueCallCount);
            Assert.Equal(0, right.FalseCallCount);

            // AndAlso only evaluates the operator if left is not false
            Assert.Equal(calledMethod ? 1 : 0, left.OperatorCallCount);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void AndAlso_UserDefinedOperator_HasMethodNotOperator(bool useInterpreter)
        {
            BinaryExpression expression = Expression.AndAlso(Expression.Constant(new NamedMethods(5)), Expression.Constant(new NamedMethods(3)));
            Func<NamedMethods> lambda = Expression.Lambda<Func<NamedMethods>>(expression).Compile(useInterpreter);
            Assert.Equal(1, lambda().Value);
        }

        [Theory]
        [PerCompilationType(nameof(AndAlso_TestData))]
        public static void AndAlso_Method(int leftValue, int rightValue, int expectedValue, bool calledMethod, bool useInterpreter)
        {
            MethodInfo method = typeof(TrueFalseClass).GetMethod(nameof(TrueFalseClass.AndMethod));

            TrueFalseClass left = new TrueFalseClass(leftValue);
            TrueFalseClass right = new TrueFalseClass(rightValue);

            BinaryExpression expression = Expression.AndAlso(Expression.Constant(left), Expression.Constant(right), method);
            Func<TrueFalseClass> lambda = Expression.Lambda<Func<TrueFalseClass>>(expression).Compile(useInterpreter);
            Assert.Equal(expectedValue, lambda().Value);

            // AndAlso only evaluates the false operator of left
            Assert.Equal(0, left.TrueCallCount);
            Assert.Equal(1, left.FalseCallCount);
            Assert.Equal(0, right.TrueCallCount);
            Assert.Equal(0, right.FalseCallCount);

            // AndAlso only evaluates the method if left is not false
            Assert.Equal(0, left.OperatorCallCount);
            Assert.Equal(calledMethod ? 1 : 0, left.MethodCallCount);
        }

        public static IEnumerable<object[]> OrElse_TestData()
        {
            yield return new object[] { 5, 3, 5, false };
            yield return new object[] { 0, 3, 3, true };
            yield return new object[] { 5, 0, 5, false };
        }

        [Theory]
        [PerCompilationType(nameof(OrElse_TestData))]
        public static void OrElse_UserDefinedOperator(int leftValue, int rightValue, int expectedValue, bool calledMethod, bool useInterpreter)
        {
            TrueFalseClass left = new TrueFalseClass(leftValue);
            TrueFalseClass right = new TrueFalseClass(rightValue);

            BinaryExpression expression = Expression.OrElse(Expression.Constant(left), Expression.Constant(right));
            Func<TrueFalseClass> lambda = Expression.Lambda<Func<TrueFalseClass>>(expression).Compile(useInterpreter);
            Assert.Equal(expectedValue, lambda().Value);

            // OrElse only evaluates the true operator of left
            Assert.Equal(1, left.TrueCallCount);
            Assert.Equal(0, left.FalseCallCount);
            Assert.Equal(0, right.TrueCallCount);
            Assert.Equal(0, right.FalseCallCount);

            // OrElse only evaluates the operator if left is not true
            Assert.Equal(calledMethod ? 1 : 0, left.OperatorCallCount);
        }

        [Theory]
        [PerCompilationType(nameof(OrElse_TestData))]
        public static void OrElse_UserDefinedOperatorTailCall(int leftValue, int rightValue, int expectedValue, bool calledMethod, bool useInterpreter)
        {
            TrueFalseClass left = new TrueFalseClass(leftValue);
            TrueFalseClass right = new TrueFalseClass(rightValue);

            BinaryExpression expression = Expression.OrElse(Expression.Constant(left), Expression.Constant(right));
            Func<TrueFalseClass> lambda = Expression.Lambda<Func<TrueFalseClass>>(expression, true).Compile(useInterpreter);
            Assert.Equal(expectedValue, lambda().Value);

            // OrElse only evaluates the true operator of left
            Assert.Equal(1, left.TrueCallCount);
            Assert.Equal(0, left.FalseCallCount);
            Assert.Equal(0, right.TrueCallCount);
            Assert.Equal(0, right.FalseCallCount);

            // OrElse only evaluates the operator if left is not true
            Assert.Equal(calledMethod ? 1 : 0, left.OperatorCallCount);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void OrElse_UserDefinedOperator_HasMethodNotOperator(bool useInterpreter)
        {
            BinaryExpression expression = Expression.OrElse(Expression.Constant(new NamedMethods(0)), Expression.Constant(new NamedMethods(3)));
            Func<NamedMethods> lambda = Expression.Lambda<Func<NamedMethods>>(expression).Compile(useInterpreter);
            Assert.Equal(3, lambda().Value);
        }

        [Theory]
        [PerCompilationType(nameof(OrElse_TestData))]
        public static void OrElse_Method(int leftValue, int rightValue, int expectedValue, bool calledMethod, bool useInterpreter)
        {
            MethodInfo method = typeof(TrueFalseClass).GetMethod(nameof(TrueFalseClass.OrMethod));

            TrueFalseClass left = new TrueFalseClass(leftValue);
            TrueFalseClass right = new TrueFalseClass(rightValue);

            BinaryExpression expression = Expression.OrElse(Expression.Constant(left), Expression.Constant(right), method);
            Func<TrueFalseClass> lambda = Expression.Lambda<Func<TrueFalseClass>>(expression).Compile(useInterpreter);
            Assert.Equal(expectedValue, lambda().Value);

            // OrElse only evaluates the true operator of left
            Assert.Equal(1, left.TrueCallCount);
            Assert.Equal(0, left.FalseCallCount);
            Assert.Equal(0, right.TrueCallCount);
            Assert.Equal(0, right.FalseCallCount);

            // OrElse only evaluates the method if left is not true
            Assert.Equal(0, left.OperatorCallCount);
            Assert.Equal(calledMethod ? 1 : 0, left.MethodCallCount);
        }

        [Fact]
        public static void AndAlso_CannotReduce()
        {
            Expression exp = Expression.AndAlso(Expression.Constant(true), Expression.Constant(false));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void OrElse_CannotReduce()
        {
            Expression exp = Expression.OrElse(Expression.Constant(true), Expression.Constant(false));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void AndAlso_LeftNull_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.AndAlso(null, Expression.Constant(true)));
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.AndAlso(null, Expression.Constant(true), null));
        }

        [Fact]
        public static void OrElse_LeftNull_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.OrElse(null, Expression.Constant(true)));
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.OrElse(null, Expression.Constant(true), null));
        }

        [Fact]
        public static void AndAlso_RightNull_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.AndAlso(Expression.Constant(true), null));
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.AndAlso(Expression.Constant(true), null, null));
        }

        [Fact]
        public static void OrElse_RightNull_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.OrElse(Expression.Constant(true), null));
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.OrElse(Expression.Constant(true), null, null));
        }

        [Fact]
        public static void AndAlso_BinaryOperatorNotDefined_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(5), Expression.Constant("hello")));
            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(5), Expression.Constant("hello"), null));
        }

        [Fact]
        public static void OrElse_BinaryOperatorNotDefined_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Expression.OrElse(Expression.Constant(5), Expression.Constant("hello")));
            Assert.Throws<InvalidOperationException>(() => Expression.OrElse(Expression.Constant(5), Expression.Constant("hello"), null));
        }

        public static IEnumerable<object[]> InvalidMethod_TestData()
        {
            yield return new object[] { typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.InstanceMethod)) };
            yield return new object[] { typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticVoidMethod)) };
        }

        [Theory]
        [ClassData(typeof(OpenGenericMethodsData))]
        [MemberData(nameof(InvalidMethod_TestData))]
        public static void InvalidMethod_ThrowsArgumentException(MethodInfo method)
        {
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.AndAlso(Expression.Constant(5), Expression.Constant(5), method));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.OrElse(Expression.Constant(5), Expression.Constant(5), method));
        }


        [Theory]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.StaticIntMethod0))]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.StaticIntMethod1))]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.StaticIntMethod3))]
        public static void Method_DoesntHaveTwoParameters_ThrowsArgumentException(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName);
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.AndAlso(Expression.Constant(5), Expression.Constant(5), method));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.OrElse(Expression.Constant(5), Expression.Constant(5), method));
        }

        [Fact]
        public static void AndAlso_Method_ExpressionDoesntMatchMethodParameters_ThrowsInvalidOperationException()
        {
            MethodInfo method = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticIntMethod2Valid));
            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant("abc"), Expression.Constant(5), method));
            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(5), Expression.Constant("abc"), method));
        }

        [Fact]
        public static void OrElse_ExpressionDoesntMatchMethodParameters_ThrowsInvalidOperationException()
        {
            MethodInfo method = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticIntMethod2Valid));
            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant("abc"), Expression.Constant(5), method));
            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(5), Expression.Constant("abc"), method));
        }

        [Fact]
        public static void MethodParametersNotEqual_ThrowsArgumentException()
        {
            MethodInfo method = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticIntMethod2Invalid1));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(5), Expression.Constant("abc"), method));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.OrElse(Expression.Constant(5), Expression.Constant("abc"), method));
        }

        [Fact]
        public static void Method_ReturnTypeNotEqualToParameterTypes_ThrowsArgumentException()
        {
            MethodInfo method = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticIntMethod2Invalid2));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(5), Expression.Constant(5), method));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.OrElse(Expression.Constant(5), Expression.Constant(5), method));
        }

        [Fact]
        public static void MethodDeclaringTypeHasNoTrueFalseOperator_ThrowsArgumentException()
        {
            MethodInfo method = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticIntMethod2Valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(5), Expression.Constant(5), method));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.OrElse(Expression.Constant(5), Expression.Constant(5), method));
        }

#if FEATURE_COMPILE

        [Fact]
        public static void AndAlso_NoMethod_NotStatic_ThrowsInvalidOperationException()
        {
            TypeBuilder type = GetTypeBuilder();
            MethodBuilder andOperator = type.DefineMethod("op_BitwiseAnd", MethodAttributes.Public, type, new Type[] { type, type });
            andOperator.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Fact]
        public static void OrElse_NoMethod_NotStatic_ThrowsInvalidOperationException()
        {
            TypeBuilder type = GetTypeBuilder();
            MethodBuilder andOperator = type.DefineMethod("op_BitwiseOr", MethodAttributes.Public, type, new Type[] { type, type });
            andOperator.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            Assert.Throws<InvalidOperationException>(() => Expression.OrElse(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Fact]
        public static void AndAlso_NoMethod_VoidReturnType_ThrowsArgumentException()
        {
            TypeBuilder type = GetTypeBuilder();
            MethodBuilder andOperator = type.DefineMethod("op_BitwiseAnd", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { type, type });
            andOperator.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            AssertExtensions.Throws<ArgumentException>("method", () => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Fact]
        public static void OrElse_NoMethod_VoidReturnType_ThrowsArgumentException()
        {
            TypeBuilder type = GetTypeBuilder();
            MethodBuilder andOperator = type.DefineMethod("op_BitwiseOr", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new Type[] { type, type });
            andOperator.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            AssertExtensions.Throws<ArgumentException>("method", () => Expression.OrElse(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public static void AndAlso_NoMethod_DoesntHaveTwoParameters_ThrowsInvalidOperationException(int parameterCount)
        {
            TypeBuilder type = GetTypeBuilder();
            MethodBuilder andOperator = type.DefineMethod("op_BitwiseAnd", MethodAttributes.Public | MethodAttributes.Static, type, Enumerable.Repeat(type, parameterCount).ToArray());
            andOperator.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public static void OrElse_NoMethod_DoesntHaveTwoParameters_ThrowsInvalidOperationException(int parameterCount)
        {
            TypeBuilder type = GetTypeBuilder();
            MethodBuilder andOperator = type.DefineMethod("op_BitwiseOr", MethodAttributes.Public | MethodAttributes.Static, type, Enumerable.Repeat(type, parameterCount).ToArray());
            andOperator.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            Assert.Throws<InvalidOperationException>(() => Expression.OrElse(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Fact]
        public static void AndAlso_NoMethod_ExpressionDoesntMatchMethodParameters_ThrowsInvalidOperationException()
        {
            TypeBuilder type = GetTypeBuilder();
            MethodBuilder andOperator = type.DefineMethod("op_BitwiseAnd", MethodAttributes.Public | MethodAttributes.Static, type, new Type[] { typeof(int), type });
            andOperator.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Fact]
        public static void OrElse_NoMethod_ExpressionDoesntMatchMethodParameters_ThrowsInvalidOperationException()
        {
            TypeBuilder type = GetTypeBuilder();
            MethodBuilder andOperator = type.DefineMethod("op_BitwiseOr", MethodAttributes.Public | MethodAttributes.Static, type, new Type[] { typeof(int), type });
            andOperator.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            Assert.Throws<InvalidOperationException>(() => Expression.OrElse(Expression.Constant(obj), Expression.Constant(obj)));
        }


        [Fact]
        public static void AndAlso_NoMethod_ReturnTypeNotEqualToParameterTypes_ThrowsArgumentException()
        {
            TypeBuilder type = GetTypeBuilder();
            MethodBuilder andOperator = type.DefineMethod("op_BitwiseAnd", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { type, type });
            andOperator.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Fact]
        public static void OrElse_NoMethod_ReturnTypeNotEqualToParameterTypes_ThrowsArgumentException()
        {
            TypeBuilder type = GetTypeBuilder();
            MethodBuilder andOperator = type.DefineMethod("op_BitwiseOr", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { type, type });
            andOperator.GetILGenerator().Emit(OpCodes.Ret);

            Type createdType = type.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.OrElse(Expression.Constant(obj), Expression.Constant(obj)));
        }

        public static IEnumerable<object[]> Operator_IncorrectMethod_TestData()
        {
            // Does not return bool
            TypeBuilder typeBuilder1 = GetTypeBuilder();
            yield return new object[] { typeBuilder1, typeof(void), new Type[] { typeBuilder1 } };

            // Parameter is not assignable from left
            yield return new object[] { GetTypeBuilder(), typeof(bool), new Type[] { typeof(int) } };

            // Has two parameters
            TypeBuilder typeBuilder2 = GetTypeBuilder();
            yield return new object[] { typeBuilder2, typeof(bool), new Type[] { typeBuilder2, typeBuilder2 } };

            // Has no parameters
            yield return new object[] { GetTypeBuilder(), typeof(bool), new Type[0] };
        }

        [Theory]
        [MemberData(nameof(Operator_IncorrectMethod_TestData))]
        public static void Method_TrueOperatorIncorrectMethod_ThrowsArgumentException(TypeBuilder builder, Type returnType, Type[] parameterTypes)
        {
            MethodBuilder opTrue = builder.DefineMethod("op_True", MethodAttributes.SpecialName | MethodAttributes.Static, returnType, parameterTypes);
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder opFalse = builder.DefineMethod("op_False", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opFalse.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, builder, new Type[] { builder, builder });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);
            MethodInfo createdMethod = createdType.GetMethod("Method");

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj), createdMethod));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.OrElse(Expression.Constant(obj), Expression.Constant(obj), createdMethod));
        }

        [Theory]
        [MemberData(nameof(Operator_IncorrectMethod_TestData))]
        public static void Method_FalseOperatorIncorrectMethod_ThrowsArgumentException(TypeBuilder builder, Type returnType, Type[]parameterTypes)
        {
            MethodBuilder opTrue = builder.DefineMethod("op_True", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder opFalse = builder.DefineMethod("op_False", MethodAttributes.SpecialName | MethodAttributes.Static, returnType, parameterTypes);
            opFalse.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, builder, new Type[] { builder, builder });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);
            MethodInfo createdMethod = createdType.GetMethod("Method");

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj), createdMethod));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.OrElse(Expression.Constant(obj), Expression.Constant(obj), createdMethod));
        }

        [Theory]
        [MemberData(nameof(Operator_IncorrectMethod_TestData))]
        public static void AndAlso_NoMethod_TrueOperatorIncorrectMethod_ThrowsArgumentException(TypeBuilder builder, Type returnType, Type[] parameterTypes)
        {
            MethodBuilder opTrue = builder.DefineMethod("op_True", MethodAttributes.SpecialName | MethodAttributes.Static, returnType, parameterTypes);
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder opFalse = builder.DefineMethod("op_False", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opFalse.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("op_BitwiseAnd", MethodAttributes.Public | MethodAttributes.Static, builder, new Type[] { builder, builder });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Theory]
        [MemberData(nameof(Operator_IncorrectMethod_TestData))]
        public static void OrElse_NoMethod_TrueOperatorIncorrectMethod_ThrowsArgumentException(TypeBuilder builder, Type returnType, Type[] parameterTypes)
        {
            MethodBuilder opTrue = builder.DefineMethod("op_True", MethodAttributes.SpecialName | MethodAttributes.Static, returnType, parameterTypes);
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder opFalse = builder.DefineMethod("op_False", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opFalse.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("op_BitwiseOr", MethodAttributes.Public | MethodAttributes.Static, builder, new Type[] { builder, builder });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.OrElse(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Theory]
        [InlineData("op_True")]
        [InlineData("op_False")]
        public static void Method_NoTrueFalseOperator_ThrowsArgumentException(string name)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            TypeBuilder builder = module.DefineType("Type");

            MethodBuilder opTrue = builder.DefineMethod(name, MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, builder, new Type[] { builder, builder });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);
            MethodInfo createdMethod = createdType.GetMethod("Method");

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj), createdMethod));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.OrElse(Expression.Constant(obj), Expression.Constant(obj), createdMethod));
        }

        [Theory]
        [InlineData("op_True")]
        [InlineData("op_False")]
        public static void AndAlso_NoMethod_NoTrueFalseOperator_ThrowsArgumentException(string name)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            TypeBuilder builder = module.DefineType("Type");

            MethodBuilder opTrue = builder.DefineMethod(name, MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("op_BitwiseAnd", MethodAttributes.Public | MethodAttributes.Static, builder, new Type[] { builder, builder });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Theory]
        [InlineData("op_True")]
        [InlineData("op_False")]
        public static void OrElse_NoMethod_NoTrueFalseOperator_ThrowsArgumentException(string name)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            TypeBuilder builder = module.DefineType("Type");

            MethodBuilder opTrue = builder.DefineMethod(name, MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("op_BitwiseOr", MethodAttributes.Public | MethodAttributes.Static, builder, new Type[] { builder, builder });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            object obj = Activator.CreateInstance(createdType);
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.OrElse(Expression.Constant(obj), Expression.Constant(obj)));
        }

        [Fact]
        public static void Method_ParamsDontMatchOperator_ThrowsInvalidOperationException()
        {
            TypeBuilder builder = GetTypeBuilder();

            MethodBuilder opTrue = builder.DefineMethod("op_True", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder opFalse = builder.DefineMethod("op_False", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opFalse.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("Method", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { typeof(int), typeof(int) });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            MethodInfo createdMethod = createdType.GetMethod("Method");

            Assert.Throws<InvalidOperationException>(() => Expression.AndAlso(Expression.Constant(5), Expression.Constant(5), createdMethod));
            Assert.Throws<InvalidOperationException>(() => Expression.OrElse(Expression.Constant(5), Expression.Constant(5), createdMethod));
        }

        [Fact]
        public static void AndAlso_NoMethod_ParamsDontMatchOperator_ThrowsInvalidOperationException()
        {
            TypeBuilder builder = GetTypeBuilder();

            MethodBuilder opTrue = builder.DefineMethod("op_True", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder opFalse = builder.DefineMethod("op_False", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opFalse.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("op_BitwiseAnd", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { typeof(int), typeof(int) });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            Assert.Throws<InvalidOperationException>(() => Expression.OrElse(Expression.Constant(5), Expression.Constant(5)));
        }

        [Fact]
        public static void OrElse_NoMethod_ParamsDontMatchOperator_ThrowsInvalidOperationException()
        {
            TypeBuilder builder = GetTypeBuilder();

            MethodBuilder opTrue = builder.DefineMethod("op_True", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opTrue.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder opFalse = builder.DefineMethod("op_False", MethodAttributes.SpecialName | MethodAttributes.Static, typeof(bool), new Type[] { builder });
            opFalse.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder method = builder.DefineMethod("op_BitwiseOr", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { typeof(int), typeof(int) });
            method.GetILGenerator().Emit(OpCodes.Ret);

            TypeInfo createdType = builder.CreateTypeInfo();
            Assert.Throws<InvalidOperationException>(() => Expression.OrElse(Expression.Constant(5), Expression.Constant(5)));
        }

#endif

        [Fact]
        public static void ImplicitConversionToBool_ThrowsArgumentException()
        {
            MethodInfo method = typeof(ClassWithImplicitBoolOperator).GetMethod(nameof(ClassWithImplicitBoolOperator.ConversionMethod));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(new ClassWithImplicitBoolOperator()), Expression.Constant(new ClassWithImplicitBoolOperator()), method));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.OrElse(Expression.Constant(new ClassWithImplicitBoolOperator()), Expression.Constant(new ClassWithImplicitBoolOperator()), method));
        }

        [Theory]
        [ClassData(typeof(UnreadableExpressionsData))]
        public static void AndAlso_LeftIsWriteOnly_ThrowsArgumentException(Expression unreadableExpression)
        {
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.AndAlso(unreadableExpression, Expression.Constant(true)));
        }

        [Theory]
        [ClassData(typeof(UnreadableExpressionsData))]
        public static void AndAlso_RightIsWriteOnly_ThrowsArgumentException(Expression unreadableExpression)
        {
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.AndAlso(Expression.Constant(true), unreadableExpression));
        }

        [Theory]
        [ClassData(typeof(UnreadableExpressionsData))]
        public static void OrElse_LeftIsWriteOnly_ThrowsArgumentException(Expression unreadableExpression)
        {
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.OrElse(unreadableExpression, Expression.Constant(true)));
        }

        [Theory]
        [ClassData(typeof(UnreadableExpressionsData))]
        public static void OrElse_RightIsWriteOnly_ThrowsArgumentException(Expression unreadableExpression)
        {
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.OrElse(Expression.Constant(false), unreadableExpression));
        }

        [Fact]
        public static void ToStringTest()
        {
            // NB: These were && and || in .NET 3.5 but shipped as AndAlso and OrElse in .NET 4.0; we kept the latter.

            BinaryExpression e1 = Expression.AndAlso(Expression.Parameter(typeof(bool), "a"), Expression.Parameter(typeof(bool), "b"));
            Assert.Equal("(a AndAlso b)", e1.ToString());

            BinaryExpression e2 = Expression.OrElse(Expression.Parameter(typeof(bool), "a"), Expression.Parameter(typeof(bool), "b"));
            Assert.Equal("(a OrElse b)", e2.ToString());
        }

#if FEATURE_COMPILE
        [Fact]
        public static void AndAlsoGlobalMethod()
        {
            MethodInfo method = GlobalMethod(typeof(int), new[] { typeof(int), typeof(int) });
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.AndAlso(Expression.Constant(1), Expression.Constant(2), method));
        }

        [Fact]
        public static void OrElseGlobalMethod()
        {
            MethodInfo method = GlobalMethod(typeof(int), new [] { typeof(int), typeof(int) });
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.OrElse(Expression.Constant(1), Expression.Constant(2), method));
        }

        private static TypeBuilder GetTypeBuilder()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect);
            ModuleBuilder module = assembly.DefineDynamicModule("Name");
            return module.DefineType("Type");
        }

        private static MethodInfo GlobalMethod(Type returnType, Type[] parameterTypes)
        {
            ModuleBuilder module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("Module");
            MethodBuilder globalMethod = module.DefineGlobalMethod("GlobalMethod", MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);
            globalMethod.GetILGenerator().Emit(OpCodes.Ret);
            module.CreateGlobalFunctions();
            return module.GetMethod(globalMethod.Name);
        }
#endif

        public class NonGenericClass
        {
            public void InstanceMethod() { }
            public static void StaticVoidMethod() { }

            public static int StaticIntMethod0() => 0;
            public static int StaticIntMethod1(int i) => 0;
            public static int StaticIntMethod3(int i1, int i2, int i3) => 0;

            public static int StaticIntMethod2Valid(int i1, int i2) => 0;

            public static int StaticIntMethod2Invalid1(int i1, string i2) => 0;
            public static string StaticIntMethod2Invalid2(int i1, int i2) => "abc";
        }

        public class TrueFalseClass
        {
            public int TrueCallCount { get; set; }
            public int FalseCallCount { get; set; }
            public int OperatorCallCount { get; set; }
            public int MethodCallCount { get; set; }

            public TrueFalseClass(int value) { Value = value; }
            public int Value { get; }

            public static bool operator true(TrueFalseClass c)
            {
                c.TrueCallCount++;
                return c.Value != 0;
            }

            public static bool operator false(TrueFalseClass c)
            {
                c.FalseCallCount++;
                return c.Value == 0;
            }

            public static TrueFalseClass operator &(TrueFalseClass c1, TrueFalseClass c2)
            {
                c1.OperatorCallCount++;
                return new TrueFalseClass(c1.Value & c2.Value);
            }

            public static TrueFalseClass AndMethod(TrueFalseClass c1, TrueFalseClass c2)
            {
                c1.MethodCallCount++;
                return new TrueFalseClass(c1.Value & c2.Value);
            }

            public static TrueFalseClass operator |(TrueFalseClass c1, TrueFalseClass c2)
            {
                c1.OperatorCallCount++;
                return new TrueFalseClass(c1.Value | c2.Value);
            }

            public static TrueFalseClass OrMethod(TrueFalseClass c1, TrueFalseClass c2)
            {
                c1.MethodCallCount++;
                return new TrueFalseClass(c1.Value | c2.Value);
            }
        }

        public class NamedMethods
        {
            public NamedMethods(int value) { Value = value; }
            public int Value { get; }

            public static bool operator true(NamedMethods c) => c.Value != 0;
            public static bool operator false(NamedMethods c) => c.Value == 0;

            public static NamedMethods op_BitwiseAnd(NamedMethods c1, NamedMethods c2) => new NamedMethods(c1.Value & c2.Value);
            public static NamedMethods op_BitwiseOr(NamedMethods c1, NamedMethods c2) => new NamedMethods(c1.Value | c2.Value);
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
