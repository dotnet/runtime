// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
