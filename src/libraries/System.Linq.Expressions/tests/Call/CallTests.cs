// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class CallTests
    {
        private struct Mutable
        {
            private int x;

            public int X
            {
                get { return x; }
                set { x = value; }
            }

            public int this[int i]
            {
                get { return x; }
                set { x = value; }
            }

            public int Foo()
            {
                return x++;
            }
        }

        private class Wrapper<T>
        {
            public const int Zero = 0;
            public T Field;
#pragma warning disable 649 // For testing purposes
            public readonly T ReadOnlyField;
#pragma warning restore
            public T Property
            {
                get { return Field; }
                set { Field = value; }
            }
        }

        private static class Methods
        {
            public static void ByRef(ref int x) { ++x; }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void UnboxReturnsReference(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(object));
            UnaryExpression unbox = Expression.Unbox(p, typeof(Mutable));
            MethodCallExpression call = Expression.Call(unbox, typeof(Mutable).GetMethod("Foo"));
            Func<object, int> lambda = Expression.Lambda<Func<object, int>>(call, p).Compile(useInterpreter);

            object boxed = new Mutable();
            Assert.Equal(0, lambda(boxed));
            Assert.Equal(1, lambda(boxed));
            Assert.Equal(2, lambda(boxed));
            Assert.Equal(3, lambda(boxed));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ArrayWriteBack(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Mutable[]));
            BinaryExpression indexed = Expression.ArrayIndex(p, Expression.Constant(0));
            MethodCallExpression call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            Func<Mutable[], int> lambda = Expression.Lambda<Func<Mutable[], int>>(call, p).Compile(useInterpreter);

            var array = new Mutable[1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void EnumArgAndReturn(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(NonGenericClass.E1));
            MethodCallExpression call = Expression.Call(typeof(NonGenericClass).GetMethod("FooEnum"), p);
            Func<NonGenericClass.E1, NonGenericClass.E2> lambda = Expression.Lambda<Func<NonGenericClass.E1, NonGenericClass.E2>>(call, p).Compile(useInterpreter);

            Assert.Equal(NonGenericClass.E2.One, lambda(NonGenericClass.E1.One));
            Assert.Equal(NonGenericClass.E2.Two, lambda(NonGenericClass.E1.Two));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void MultiRankArrayWriteBack(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Mutable[,]));
            MethodCallExpression indexed = Expression.ArrayIndex(p, Expression.Constant(0), Expression.Constant(0));
            MethodCallExpression call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            Func<Mutable[,], int> lambda = Expression.Lambda<Func<Mutable[,], int>>(call, p).Compile(useInterpreter);

            var array = new Mutable[1, 1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ArrayAccessWriteBack(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Mutable[]));
            IndexExpression indexed = Expression.ArrayAccess(p, Expression.Constant(0));
            MethodCallExpression call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            Func<Mutable[], int> lambda = Expression.Lambda<Func<Mutable[], int>>(call, p).Compile(useInterpreter);

            var array = new Mutable[1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void MultiRankArrayAccessWriteBack(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Mutable[,]));
            IndexExpression indexed = Expression.ArrayAccess(p, Expression.Constant(0), Expression.Constant(0));
            MethodCallExpression call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            Func<Mutable[,], int> lambda = Expression.Lambda<Func<Mutable[,], int>>(call, p).Compile(useInterpreter);

            var array = new Mutable[1, 1];
            Assert.Equal(0, lambda(array));
            Assert.Equal(1, lambda(array));
            Assert.Equal(2, lambda(array));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void IndexedPropertyAccessNoWriteBack(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(List<Mutable>));
            IndexExpression indexed = Expression.Property(p, typeof(List<Mutable>).GetProperty("Item"), Expression.Constant(0));
            MethodCallExpression call = Expression.Call(indexed, typeof(Mutable).GetMethod("Foo"));
            Func<List<Mutable>, int> lambda = Expression.Lambda<Func<List<Mutable>, int>>(call, p).Compile(useInterpreter);

            var list = new List<Mutable> { new Mutable() };
            Assert.Equal(0, lambda(list));
            Assert.Equal(0, lambda(list));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void FieldAccessWriteBack(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Wrapper<Mutable>));
            MemberExpression member = Expression.Field(p, typeof(Wrapper<Mutable>).GetField("Field"));
            MethodCallExpression call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            Func<Wrapper<Mutable>, int> lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(useInterpreter);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(1, lambda(wrapper));
            Assert.Equal(2, lambda(wrapper));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void PropertyAccessNoWriteBack(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Wrapper<Mutable>));
            MemberExpression member = Expression.Property(p, typeof(Wrapper<Mutable>).GetProperty("Property"));
            MethodCallExpression call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            Func<Wrapper<Mutable>, int> lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(useInterpreter);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ReadonlyFieldAccessWriteBack(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Wrapper<Mutable>));
            MemberExpression member = Expression.Field(p, typeof(Wrapper<Mutable>).GetField("ReadOnlyField"));
            MethodCallExpression call = Expression.Call(member, typeof(Mutable).GetMethod("Foo"));
            Func<Wrapper<Mutable>, int> lambda = Expression.Lambda<Func<Wrapper<Mutable>, int>>(call, p).Compile(useInterpreter);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
            Assert.Equal(0, lambda(wrapper));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ConstFieldAccessWriteBack(bool useInterpreter)
        {
            MemberExpression member = Expression.Field(null, typeof(Wrapper<Mutable>).GetField("Zero"));
            MethodCallExpression call = Expression.Call(member, typeof(int).GetMethod("GetType"));
            Func<Type> lambda = Expression.Lambda<Func<Type>>(call).Compile(useInterpreter);

            var wrapper = new Wrapper<Mutable>();
            Assert.Equal(typeof(int), lambda());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CallByRefMutableStructPropertyWriteBack(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Mutable));
            MemberExpression x = Expression.Property(p, "X");
            MethodCallExpression call = Expression.Call(typeof(Methods).GetMethod("ByRef"), x);
            BlockExpression body = Expression.Block(call, x);
            Func<Mutable, int> lambda = Expression.Lambda<Func<Mutable, int>>(body, p).Compile(useInterpreter);

            var m = new Mutable() { X = 41 };
            Assert.Equal(42, lambda(m));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CallByRefMutableStructIndexWriteBack(bool useInterpreter)
        {
            // Should not produce tail-call, but should still succeed
            ParameterExpression p = Expression.Parameter(typeof(Mutable));
            IndexExpression x = Expression.MakeIndex(p, typeof(Mutable).GetProperty("Item"), new[] { Expression.Constant(0) });
            MethodCallExpression call = Expression.Call(typeof(Methods).GetMethod("ByRef"), x);
            Action<Mutable> act = Expression.Lambda<Action<Mutable>>(call, true, p).Compile(useInterpreter);

            Mutable m = new Mutable { X = 41 };
            act(m);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CallByRefAttemptTailCall(bool useInterpreter)
        {
            ParameterExpression p = Expression.Parameter(typeof(Mutable));
            IndexExpression x = Expression.MakeIndex(p, typeof(Mutable).GetProperty("Item"), new[] { Expression.Constant(0) });
            MethodCallExpression call = Expression.Call(typeof(Methods).GetMethod("ByRef"), x);
            BlockExpression body = Expression.Block(call, x);
            Func<Mutable, int> lambda = Expression.Lambda<Func<Mutable, int>>(body, p).Compile(useInterpreter);

            var m = new Mutable() { X = 41 };
            Assert.Equal(42, lambda(m));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Call_InstanceNullInside_ThrowsNullReferenceExceptionOnInvocation(bool useInterpreter)
        {
            Expression call = Expression.Call(Expression.Constant(null, typeof(NonGenericClass)), typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.InstanceMethod)));
            Action compiledDelegate = Expression.Lambda<Action>(call).Compile(useInterpreter);
            TargetInvocationException exception = Assert.Throws<TargetInvocationException>(() => compiledDelegate.DynamicInvoke());
            Assert.IsType<NullReferenceException>(exception.InnerException);
        }

        public static IEnumerable<object[]> Call_NoParameters_TestData()
        {
            // Basic
            yield return new object[] { Expression.Constant(new ClassWithInterface1()), typeof(ClassWithInterface1).GetMethod(nameof(ClassWithInterface1.Method)), 2 };
            yield return new object[] { Expression.Constant(new StructWithInterface1()), typeof(StructWithInterface1).GetMethod(nameof(StructWithInterface1.Method)), 2 };

            // Object method
            yield return new object[] { Expression.Constant(new ClassWithInterface1()), typeof(object).GetMethod(nameof(object.GetType)), typeof(ClassWithInterface1) };
            yield return new object[] { Expression.Constant(new StructWithInterface1()), typeof(object).GetMethod(nameof(object.GetType)), typeof(StructWithInterface1) };
            yield return new object[] { Expression.Constant(Int32Enum.A), typeof(object).GetMethod(nameof(object.GetType)), typeof(Int32Enum) };

            // ValueType method from struct
            yield return new object[] { Expression.Constant(new StructWithInterface1()), typeof(ValueType).GetMethod(nameof(ValueType.ToString)), new StructWithInterface1().ToString() };

            // Enum method from enum
            yield return new object[] { Expression.Constant(Int32Enum.A), typeof(Enum).GetMethod(nameof(Enum.ToString), new Type[0]), "A" };

            // Interface method
            yield return new object[] { Expression.Constant(new ClassWithInterface1()), typeof(Interface1).GetMethod(nameof(Interface1.InterfaceMethod)), 1 };
            yield return new object[] { Expression.Constant(new StructWithInterface1()), typeof(Interface1).GetMethod(nameof(Interface1.InterfaceMethod)), 1 };
            yield return new object[] { Expression.Constant(new ClassWithCompoundInterface()), typeof(Interface1).GetMethod(nameof(Interface1.InterfaceMethod)), 1 };
            yield return new object[] { Expression.Constant(new StructWithCompoundInterface()), typeof(Interface1).GetMethod(nameof(Interface1.InterfaceMethod)), 1 };

            // Interface method, interface type
            yield return new object[] { Expression.Constant(new ClassWithInterface1(), typeof(Interface1)), typeof(Interface1).GetMethod(nameof(Interface1.InterfaceMethod)), 1 };
            yield return new object[] { Expression.Constant(new StructWithInterface1(), typeof(Interface1)), typeof(Interface1).GetMethod(nameof(Interface1.InterfaceMethod)), 1 };
        }

        [Theory]
        [PerCompilationType(nameof(Call_NoParameters_TestData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/39771", TestPlatforms.Browser)]
        public static void Call_NoParameters(Expression instance, MethodInfo method, object expected, bool useInterpreter)
        {
            Expression call = Expression.Call(instance, method);
            Delegate compiledDelegate = Expression.Lambda(call).Compile(useInterpreter);
            Assert.Equal(expected, compiledDelegate.DynamicInvoke());
        }

        private static Expression s_valid => Expression.Constant(5);

        private static MethodInfo s_method0 = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.Method0));
        private static MethodInfo s_method1 = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.Method1));
        private static MethodInfo s_method2 = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.Method2));
        private static MethodInfo s_method3 = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.Method3));
        private static MethodInfo s_method4 = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.Method4));
        private static MethodInfo s_method5 = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.Method5));
        private static MethodInfo s_method6 = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.Method6));
        private static MethodInfo s_method7 = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.Method7));

        public static IEnumerable<object[]> Method_Invalid_TestData()
        {
            yield return new object[] { null, typeof(ArgumentNullException) };
            yield return new object[] { typeof(GenericClass<>).GetMethod(nameof(GenericClass<string>.NonGenericMethod)), typeof(ArgumentException) };
            yield return new object[] { typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.GenericMethod)), typeof(ArgumentException) };
        }

        [Theory]
        [MemberData(nameof(Method_Invalid_TestData))]
        public static void Method_Invalid_ThrowsArgumentException(MethodInfo method, Type exceptionType)
        {
            AssertArgumentException(() => Expression.Call(null, method), exceptionType, "method");
            AssertArgumentException(() => Expression.Call(null, method, s_valid, s_valid), exceptionType, "method");
            AssertArgumentException(() => Expression.Call(null, method, s_valid, s_valid, s_valid), exceptionType, "method");
            AssertArgumentException(() => Expression.Call(null, method, new Expression[0]), exceptionType, "method");
            AssertArgumentException(() => Expression.Call(null, method, (IEnumerable<Expression>)new Expression[0]), exceptionType, "method");

            AssertArgumentException(() => Expression.Call(method, s_valid), exceptionType, "method");
            AssertArgumentException(() => Expression.Call(method, s_valid, s_valid), exceptionType, "method");
            AssertArgumentException(() => Expression.Call(method, s_valid, s_valid, s_valid), exceptionType, "method");
            AssertArgumentException(() => Expression.Call(method, s_valid, s_valid, s_valid, s_valid), exceptionType, "method");
            AssertArgumentException(() => Expression.Call(method, s_valid, s_valid, s_valid, s_valid, s_valid), exceptionType, "method");
            AssertArgumentException(() => Expression.Call(method, new Expression[0]), exceptionType, "method");
            AssertArgumentException(() => Expression.Call(method, (IEnumerable<Expression>)new Expression[0]), exceptionType, "method");
        }

        [Fact]
        public static void Method_Invalid_Via_Name()
        {
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(typeof(GenericClass<>), nameof(GenericClass<string>.NonGenericMethod), Type.EmptyTypes));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(typeof(GenericClass<>).MakeGenericType(typeof(GenericClass<>)), nameof(GenericClass<string>.NonGenericMethod), Type.EmptyTypes));
        }

        public static IEnumerable<object[]> Method_DoesntBelongToInstance_TestData()
        {
            // Different declaring type
            yield return new object[] { Expression.Constant(new ClassWithInterface1()), typeof(OtherClassWithInterface1).GetMethod(nameof(Interface1.InterfaceMethod)) };
            yield return new object[] { Expression.Constant(new StructWithInterface1()), typeof(OtherStructWithInterface1).GetMethod(nameof(Interface1.InterfaceMethod)) };

            // Different interface
            yield return new object[] { Expression.Constant(new ClassWithInterface1()), typeof(Interface2).GetMethod(nameof(Interface2.InterfaceMethod)) };
            yield return new object[] { Expression.Constant(new StructWithInterface1()), typeof(Interface2).GetMethod(nameof(Interface2.InterfaceMethod)) };

            // Custom type
            yield return new object[] { Expression.Constant(new ClassWithInterface1(), typeof(object)), typeof(ClassWithInterface1).GetMethod(nameof(ClassWithInterface1.Method)) };
            yield return new object[] { Expression.Constant(new StructWithInterface1(), typeof(object)), typeof(ClassWithInterface1).GetMethod(nameof(ClassWithInterface1.Method)) };
        }

        [Theory]
        [MemberData(nameof(Method_DoesntBelongToInstance_TestData))]
        public static void Method_DoesntBelongToInstance_ThrowsArgumentException(Expression instance, MethodInfo method)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(instance, method));
        }

        [Fact]
        public static void InstanceMethod_NullInstance_ThrowsArgumentException()
        {
            MethodInfo method = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.InstanceMethod));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(method, s_valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(method, s_valid, s_valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(method, s_valid, s_valid, s_valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(method, s_valid, s_valid, s_valid, s_valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(method, s_valid, s_valid, s_valid, s_valid, s_valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(method, new Expression[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(method, (IEnumerable<Expression>)new Expression[0]));

            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(null, method, s_valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(null, method, s_valid, s_valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(null, method, s_valid, s_valid, s_valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(null, method, new Expression[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(null, method, (IEnumerable<Expression>)new Expression[0]));
        }

        [Fact]
        public static void StaticMethod_NonNullInstance_ThrowsArgumentException()
        {
            Expression instance = Expression.Constant(new NonGenericClass());
            MethodInfo method = typeof(NonGenericClass).GetMethod(nameof(NonGenericClass.StaticMethod));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(instance, method, s_valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(instance, method, s_valid, s_valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(instance, method, s_valid, s_valid, s_valid));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(instance, method, new Expression[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(instance, method, (IEnumerable<Expression>)new Expression[0]));
        }

        public static IEnumerable<object[]> InvalidArg_TestData()
        {
            yield return new object[] { null, typeof(ArgumentNullException) };
            yield return new object[] { Expression.Property(null, typeof(Unreadable<string>), nameof(Unreadable<string>.WriteOnly)), typeof(ArgumentException) };
            yield return new object[] { Expression.Constant("abc"), typeof(ArgumentException) };
        }

        [Theory]
        [MemberData(nameof(InvalidArg_TestData))]
        public static void Arg0_Invalid(Expression arg, Type exceptionType)
        {
            AssertArgumentException(() => Expression.Call(s_method1, arg), exceptionType, "arg0");
            AssertArgumentException(() => Expression.Call(s_method2, arg, s_valid), exceptionType, "arg0");
            AssertArgumentException(() => Expression.Call(s_method3, arg, s_valid, s_valid), exceptionType, "arg0");
            AssertArgumentException(() => Expression.Call(s_method4, arg, s_valid, s_valid, s_valid), exceptionType, "arg0");
            AssertArgumentException(() => Expression.Call(s_method5, arg, s_valid, s_valid, s_valid, s_valid), exceptionType, "arg0");

            AssertArgumentException(() => Expression.Call(null, s_method1, arg), exceptionType, "arg0");
            AssertArgumentException(() => Expression.Call(null, s_method2, arg, s_valid), exceptionType, "arg0");
            AssertArgumentException(() => Expression.Call(null, s_method3, arg, s_valid, s_valid), exceptionType, "arg0");
        }

        [Theory]
        [MemberData(nameof(InvalidArg_TestData))]
        public static void Arg1_Invalid(Expression arg, Type exceptionType)
        {
            AssertArgumentException(() => Expression.Call(s_method2, s_valid, arg), exceptionType, "arg1");
            AssertArgumentException(() => Expression.Call(s_method3, s_valid, arg, s_valid), exceptionType, "arg1");
            AssertArgumentException(() => Expression.Call(s_method4, s_valid, arg, s_valid, s_valid), exceptionType, "arg1");
            AssertArgumentException(() => Expression.Call(s_method5, s_valid, arg, s_valid, s_valid, s_valid), exceptionType, "arg1");

            AssertArgumentException(() => Expression.Call(null, s_method2, s_valid, arg), exceptionType, "arg1");
            AssertArgumentException(() => Expression.Call(null, s_method3, s_valid, arg, s_valid), exceptionType, "arg1");
        }

        [Theory]
        [MemberData(nameof(InvalidArg_TestData))]
        public static void Arg2_Invalid(Expression arg, Type exceptionType)
        {
            AssertArgumentException(() => Expression.Call(s_method3, s_valid, s_valid, arg), exceptionType, "arg2");
            AssertArgumentException(() => Expression.Call(s_method4, s_valid, s_valid, arg, s_valid), exceptionType, "arg2");
            AssertArgumentException(() => Expression.Call(s_method5, s_valid, s_valid, arg, s_valid, s_valid), exceptionType, "arg2");

            AssertArgumentException(() => Expression.Call(null, s_method3, s_valid, s_valid, arg), exceptionType, "arg2");
        }

        [Theory]
        [MemberData(nameof(InvalidArg_TestData))]
        public static void Arg3_Invalid(Expression arg, Type exceptionType)
        {
            AssertArgumentException(() => Expression.Call(s_method4, s_valid, s_valid, s_valid, arg), exceptionType, "arg3");
            AssertArgumentException(() => Expression.Call(s_method5, s_valid, s_valid, s_valid, arg, s_valid), exceptionType, "arg3");
        }

        [Theory]
        [MemberData(nameof(InvalidArg_TestData))]
        public static void Arg4_Invalid(Expression arg, Type exceptionType)
        {
            AssertArgumentException(() => Expression.Call(s_method5, s_valid, s_valid, s_valid, s_valid, arg), exceptionType, "arg4");
        }

        private static void AssertArgumentException(Action action, Type exceptionType, string paramName)
        {
            ArgumentException ex = (ArgumentException)Assert.Throws(exceptionType, action);
            Assert.Equal(paramName, ex.ParamName);
        }

        [Theory]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.Method0), 0)]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.Method1), 1)]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.Method2), 2)]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.Method3), 3)]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.Method4), 4)]
        [InlineData(typeof(NonGenericClass), nameof(NonGenericClass.Method5), 5)]
        public static void InvalidArgumentCount_ThrowsArgumentException(Type type, string name, int count)
        {
            MethodInfo method = type.GetMethod(name);
            Expression arg = Expression.Constant("abc");
            if (count != 0)
            {
                AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(method));
                AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(null, method));
            }
            if (count != 1)
            {
                AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(method, arg));
                AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(null, method, arg));
            }
            if (count != 2)
            {
                AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(method, arg, arg));
                AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(null, method, arg, arg));
            }
            if (count != 3)
            {
                AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(method, arg, arg, arg));
                AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(null, method, arg, arg, arg));
            }
            if (count != 4)
            {
                AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(method, arg, arg, arg, arg));
            }
            if (count != 5)
            {
                AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(method, arg, arg, arg, arg, arg));
            }
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(method, Enumerable.Repeat(arg, count + 1).ToArray()));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(method, Enumerable.Repeat(arg, count + 1)));

            AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(null, method, Enumerable.Repeat(arg, count + 1).ToArray()));
            AssertExtensions.Throws<ArgumentException>("method", () => Expression.Call(null, method, Enumerable.Repeat(arg, count + 1)));
        }

        [Fact]
        public static void MethodName_NullInstance_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("instance", () => Expression.Call((Expression)null, "methodName", new Type[0], new Expression[0]));
        }

        [Fact]
        public static void MethodName_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Expression.Call((Type)null, "methodName", new Type[0], new Expression[0]));
        }

        [Fact]
        public static void NullMethodName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("methodName", () => Expression.Call(Expression.Constant(new NonGenericClass()), null, new Type[0], new Expression[0]));
            AssertExtensions.Throws<ArgumentNullException>("methodName", () => Expression.Call(typeof(NonGenericClass), null, new Type[0], new Expression[0]));
        }

        [Fact]
        public static void MethodName_DoesNotExist_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Expression.Call(Expression.Constant(new NonGenericClass()), "NoSuchMethod", null));
            Assert.Throws<InvalidOperationException>(() => Expression.Call(typeof(NonGenericClass), "NoSuchMethod", null));
        }

        public static IEnumerable<object[]> InvalidTypeArgs_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new Type[0] };
            yield return new object[] { new Type[2] };
        }

        [Theory]
        [MemberData(nameof(InvalidTypeArgs_TestData))]
        public static void MethodName_NoSuchGenericMethodWithTypeArgs_ThrowsInvalidOperationException(Type[] typeArgs)
        {
            Assert.Throws<InvalidOperationException>(() => Expression.Call(Expression.Constant(new NonGenericClass()), nameof(NonGenericClass.GenericInstanceMethod), typeArgs));
            Assert.Throws<InvalidOperationException>(() => Expression.Call(typeof(NonGenericClass), nameof(NonGenericClass.GenericStaticMethod), typeArgs));
        }

        [Fact]
        public static void MethodName_TypeArgsDontMatchConstraints_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(Expression.Constant(new NonGenericClass()), nameof(NonGenericClass.ConstrainedInstanceMethod), new Type[] { typeof(object) }));
            AssertExtensions.Throws<ArgumentException>(null, () => Expression.Call(typeof(NonGenericClass), nameof(NonGenericClass.ConstrainedStaticMethod), new Type[] { typeof(object) }));
        }

        [Fact]
        public static void MethodName_NonGenericMethodHasTypeArgs_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Expression.Call(Expression.Constant(new NonGenericClass()), nameof(NonGenericClass.InstanceMethod), new Type[1]));
            Assert.Throws<InvalidOperationException>(() => Expression.Call(typeof(NonGenericClass), nameof(NonGenericClass.StaticMethod), new Type[1]));
        }

        [Fact]
        public static void MethodName_TypeArgsHasNullValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>(null, () => Expression.Call(Expression.Constant(new NonGenericClass()), nameof(NonGenericClass.GenericInstanceMethod), new Type[] { null }));
            AssertExtensions.Throws<ArgumentNullException>(null, () => Expression.Call(typeof(NonGenericClass), nameof(NonGenericClass.GenericStaticMethod), new Type[] { null }));
        }

        [Fact]
        public static void MethodName_ArgumentsHasNullValue_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("arguments", () => Expression.Call(Expression.Constant(new NonGenericClass()), nameof(NonGenericClass.InstanceMethod1), new Type[0], new Expression[] { null }));
            AssertExtensions.Throws<ArgumentNullException>("arguments", () => Expression.Call(typeof(NonGenericClass), nameof(NonGenericClass.StaticMethod1), new Type[0], new Expression[] { null }));
        }

        [Fact]
        public static void MethodName_ArgumentsHasNullValueButDifferentCount_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Expression.Call(Expression.Constant(new NonGenericClass()), nameof(NonGenericClass.InstanceMethod1), new Type[0], new Expression[] { null, Expression.Constant("") }));
            Assert.Throws<InvalidOperationException>(() => Expression.Call(typeof(NonGenericClass), nameof(NonGenericClass.StaticMethod1), new Type[0], new Expression[] { null, Expression.Constant("") }));
        }

        [Fact]
        public static void ToStringTest()
        {
            // NB: Static methods are inconsistent compared to static members; the declaring type is not included

            MethodCallExpression e1 = Expression.Call(null, typeof(SomeMethods).GetMethod(nameof(SomeMethods.S0), BindingFlags.Static | BindingFlags.Public));
            Assert.Equal("S0()", e1.ToString());

            MethodCallExpression e2 = Expression.Call(null, typeof(SomeMethods).GetMethod(nameof(SomeMethods.S1), BindingFlags.Static | BindingFlags.Public), Expression.Parameter(typeof(int), "x"));
            Assert.Equal("S1(x)", e2.ToString());

            MethodCallExpression e3 = Expression.Call(null, typeof(SomeMethods).GetMethod(nameof(SomeMethods.S2), BindingFlags.Static | BindingFlags.Public), Expression.Parameter(typeof(int), "x"), Expression.Parameter(typeof(int), "y"));
            Assert.Equal("S2(x, y)", e3.ToString());

            MethodCallExpression e4 = Expression.Call(Expression.Parameter(typeof(SomeMethods), "o"), typeof(SomeMethods).GetMethod(nameof(SomeMethods.I0), BindingFlags.Instance | BindingFlags.Public));
            Assert.Equal("o.I0()", e4.ToString());

            MethodCallExpression e5 = Expression.Call(Expression.Parameter(typeof(SomeMethods), "o"), typeof(SomeMethods).GetMethod(nameof(SomeMethods.I1), BindingFlags.Instance | BindingFlags.Public), Expression.Parameter(typeof(int), "x"));
            Assert.Equal("o.I1(x)", e5.ToString());

            MethodCallExpression e6 = Expression.Call(Expression.Parameter(typeof(SomeMethods), "o"), typeof(SomeMethods).GetMethod(nameof(SomeMethods.I2), BindingFlags.Instance | BindingFlags.Public), Expression.Parameter(typeof(int), "x"), Expression.Parameter(typeof(int), "y"));
            Assert.Equal("o.I2(x, y)", e6.ToString());

            MethodCallExpression e7 = Expression.Call(null, typeof(ExtensionMethods).GetMethod(nameof(ExtensionMethods.E0), BindingFlags.Static | BindingFlags.Public), Expression.Parameter(typeof(int), "x"));
            Assert.Equal("x.E0()", e7.ToString());

            MethodCallExpression e8 = Expression.Call(null, typeof(ExtensionMethods).GetMethod(nameof(ExtensionMethods.E1), BindingFlags.Static | BindingFlags.Public), Expression.Parameter(typeof(int), "x"), Expression.Parameter(typeof(int), "y"));
            Assert.Equal("x.E1(y)", e8.ToString());

            MethodCallExpression e9 = Expression.Call(null, typeof(ExtensionMethods).GetMethod(nameof(ExtensionMethods.E2), BindingFlags.Static | BindingFlags.Public), Expression.Parameter(typeof(int), "x"), Expression.Parameter(typeof(int), "y"), Expression.Parameter(typeof(int), "z"));
            Assert.Equal("x.E2(y, z)", e9.ToString());
        }

        [Fact]
        public static void GetArguments()
        {
            VerifyGetArguments(Expression.Call(null, s_method0));
            VerifyGetArguments(Expression.Call(null, s_method1, Expression.Constant(0)));
            VerifyGetArguments(
                Expression.Call(null, s_method2, Enumerable.Range(0, 2).Select(i => Expression.Constant(i))));
            VerifyGetArguments(
                Expression.Call(null, s_method3, Enumerable.Range(0, 3).Select(i => Expression.Constant(i))));
            VerifyGetArguments(
                Expression.Call(null, s_method4, Enumerable.Range(0, 4).Select(i => Expression.Constant(i))));
            VerifyGetArguments(
                Expression.Call(null, s_method5, Enumerable.Range(0, 5).Select(i => Expression.Constant(i))));
            VerifyGetArguments(
                Expression.Call(null, s_method6, Enumerable.Range(0, 6).Select(i => Expression.Constant(i))));
            VerifyGetArguments(
                Expression.Call(null, s_method7, Enumerable.Range(0, 7).Select(i => Expression.Constant(i))));
            var site = Expression.Default(typeof(NonGenericClass));
            VerifyGetArguments(Expression.Call(site, nameof(NonGenericClass.InstanceMethod0), null));
            VerifyGetArguments(
                Expression.Call(site, nameof(NonGenericClass.InstanceMethod1), null, Expression.Constant(0)));
            VerifyGetArguments(
                Expression.Call(
                    site, nameof(NonGenericClass.InstanceMethod2), null,
                    Enumerable.Range(0, 2).Select(i => Expression.Constant(i)).ToArray()));
            VerifyGetArguments(
                Expression.Call(
                    site, nameof(NonGenericClass.InstanceMethod3), null,
                    Enumerable.Range(0, 3).Select(i => Expression.Constant(i)).ToArray()));
            VerifyGetArguments(
                Expression.Call(
                    site, nameof(NonGenericClass.InstanceMethod4), null,
                    Enumerable.Range(0, 4).Select(i => Expression.Constant(i)).ToArray()));
        }

        private static void VerifyGetArguments(MethodCallExpression call)
        {
            var args = call.Arguments;
            Assert.Equal(args.Count, call.ArgumentCount);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => call.GetArgument(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => call.GetArgument(args.Count));
            for (int i = 0; i != args.Count; ++i)
            {
                Assert.Same(args[i], call.GetArgument(i));
                Assert.Equal(i, ((ConstantExpression)call.GetArgument(i)).Value);
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void EnumReturnType0(bool useInterpreter)
        {
            Expression<Func<DayOfWeek[]>> expr = () => new[] { ToDayOfWeek0() };
            Assert.Equal(DayOfWeek.Monday, expr.Compile(useInterpreter)()[0]);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void EnumReturnType1(bool useInterpreter)
        {
            Expression<Func<DayOfWeek[]>> expr = () => new[] { ToDayOfWeek1(1) };
            Assert.Equal(DayOfWeek.Monday, expr.Compile(useInterpreter)()[0]);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void EnumReturnType2(bool useInterpreter)
        {
            Expression<Func<DayOfWeek[]>> expr = () => new[] { ToDayOfWeek2(0, 1) };
            Assert.Equal(DayOfWeek.Monday, expr.Compile(useInterpreter)()[0]);
        }

        private static DayOfWeek ToDayOfWeek0() => DayOfWeek.Monday;
        private static DayOfWeek ToDayOfWeek1(int i) => (DayOfWeek)i;
        private static DayOfWeek ToDayOfWeek2(int i, int j) => (DayOfWeek)(i + j);

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NullableEnumReturnType0(bool useInterpreter)
        {
            Expression<Func<DayOfWeek?[]>> expr = () => new[] { ToDayOfWeekOpt0() };
            Assert.Equal(DayOfWeek.Monday, expr.Compile(useInterpreter)()[0].Value);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NullableEnumReturnType1(bool useInterpreter)
        {
            Expression<Func<DayOfWeek?[]>> expr = () => new[] { ToDayOfWeekOpt1(1) };
            Assert.Equal(DayOfWeek.Monday, expr.Compile(useInterpreter)()[0].Value);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void NullableEnumReturnType2(bool useInterpreter)
        {
            Expression<Func<DayOfWeek?[]>> expr = () => new[] { ToDayOfWeekOpt2(0, 1) };
            Assert.Equal(DayOfWeek.Monday, expr.Compile(useInterpreter)()[0].Value);
        }

        private static DayOfWeek? ToDayOfWeekOpt0() => DayOfWeek.Monday;
        private static DayOfWeek? ToDayOfWeekOpt1(int i) => (DayOfWeek)i;
        private static DayOfWeek? ToDayOfWeekOpt2(int i, int j) => (DayOfWeek)(i + j);

        public class GenericClass<T>
        {
            public static void NonGenericMethod() { }
        }

        public static class Unreadable<T>
        {
            public static T WriteOnly { set { } }
        }

        public class NonGenericClass
        {
            public static void GenericMethod<T>() { }
            public void InstanceMethod() { }
            public static void StaticMethod() { }

            public static void Method0() { }
            public static void Method1(int i1) { }
            public static void Method2(int i1, int i2) { }
            public static void Method3(int i1, int i2, int i3) { }
            public static void Method4(int i1, int i2, int i3, int i4) { }
            public static void Method5(int i1, int i2, int i3, int i4, int i5) { }
            public static void Method6(int i1, int i2, int i3, int i4, int i5, int i6) { }
            public static void Method7(int i1, int i2, int i3, int i4, int i5, int i6, int i7) { }

            public void staticSameName(uint i1) { }
            public void instanceSameName(int i1) { }

            public static void StaticSameName(uint i1) { }
            public static void staticSameName(int i1) { }

            public void GenericInstanceMethod<T>(T t1) { }
            public static void GenericStaticMethod<T>(T t1) { }

            public void ConstrainedInstanceMethod<T>(T t1) where T : struct { }
            public static void ConstrainedStaticMethod<T>(T t1) where T : struct { }

            public void InstanceMethod0() { }
            public void InstanceMethod1(int i1) { }
            public void InstanceMethod2(int i1, int i2) { }
            public void InstanceMethod3(int i1, int i2, int i3) { }
            public void InstanceMethod4(int i1, int i2, int i3, int i4) { }
            public static void StaticMethod1(int i1) { }

            public enum E1 : byte
            {
                One,
                Two
            }

            public enum E2 : int
            {
                One,
                Two
            }

            public static E2 FooEnum(E1 arg)
            {
                return (E2)arg;
            }
        }

        public interface Interface1
        {
            int InterfaceMethod();
        }

        public interface Interface2
        {
            int InterfaceMethod();
        }

        public interface CompoundInterface : Interface1 { }

        public class ClassWithInterface1 : Interface1
        {
            public int InterfaceMethod() => 1;
            public int Method() => 2;
        }

        public class OtherClassWithInterface1 : Interface1
        {
            public int InterfaceMethod() => 1;
        }

        public struct StructWithInterface1 : Interface1
        {
            public int InterfaceMethod() => 1;
            public int Method() => 2;
        }

        public struct OtherStructWithInterface1 : Interface1
        {
            public int InterfaceMethod() => 1;
        }

        public class ClassWithCompoundInterface : CompoundInterface
        {
            public int InterfaceMethod() => 1;
        }

        public struct StructWithCompoundInterface : CompoundInterface
        {
            public int InterfaceMethod() => 1;
        }
    }

    class SomeMethods
    {
        public static void S0() { }
        public static void S1(int x) { }
        public static void S2(int x, int y) { }

        public void I0() { }
        public void I1(int x) { }
        public void I2(int x, int y) { }
    }

    static class ExtensionMethods
    {
        public static void E0(this int x) { }
        public static void E1(this int x, int y) { }
        public static void E2(this int x, int y, int z) { }
    }
}
