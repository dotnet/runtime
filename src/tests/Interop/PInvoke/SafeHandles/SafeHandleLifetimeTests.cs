// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace SafeHandleTests
{
    public class SafeHandleLifetimeTests
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
            Assert.False(testHandle.IsClosed);

            Assert.True(SafeHandleNative.SafeHandleByRef(ref testHandle, initialValue, newValue));
            Assert.False(testHandle.IsClosed);

            testHandle = null;
            SafeHandleNative.SafeHandleOut(out testHandle, initialValue);
            Assert.False(testHandle.IsClosed);

            testHandle = SafeHandleNative.SafeHandleReturn(newValue);
            Assert.False(testHandle.IsClosed);

            testHandle = SafeHandleNative.SafeHandleReturn_Swapped(newValue);
            Assert.False(testHandle.IsClosed);

            var str = new SafeHandleNative.StructWithHandle
            {
                handle = new TestSafeHandle(initialValue)
            };

            SafeHandleNative.StructWithSafeHandleByValue(str, initialValue);
            Assert.False(str.handle.IsClosed);

            SafeHandleNative.StructWithSafeHandleByRef(ref str, initialValue, initialValue);
            Assert.False(str.handle.IsClosed);
        }
    }
}
