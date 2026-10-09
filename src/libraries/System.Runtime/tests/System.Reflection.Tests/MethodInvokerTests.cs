// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Tests
{
    /// <summary>
    /// These tests use the shared tests from the base class with MethodInvoker.Invoke.
    /// </summary>
    public class MethodInvokerTests : MethodCommonTests
    {
        public override object? Invoke(MethodInfo methodInfo, object? obj, object?[]? parameters)
        {
            return MethodInvoker.Create(methodInfo).Invoke(obj, new Span<object>(parameters));
        }

        protected override bool SupportsMissing => false;

        [Theory]
        [InlineData(nameof(RefReturningArgument))]
        [InlineData(nameof(RefReturningArgumentFew))]
        [InlineData(nameof(RefReturningArgumentMany))]
        public void Invoke_RefReturnAliasesArgument(string methodName)
        {
            MethodInfo method = typeof(MethodInvokerTests).GetMethod(methodName)!;
            MethodInvoker invoker = MethodInvoker.Create(method);
            object?[] arguments = new object?[method.GetParameters().Length];
            Array.Fill(arguments, new object());

            for (int i = 0; i < 100; i++)
            {
                object? expected = i % 2 == 0 ? new object() : null;
                arguments[0] = expected;
                Assert.Same(expected, method.Invoke(null, arguments));
                Assert.Same(expected, arguments[0]);
                Assert.Same(expected, invoker.Invoke(null, arguments.AsSpan()));
                Assert.Same(expected, arguments[0]);

                if (arguments.Length == 1)
                {
                    Assert.Same(expected, invoker.Invoke(null, arguments[0]));
                }
                else if (arguments.Length == 4)
                {
                    Assert.Same(expected, invoker.Invoke(null, arguments[0], arguments[1], arguments[2], arguments[3]));
                }
            }
        }

        public static ref object? RefReturningArgument(ref object? value) => ref value;

        public static ref object? RefReturningArgumentFew(ref object? value, object? a, object? b, object? c) => ref value;

        public static ref object? RefReturningArgumentMany(ref object? value, object? a, object? b, object? c, object? d) => ref value;

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void SharedThunk_CachedInvokerPromotes(bool useByRef)
        {
            MethodInfo method = typeof(CachedInvokerTarget).GetMethod(
                useByRef ? nameof(CachedInvokerTarget.TryGetValue) : nameof(CachedInvokerTarget.Echo))!;
            MethodInvoker invoker = MethodInvoker.Create(method);
            var target = new CachedInvokerTarget();
            object argument = new object();
            object?[] arguments = { target, null };

            for (int i = 0; i <= IntrinsicInvokeSelectionAssertions.CachedTargetSpecializationThreshold; i++)
            {
                if (useByRef)
                {
                    Assert.Equal(true, invoker.Invoke(null, arguments.AsSpan()));
                    Assert.Same(target, arguments[1]);
                }
                else
                {
                    Assert.Same(argument, invoker.Invoke(target, argument));
                }

                if (i == 0 || i == IntrinsicInvokeSelectionAssertions.CachedTargetSpecializationThreshold - 1)
                {
                    IntrinsicInvokeSelectionAssertions.AssertNotPromoted(invoker, i + 1, IntrinsicInvokeSelectionAssertions.CachedTargetSpecializationThreshold);
                }
            }

            Assert.Equal(IntrinsicInvokeSelectionAssertions.CachedTargetSpecializationThreshold + 1, target.CallCount);
            IntrinsicInvokeSelectionAssertions.AssertPromoted(invoker, IntrinsicInvokeSelectionAssertions.CachedTargetSpecializationThreshold);
        }

        [Fact]
        public void SharedThunk_ObjectMethodOnBoxedValueReceiverUsesSharedThunk()
        {
            MethodInfo method = typeof(object).GetMethod(nameof(object.ToString))!;
            MethodInvoker invoker = MethodInvoker.Create(method);

            // The virtual dispatch resolves to the struct's own unboxing(-and-instantiating) stub,
            // which is self-contained and call-compatible with the shared thunk.
            Assert.Equal("50", invoker.Invoke(new IntrinsicInvokeStructReceiver(50)));
            IntrinsicInvokeSelectionAssertions.AssertShared(invoker);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Constructor_ExistingInstanceAcrossTiers(bool useSpan)
        {
            ConstructorInfo constructor = typeof(RefConstructorTarget).GetConstructor(new[] { typeof(int).MakeByRefType() });
            MethodInvoker invoker = MethodInvoker.Create(constructor);
            var target = (RefConstructorTarget)RuntimeHelpers.GetUninitializedObject(typeof(RefConstructorTarget));

            for (int i = 0; i <= IntrinsicInvokeSelectionAssertions.CachedTargetSpecializationThreshold; i++)
            {
                if (useSpan)
                {
                    object[] arguments = { i };
                    Assert.Null(invoker.Invoke(target, arguments.AsSpan()));
                    Assert.Equal(i + 1, arguments[0]);
                }
                else
                {
                    Assert.Null(invoker.Invoke(target, i));
                }

                Assert.Equal(i, target.Value);
            }
        }

        public sealed class RefConstructorTarget
        {
            public int Value;

            public RefConstructorTarget(ref int value)
            {
                Value = value;
                value++;
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/134901", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/134903", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void Constructor_AbstractDeclaringTypeWithExistingInstance()
        {
            ConstructorInfo constructor = typeof(AbstractRefConstructorTarget).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new[] { typeof(int).MakeByRefType() },
                modifiers: null)!;
            MethodInvoker invoker = MethodInvoker.Create(constructor);
            int initialValue = -1;
            var target = new ConcreteRefConstructorTarget(ref initialValue);
            object?[] arguments = { 42 };

            Assert.Null(invoker.Invoke(target, arguments.AsSpan()));
            Assert.Equal(42, target.Value);
            Assert.Equal(43, arguments[0]);

            object?[] constructorArguments = { 84 };
            Assert.Null(constructor.Invoke(target, constructorArguments));
            Assert.Equal(84, target.Value);
            Assert.Equal(85, constructorArguments[0]);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/134901", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/134903", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void Constructor_AbstractDeclaringTypeWithExistingInstance_RegularArguments()
        {
            ConstructorInfo constructor = typeof(AbstractRegularConstructorTarget).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new[] { typeof(int) },
                modifiers: null)!;
            MethodInvoker invoker = MethodInvoker.Create(constructor);
            var target = new ConcreteRegularConstructorTarget(0);

            Assert.Null(invoker.Invoke(target, 42));
            Assert.Equal(42, target.Value);
            Assert.Null(invoker.Invoke(target, 43));
            Assert.Equal(43, target.Value);

            object?[] arguments = { 44 };
            Assert.Null(invoker.Invoke(target, arguments.AsSpan()));
            Assert.Equal(44, target.Value);
            arguments[0] = 45;
            Assert.Null(invoker.Invoke(target, arguments.AsSpan()));
            Assert.Equal(45, target.Value);

            Assert.Throws<MemberAccessException>(() => invoker.Invoke(null, 46));
            object?[] nullTargetArguments = { 47 };
            Assert.Throws<MemberAccessException>(() => invoker.Invoke(null, nullTargetArguments.AsSpan()));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/134901", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/134903", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void Constructor_AbstractDeclaringTypeWithExistingInstance_ManyRegularArguments()
        {
            ConstructorInfo constructor = typeof(AbstractRegularConstructorTarget).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                new[]
                {
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(int),
                    typeof(int)
                },
                modifiers: null)!;
            MethodInvoker invoker = MethodInvoker.Create(constructor);
            var target = new ConcreteRegularConstructorTarget(0);
            object?[] arguments = { 1, 2, 3, 4, 5 };

            Assert.Null(invoker.Invoke(target, arguments.AsSpan()));
            Assert.Equal(15, target.Value);
            arguments[0] = 10;
            Assert.Null(invoker.Invoke(target, arguments.AsSpan()));
            Assert.Equal(24, target.Value);
        }

        public abstract class AbstractRefConstructorTarget
        {
            public int Value;

            protected AbstractRefConstructorTarget(ref int value)
            {
                Value = value;
                value++;
            }
        }

        public sealed class ConcreteRefConstructorTarget : AbstractRefConstructorTarget
        {
            public ConcreteRefConstructorTarget(ref int value) : base(ref value)
            {
            }
        }

        public abstract class AbstractRegularConstructorTarget
        {
            public int Value;

            protected AbstractRegularConstructorTarget(int value)
            {
                Value = value;
            }

            protected AbstractRegularConstructorTarget(int a, int b, int c, int d, int e)
            {
                Value = a + b + c + d + e;
            }
        }

        public sealed class ConcreteRegularConstructorTarget : AbstractRegularConstructorTarget
        {
            public ConcreteRegularConstructorTarget(int value) : base(value)
            {
            }
        }

        [Fact]
        public void NullTypeValidation()
        {
            Assert.Throws<ArgumentNullException>(() => MethodInvoker.Create(null));
        }

        [Fact]
        public void Args_0()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_0)));
            Assert.Equal("0", invoker.Invoke(obj: null));
            Assert.Equal("0", invoker.Invoke(obj: null, new Span<object?>()));
        }

        [Fact]
        public void Args_1()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_1)));
            Assert.Equal("1", invoker.Invoke(obj: null, "1"));
            Assert.Equal("1", invoker.Invoke(obj: null, new Span<object?>(new object[] { "1" })));
        }

        [Fact]
        public void Args_2()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_2)));
            Assert.Equal("12", invoker.Invoke(obj: null, "1", "2"));
            Assert.Equal("12", invoker.Invoke(obj: null, new Span<object?>(new object[] { "1", "2" })));
        }

        [Fact]
        public void Args_3()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_3)));
            Assert.Equal("123", invoker.Invoke(obj: null, "1", "2", "3"));
            Assert.Equal("123", invoker.Invoke(obj: null, new Span<object?>(new object[] { "1", "2", "3" })));
        }

        [Fact]
        public void Args_4()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_4)));
            Assert.Equal("1234", invoker.Invoke(obj: null, "1", "2", "3", "4"));
            Assert.Equal("1234", invoker.Invoke(obj: null, new Span<object?>(new object[] { "1", "2", "3", "4" })));
        }

        [Fact]
        public void Args_5()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_5)));
            Assert.Equal("12345", invoker.Invoke(obj: null, new Span<object?>(new object[] { "1", "2", "3", "4", "5" })));
        }

        [Fact]
        public void Args_0_Extra_Throws()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_0)));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(obj: null, 42));
        }

        [Fact]
        public void Args_1_Extra_Throws()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_1)));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(obj: null, "1", 42));
        }

        [Fact]
        public void Args_2_Extra_Throws()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_2)));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(obj: null, "1", "2", 42));
        }

        [Fact]
        public void Args_3_Extra_Throws()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_3)));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(obj: null, "1", "2", "3", 42));
        }

        [Fact]
        public void Args_Span_Extra_Throws()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_1)));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(obj: null, new Span<object?>(new object[] { "1", "2" })));
        }

        [Fact]
        public void Args_1_NotEnoughArgs_Throws()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_1)));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(obj: null));
        }

        [Fact]
        public void Args_2_NotEnoughArgs_Throws()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_2)));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(obj: null));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke("1"));
        }

        [Fact]
        public void Args_3_NotEnoughArgs_Throws()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_3)));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(obj: null));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(obj: null, "1"));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(obj: null, "1", "2"));
        }

        [Fact]
        public void Args_Span_NotEnoughArgs_Throws()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_1)));
            Assert.Throws<TargetParameterCountException>(() => invoker.Invoke(obj: null, new Span<object?>()));
        }

        [Fact]
        public void Args_ByRef()
        {
            string argValue = "Value";
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_ByRef)));

            // Although no copy-back, verify we can call.
            Assert.Equal("Hello", invoker.Invoke(obj: null, argValue));

            // The Span version supports copy-back.
            object[] args = new object[] { argValue };
            invoker.Invoke(obj: null, new Span<object?>(args));
            Assert.Equal("Hello", args[0]);

            args[0] = null;
            invoker.Invoke(obj: null, new Span<object?>(args));
            Assert.Equal("Hello", args[0]);
        }

        [Fact]
        public unsafe void Args_Pointer()
        {
            int i = 7;
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_ByPointer)));

            invoker.Invoke(obj: null, (IntPtr)(void*)&i);
            Assert.Equal(8, i);

            object[] args = new object[] { (IntPtr)(void*)&i };
            invoker.Invoke(obj: null, new Span<object?>(args));
            Assert.Equal(9, i);
        }

        [Fact]
        public unsafe void Args_SystemPointer()
        {
            int i = 7;
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Args_BySystemPointer)));

            object pointer = Pointer.Box(&i, typeof(int).MakePointerType());
            invoker.Invoke(obj: null, pointer);
            Assert.Equal(8, i);

            object[] args = new object[] { pointer };
            invoker.Invoke(obj: null, new Span<object?>(args));
            Assert.Equal(9, i);
        }

        [Theory]
        [MemberData(nameof(Invoke_TestData))]
        public void ArgumentConversions(Type methodDeclaringType, string methodName, object obj, object[] parameters, object result)
        {
            MethodInvoker invoker = MethodInvoker.Create(GetMethod(methodDeclaringType, methodName));

            // Adapt the input since Type.Missing is not supported, and Span<object> requires an object[] array (e.g. not string[]).
            if (parameters is null)
            {
                Assert.Equal(result, invoker.Invoke(obj, new Span<object?>()));
                Assert.Equal(result, invoker.Invoke(obj));
            }
            else if (HasTypeMissing())
            {
                if (parameters.GetType().GetElementType() == typeof(object))
                {
                    Assert.Throws<ArgumentException>(() => invoker.Invoke(obj, new Span<object?>(parameters)));
                }
                else
                {
                    // Using 'string[]', for example, is not supported with Span<object>.
                    Assert.Throws<ArrayTypeMismatchException>(() => invoker.Invoke(obj, new Span<object?>(parameters)));
                }
            }
            else
            {
                if (parameters.GetType().GetElementType() == typeof(object))
                {
                    Assert.Equal(result, invoker.Invoke(obj, new Span<object?>(parameters)));

                    // Also verify explicit length parameters.
                    switch (parameters.Length)
                    {
                        case 0:
                            Assert.Equal(result, invoker.Invoke(obj));
                            break;
                        case 1:
                            Assert.Equal(result, invoker.Invoke(obj, parameters[0]));
                            break;
                        case 2:
                            Assert.Equal(result, invoker.Invoke(obj, parameters[0], parameters[1]));
                            break;
                        case 3:
                            Assert.Equal(result, invoker.Invoke(obj, parameters[0], parameters[1], parameters[2]));
                            break;
                        case 4:
                            Assert.Equal(result, invoker.Invoke(obj, parameters[0], parameters[1], parameters[2], parameters[3]));
                            break;
                    }
                }
                else
                {
                    Assert.Throws<ArrayTypeMismatchException>(() => invoker.Invoke(obj, new Span<object?>(parameters)));
                }
            }

            bool HasTypeMissing()
            {
                if (parameters is not null)
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (ReferenceEquals(parameters[i], Type.Missing))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        [Fact]
        public void ThrowsNonWrappedException_0()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Throw_0)));
            Assert.Throws<InvalidOperationException>(() => invoker.Invoke(obj: null));
            Assert.Throws<InvalidOperationException>(() => invoker.Invoke(obj: null, new Span<object?>()));
        }

        [Fact]
        public void ThrowsNonWrappedException_1()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Throw_1)));
            Assert.Throws<InvalidOperationException>(() => invoker.Invoke(obj: null, "1"));
            Assert.Throws<InvalidOperationException>(() => invoker.Invoke(obj: null, new Span<object?>(new object[] { "1" })));
        }

        [Fact]
        public void ThrowsNonWrappedException_5()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.Throw_5)));
            Assert.Throws<InvalidOperationException>(() => invoker.Invoke(obj: null, new Span<object?>(new object[] { "1", "2", "3", "4", "5" })));
        }

        [Fact]
        public void VerifyThisObj_WrongType()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.VerifyThisObj)));
            Assert.Throws<TargetException>(() => invoker.Invoke(obj: 42));
        }

        [Fact]
        public void VerifyThisObj_Null()
        {
            MethodInvoker invoker = MethodInvoker.Create(typeof(TestClass).GetMethod(nameof(TestClass.VerifyThisObj)));
            Assert.Throws<TargetException>(() => invoker.Invoke(obj: null));
        }

        public static IEnumerable<object[]> Invoke_TestData() => MethodInfoTests.Invoke_TestData();

        private sealed class CachedInvokerTarget
        {
            internal int CallCount { get; private set; }

            public static bool TryGetValue(CachedInvokerTarget target, out object result)
            {
                result = target.Echo(target);
                return true;
            }

            public object Echo(object value)
            {
                if (CallCount++ == 0)
                {
                    GC.Collect();
                }

                return value;
            }
        }

        private class TestClass
        {
            private int _i = 42;

            public static string Args_0() => "0";
            public static string Args_1(string arg) => arg;
            public static string Args_2(string arg1, string arg2) => arg1 + arg2;
            public static string Args_3(string arg1, string arg2, string arg3) => arg1 + arg2 + arg3;
            public static string Args_4(string arg1, string arg2, string arg3, string arg4) => arg1 + arg2 + arg3 + arg4;
            public static string Args_5(string arg1, string arg2, string arg3, string arg4, string arg5) => arg1 + arg2 + arg3 + arg4 + arg5;

            public static string Args_ByRef(ref string arg)
            {
                arg = "Hello";
                return arg;
            }

            public static unsafe void Args_ByPointer(int* arg)
            {
                *arg = (*arg) +1;
            }

            public static unsafe void Args_BySystemPointer(Pointer arg)
            {
                int* p = (int*)Pointer.Unbox(arg);
                *p = (*p) + 1;
            }

            public static int TypeMissing(int i = 42)
            {
                return i;
            }

            public void VerifyThisObj()
            {
                Assert.Equal(42, _i);
            }

            public static void Throw_0() =>
                throw new InvalidOperationException();
            public static void Throw_1(string arg1) =>
                throw new InvalidOperationException();
            public static void Throw_5(string arg1, string arg2, string arg3, string arg4, string arg5) =>
                throw new InvalidOperationException();
        }

    }
}
