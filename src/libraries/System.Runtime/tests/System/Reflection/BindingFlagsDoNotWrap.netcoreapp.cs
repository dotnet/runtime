// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit;

namespace System.Reflection.Tests
{
    public static class BindingFlagsDoNotWrapTests
    {
        [Fact]
        public static void MethodInvoke()
        {
            MethodInfo m = typeof(TestClass).GetMethod(nameof(TestClass.Moo), BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            TestDoNotWrap<MyException1>((bf) => m.Invoke(null, bf, null, Array.Empty<object>(), null));
        }

        [Fact]
        public static void ConstructorInvoke()
        {
            ConstructorInfo c = typeof(TestClass).GetConstructor(BindingFlags.Public|BindingFlags.Instance, null, Array.Empty<Type>(), null);
            TestDoNotWrap<MyException2>((bf) => c.Invoke(bf, null, Array.Empty<object>(), null));
        }

        [Fact]
        public static void ConstructorInvokeStringCtor()
        {
            // Code coverage: Project N - String constructors go through a separate code path.
            ConstructorInfo c = typeof(string).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(char[]), typeof(int), typeof(int) }, null);
            TestDoNotWrap<ArgumentNullException>((bf) => c.Invoke(bf, null, new object[] { null, 0, 0 }, null));
        }

        [Fact]
        public static void ConstructorInvokeUsingMethodInfoInvoke()
        {
            ConstructorInfo c = typeof(TestClass).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Array.Empty<Type>(), null);
            TestDoNotWrap<MyException2>((bf) => c.Invoke(new TestClass(0), bf, null, Array.Empty<object>(), null));
        }

        [Fact]
        public static void PropertyGet()
        {
            PropertyInfo p = typeof(TestClass).GetProperty(nameof(TestClass.MyProperty));
            TestDoNotWrap<MyException3>((bf) => p.GetValue(null, bf, null, null, null));
        }

        [Fact]
        public static void PropertySet()
        {
            PropertyInfo p = typeof(TestClass).GetProperty(nameof(TestClass.MyProperty));
            TestDoNotWrap<MyException4>((bf) => p.SetValue(null, 42, bf, null, null, null));
        }

        [Fact]
        public static void InvokeMember_Method()
        {
            Type t = typeof(TestClass);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.InvokeMethod;
            TestDoNotWrap<MyException1>((bf) => t.InvokeMember(nameof(TestClass.Moo), bf | flags, null, null, Array.Empty<object>(), null, null, null));
        }

        [Fact]
        public static void InvokeMember_PropertyGet()
        {
            Type t = typeof(TestClass);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.GetProperty;
            TestDoNotWrap<MyException3>((bf) => t.InvokeMember(nameof(TestClass.MyProperty), bf | flags, null, null, Array.Empty<object>(), null, null, null));
        }

        [Fact]
        public static void InvokeMember_PropertySet()
        {
            Type t = typeof(TestClass);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.SetProperty;
            TestDoNotWrap<MyException4>((bf) => t.InvokeMember(nameof(TestClass.MyProperty), bf | flags, null, null, new object[] { 42 }, null, null, null));
        }

        [Fact]
        public static void ActivatorCreateInstance()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            TestDoNotWrap<MyException2>((bf) => Activator.CreateInstance(typeof(TestClass), bf | flags, null, Array.Empty<object>(), null, null));
        }

        [Fact]
        public static void ActivatorCreateInstanceOneArgument()
        {
            // For code coverage on CoreCLR: Activator.CreateInstance with parameters uses different code path from one without parameters.
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            TestDoNotWrap<MyException5>((bf) => Activator.CreateInstance(typeof(TestClass), bf | flags, null, new object[] { "Hello" }, null, null));
        }

        [Fact]
        public static void ActivatorCreateInstanceCaching()
        {
            // For code coverage on CoreCLR: Activator.CreateInstance - second call after non-throwing call goes through a different path
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            TestClassConditional.ThrowInConstructor = true;
            TestDoNotWrap<MyException6>((bf) => Activator.CreateInstance(typeof(TestClassConditional), bf | flags, null, Array.Empty<object>(), null, null));

            TestClassConditional.ThrowInConstructor = false;
            Assert.True(Activator.CreateInstance(typeof(TestClassConditional), flags, null, Array.Empty<object>(), null, null) is TestClassConditional);

            TestClassConditional.ThrowInConstructor = true;
            TestDoNotWrap<MyException6>((bf) => Activator.CreateInstance(typeof(TestClassConditional), bf | flags, null, Array.Empty<object>(), null, null));
        }

        [Fact]
        public static void ActivatorCreateInstance_BadCCtor()
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            TestDoNotWrap<TypeInitializationException>((bf) => Activator.CreateInstance(typeof(TestClassBadCCtor), bf | flags, null, Array.Empty<object>(), null, null));
        }

        [Fact]
        public static void AssemblyCreateInstance()
        {
            Assembly a = typeof(TestClass).Assembly;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            TestDoNotWrap<MyException2>((bf) => a.CreateInstance(typeof(TestClass).FullName, false, bf | flags, null, Array.Empty<object>(), null, null));
        }

        [Fact]
        public static void AssemblyCreateInstance_BadCCtor()
        {
            Assembly a = typeof(TestClass).Assembly;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            TestDoNotWrap<TypeInitializationException>((bf) => a.CreateInstance(typeof(TestClassBadCCtor).FullName, false, bf | flags, null, Array.Empty<object>(), null, null));
        }

        private static void TestDoNotWrap<T>(Action<BindingFlags> action) where T : Exception
        {
            Assert.Throws<T>(() => action(BindingFlags.DoNotWrapExceptions));
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => action(default(BindingFlags)));
            Assert.Equal(typeof(T), tie.InnerException.GetType());
        }

        private sealed class TestClass
        {
            public TestClass() => throw new MyException2();
            public TestClass(int _) { }
            public TestClass(string s) => throw new MyException5();
            public static void Moo() => throw new MyException1();
            public static int MyProperty { get { throw new MyException3(); } set { throw new MyException4(); } }
        }

        public sealed class TestClassBadCCtor
        {
            public TestClassBadCCtor() { }

            static TestClassBadCCtor()
            {
                throw new MyException1();
            }
        }

        private sealed class TestClassConditional
        {
            public TestClassConditional() { if (ThrowInConstructor) throw new MyException6(); }

            public static bool ThrowInConstructor;
        }

        private sealed class MyException1 : Exception { } 
        private sealed class MyException2 : Exception { }
        private sealed class MyException3 : Exception { }
        private sealed class MyException4 : Exception { }
        private sealed class MyException5 : Exception { }
        private sealed class MyException6 : Exception { }
    }
}
