// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class GetFunctionPointerForDelegateTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoAOT))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/39187", TestPlatforms.Browser)]
        public void GetFunctionPointerForDelegate_NormalDelegateNonGeneric_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = targetMethod.CreateDelegate(typeof(NonGenericDelegate));

            IntPtr pointer1 = Marshal.GetFunctionPointerForDelegate(d);
            IntPtr pointer2 = Marshal.GetFunctionPointerForDelegate(d);
            Assert.NotEqual(IntPtr.Zero, pointer1);
            Assert.Equal(pointer1, pointer2);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoAOT))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/39187", TestPlatforms.Browser)]
        public void GetFunctionPointerForDelegate_MarshalledDelegateNonGeneric_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
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

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoAOT))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/39187", TestPlatforms.Browser)]
        public void GetFunctionPointerForDelegate_NormalDelegateGeneric_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
            NonGenericDelegate d = (NonGenericDelegate)targetMethod.CreateDelegate(typeof(NonGenericDelegate));

            IntPtr pointer1 = Marshal.GetFunctionPointerForDelegate(d);
            IntPtr pointer2 = Marshal.GetFunctionPointerForDelegate(d);
            Assert.NotEqual(IntPtr.Zero, pointer1);
            Assert.Equal(pointer1, pointer2);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoAOT))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/39187", TestPlatforms.Browser)]
        public void GetFunctionPointerForDelegate_MarshalledDelegateGeneric_ReturnsExpected()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
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
        [ActiveIssue("https://github.com/mono/mono/issues/15097", TestRuntimes.Mono)]
        public void GetFunctionPointer_GenericDelegate_ThrowsArgumentException()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate d = targetMethod.CreateDelegate(typeof(GenericDelegate<string>));
            AssertExtensions.Throws<ArgumentException>("delegate", () => Marshal.GetFunctionPointerForDelegate(d));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoAOT))]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser.")]
        public void GetFunctionPointerForDelegate_MarshalledOpenStaticDelegate()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate original = targetMethod.CreateDelegate(typeof(NonGenericDelegate), null);
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(original);
            Assert.NotEqual(IntPtr.Zero, ptr);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoAOT))]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser.")]
        public void GetFunctionPointerForDelegate_MarshalledClosedStaticDelegate()
        {
            MethodInfo targetMethod = typeof(GetFunctionPointerForDelegateTests).GetMethod(nameof(Method), BindingFlags.NonPublic | BindingFlags.Static);
            Delegate original = targetMethod.CreateDelegate(typeof(NoArgsDelegate), "value");
            IntPtr ptr = Marshal.GetFunctionPointerForDelegate(original);
            Assert.NotEqual(IntPtr.Zero, ptr);
        }

        public delegate void GenericDelegate<T>(T t);
        public delegate void NonGenericDelegate(string t);
        public delegate void NoArgsDelegate();

        private static void Method(string s) { }
    }
}
