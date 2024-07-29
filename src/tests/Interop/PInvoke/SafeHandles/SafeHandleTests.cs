// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace SafeHandleTests
{
    public class SafeHandleTest
    {
        private static readonly IntPtr initialValue = new IntPtr(458613);
        private static readonly IntPtr newValue = new IntPtr(987185);

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/168", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/48084", TestRuntimes.Mono)]
        public static void RunTest()
        {
            var testHandle = new TestSafeHandle(initialValue);
            Assert.True(SafeHandleNative.SafeHandleByValue(testHandle, initialValue));

            Assert.True(SafeHandleNative.SafeHandleByRef(ref testHandle, initialValue, newValue));
            Assert.Equal(newValue, testHandle.DangerousGetHandle());

            AbstractDerivedSafeHandle abstrHandle = new AbstractDerivedSafeHandleImplementation(initialValue);
            Assert.True(SafeHandleNative.SafeHandleInByRef(abstrHandle, initialValue));
            Assert.Throws<MarshalDirectiveException>(() => SafeHandleNative.SafeHandleByRef(ref abstrHandle, initialValue, newValue));

            NoDefaultConstructorSafeHandle noDefaultCtorHandle = new NoDefaultConstructorSafeHandle(initialValue);
            Assert.Throws<MissingMethodException>(() => SafeHandleNative.SafeHandleByRef(ref noDefaultCtorHandle, initialValue, newValue));

            testHandle = null;
            SafeHandleNative.SafeHandleOut(out testHandle, initialValue);
            Assert.Equal(initialValue, testHandle.DangerousGetHandle());

            testHandle = SafeHandleNative.SafeHandleReturn(newValue);
            Assert.Equal(newValue, testHandle.DangerousGetHandle());

            Assert.Throws<MarshalDirectiveException>(() => SafeHandleNative.SafeHandleReturn_AbstractDerived(initialValue));
            Assert.Throws<MissingMethodException>(() => SafeHandleNative.SafeHandleReturn_NoDefaultConstructor(initialValue));

            var abstractDerivedImplementationHandle = SafeHandleNative.SafeHandleReturn_AbstractDerivedImplementation(initialValue);
            Assert.Equal(initialValue, abstractDerivedImplementationHandle.DangerousGetHandle());

            testHandle = SafeHandleNative.SafeHandleReturn_Swapped(newValue);
            Assert.Equal(newValue, testHandle.DangerousGetHandle());

            Assert.Throws<MarshalDirectiveException>(() => SafeHandleNative.SafeHandleReturn_Swapped_AbstractDerived(initialValue));
            Assert.Throws<MissingMethodException>(() => SafeHandleNative.SafeHandleReturn_Swapped_NoDefaultConstructor(initialValue));

            var str = new SafeHandleNative.StructWithHandle
            {
                handle = new TestSafeHandle(initialValue)
            };

            Assert.True(SafeHandleNative.StructWithSafeHandleByValue(str, initialValue));

            Assert.True(SafeHandleNative.StructWithSafeHandleByRef(ref str, initialValue, initialValue));

            // Cannot change the value of a SafeHandle-derived field in a struct when marshalling byref.
            Assert.Throws<NotSupportedException>(() => SafeHandleNative.StructWithSafeHandleByRef(ref str, initialValue, newValue));

            // Cannot create a SafeHandle-derived field value.
            Assert.Throws<NotSupportedException>(() => SafeHandleNative.StructWithSafeHandleOut(out var defaultOutStruct, initialValue));
        }
    }
}
