// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Xunit;

namespace System.Reflection.Tests
{
    public class InvokeRefReturnNetcoreTests
    {
        [Theory]
        [MemberData(nameof(RefReturnInvokeTestData))]
        public static void TestRefReturnPropertyGetValue<T>(T value)
        {
            TestRefReturnInvoke<T>(value, (p, t) => p.GetValue(t));
        }

        [Theory]
        [MemberData(nameof(RefReturnInvokeTestData))]
        public static void TestRefReturnMethodInvoke<T>(T value)
        {
            TestRefReturnInvoke<T>(value, (p, t) => p.GetGetMethod().Invoke(t, Array.Empty<object>()));
        }

        [Fact]
        public static void TestRefReturnNullable()
        {
            TestRefReturnInvokeNullable<int>(42);
        }

        [Fact]
        public static void TestRefReturnNullableNoValue()
        {
            TestRefReturnInvokeNullable<int>(default(int?));
        }

        [Theory]
        [MemberData(nameof(RefReturnInvokeTestData))]
        public static void TestNullRefReturnInvoke<T>(T value)
        {
            TestClass<T> tc = new TestClass<T>(value);
            PropertyInfo p = typeof(TestClass<T>).GetProperty(nameof(TestClass<T>.NullRefReturningProp));
            Assert.NotNull(p);
            Assert.Throws<NullReferenceException>(() => p.GetValue(tc));
        }

        [Fact]
        public static unsafe void TestRefReturnOfPointer()
        {
            int* expected = (int*)0x1122334455667788;
            TestClassIntPointer tc = new TestClassIntPointer(expected);
            PropertyInfo p = typeof(TestClassIntPointer).GetProperty(nameof(TestClassIntPointer.RefReturningProp));
            object rv = p.GetValue(tc);
            Assert.True(rv is Pointer);
            int* actual = (int*)(Pointer.Unbox(rv));
            Assert.Equal((IntPtr)expected, (IntPtr)actual);
        }

        [Fact]
        public static unsafe void TestNullRefReturnOfPointer()
        {
            TestClassIntPointer tc = new TestClassIntPointer(null);

            PropertyInfo p = typeof(TestClassIntPointer).GetProperty(nameof(TestClassIntPointer.NullRefReturningProp));
            Assert.NotNull(p);
            Assert.Throws<NullReferenceException>(() => p.GetValue(tc));
        }

        [Fact]
        public static unsafe void TestByRefLikeRefReturn()
        {
            ByRefLike brl = new ByRefLike();
            ByRefLike* pBrl = &brl;
            MethodInfo mi = typeof(TestClass<int>).GetMethod(nameof(TestClass<int>.ByRefLikeRefReturningMethod));
            try
            {
                // Don't use Assert.Throws because that will make a lambda and invalidate the pointer
                object o = mi.Invoke(null, new object[] { Pointer.Box(pBrl, typeof(ByRefLike*)) });

                // If this is reached, it means `o` is a boxed byref-like type. That's a GC hole right there.
                throw new Xunit.Sdk.XunitException("Boxed a byref-like type.");
            }
            catch (NotSupportedException)
            {
                // We expect a NotSupportedException from the Invoke call. Methods returning byref-like types by reference
                // are not reflection invokable.
            }
        }

        public static IEnumerable<object[]> RefReturnInvokeTestData
        {
            get
            {
                yield return new object[] { true };
                yield return new object[] { 'a' };
                yield return new object[] { (byte)42 };
                yield return new object[] { (sbyte)42 };
                yield return new object[] { (ushort)42 };
                yield return new object[] { (short)42 };
                yield return new object[] { 42u };
                yield return new object[] { 42 };
                yield return new object[] { 42UL };
                yield return new object[] { 42L };
                yield return new object[] { 43.67f };
                yield return new object[] { 43.67 };
                // [ActiveIssue("https://github.com/xunit/xunit/issues/1771")]
                //yield return new object[] { new IntPtr(42) };
                //yield return new object[] { new UIntPtr(42) };
                //yield return new object[] { 232953453454m };
                yield return new object[] { BindingFlags.IgnoreCase };
                yield return new object[] { "Hello" };
                yield return new object[] { new object() };
                yield return new object[] { new UriBuilder() };
                yield return new object[] { new int[5] };
                yield return new object[] { new int[5, 5] };
            }
        }

        private static void TestRefReturnInvoke<T>(T value, Func<PropertyInfo, TestClass<T>, object> invoker)
        {
            TestClass<T> tc = new TestClass<T>(value);
            PropertyInfo p = typeof(TestClass<T>).GetProperty(nameof(TestClass<T>.RefReturningProp));
            object rv = invoker(p, tc);
            if (rv != null)
            {
                Assert.Equal(typeof(T), rv.GetType());
            }

            if (typeof(T).IsValueType)
            {
                Assert.Equal(value, rv);
            }
            else
            {
                Assert.Same((object)value, rv);
            }
        }

        private static void TestRefReturnInvokeNullable<T>(T? nullable) where T : struct
        {
            TestClass<T?> tc = new TestClass<T?>(nullable);
            PropertyInfo p = typeof(TestClass<T?>).GetProperty(nameof(TestClass<T?>.RefReturningProp));
            object rv = p.GetValue(tc);
            if (rv != null)
            {
                Assert.Equal(typeof(T), rv.GetType());
            }
            if (nullable.HasValue)
            {
                Assert.Equal(nullable.Value, rv);
            }
            else
            {
                Assert.Null(rv);
            }
        }

        public ref struct ByRefLike { }

        private sealed class TestClass<T>
        {
            private T _value;

            public TestClass(T value) { _value = value; }
            public ref T RefReturningProp => ref _value;
            public unsafe ref T NullRefReturningProp => ref Unsafe.AsRef<T>((void*)null);
            public static unsafe ref ByRefLike ByRefLikeRefReturningMethod(ByRefLike* a) => ref *a;
        }

        private sealed unsafe class TestClassIntPointer
        {
            private int* _value;

            public TestClassIntPointer(int* value) { _value = value; }
            public ref int* RefReturningProp => ref _value;
            public ref int* NullRefReturningProp => ref *(int**)null;
        }
    }
}
