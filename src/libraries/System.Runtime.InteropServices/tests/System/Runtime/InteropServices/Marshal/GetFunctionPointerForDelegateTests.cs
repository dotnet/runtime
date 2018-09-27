// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetFunctionPointerForDelegateTests
    {
        [Fact]
        public void GetFunctionPointerForDelegate_NormalDelegateNonGeneric_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method));
            Delegate d = targetMethod.CreateDelegate(typeof(NonGenericDelegate));

            IntPtr pointer1 = Marshal.GetFunctionPointerForDelegate(d);
            IntPtr pointer2 = Marshal.GetFunctionPointerForDelegate(d);
            Assert.NotEqual(IntPtr.Zero, pointer1);
            Assert.Equal(pointer1, pointer2);
        }

        [Fact]
        public void GetFunctionPointerForDelegate_MarshalledDelegateNonGeneric_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method));
            Delegate original = targetMethod.CreateDelegate(typeof(NonGenericDelegate));
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(original);
            Delegate d = Marshal.GetDelegateForFunctionPointer<NonGenericDelegate>(ptr);
            GC.KeepAlive(original);

            IntPtr pointer1 = Marshal.GetFunctionPointerForDelegate(d);
            IntPtr pointer2 = Marshal.GetFunctionPointerForDelegate(d);
            Assert.NotEqual(IntPtr.Zero, pointer1);
            Assert.Equal(ptr, pointer1);
            Assert.Equal(pointer1, pointer2);
        }

        [Fact]
        public void GetFunctionPointerForDelegate_NormalDelegateGeneric_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method));
            NonGenericDelegate d = (NonGenericDelegate)targetMethod.CreateDelegate(typeof(NonGenericDelegate));

            IntPtr pointer1 = Marshal.GetFunctionPointerForDelegate(d);
            IntPtr pointer2 = Marshal.GetFunctionPointerForDelegate(d);
            Assert.NotEqual(IntPtr.Zero, pointer1);
            Assert.Equal(pointer1, pointer2);
        }

        [Fact]
        public void GetFunctionPointerForDelegate_MarshalledDelegateGeneric_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method));
            Delegate original = targetMethod.CreateDelegate(typeof(NonGenericDelegate));
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(original);
            NonGenericDelegate d = Marshal.GetDelegateForFunctionPointer<NonGenericDelegate>(ptr);
            GC.KeepAlive(original);
            
            IntPtr pointer1 = Marshal.GetFunctionPointerForDelegate(d);
            IntPtr pointer2 = Marshal.GetFunctionPointerForDelegate(d);
            Assert.NotEqual(IntPtr.Zero, pointer1);
            Assert.Equal(ptr, pointer1);
            Assert.Equal(pointer1, pointer2);
        }

        [Fact]
        public void GetFunctionPointerForDelegate_NullDelegate_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("d", () => Marshal.GetFunctionPointerForDelegate(null));
            AssertExtensions.Throws<ArgumentNullException>("d", () => Marshal.GetFunctionPointerForDelegate<string>(null));
        }

        [Fact]
        public void GetFunctionPointerForDelegate_ObjectNotDelegate_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => Marshal.GetFunctionPointerForDelegate(10));
        }

        [Fact]
        public void GetFunctionPointer_GenericDelegate_ThrowsArgumentException()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method));
            Delegate d = targetMethod.CreateDelegate(typeof(GenericDelegate<string>));
            AssertExtensions.Throws<ArgumentException>("delegate", () => Marshal.GetFunctionPointerForDelegate(d));
        }

        public delegate void GenericDelegate<T>(T t);
        public delegate void NonGenericDelegate(string t);

        public static void Method(string s) { }
    }
}
