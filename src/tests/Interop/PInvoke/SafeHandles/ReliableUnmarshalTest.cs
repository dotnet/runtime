// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace SafeHandleTests
{
    public class ReliableUnmarshalTest
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/48084", TestRuntimes.Mono)]
        public static void RunTest()
        {
            // Test that our SafeHandle-derived object has its underlying handle set after a P/Invoke
            // even if there's an exception during the unmarshal phase.
            IntPtr value = (IntPtr)123;
            TestSafeHandle h = new TestSafeHandle();

            Assert.Throws<InvalidOperationException>(() => SafeHandleNative.GetHandleAndCookie(out _, value, out h));

            Assert.Equal(value, h.DangerousGetHandle());

            // Try again, this time triggering unmarshal failure with an array.
            value = (IntPtr)456;
            h = new TestSafeHandle();

            Assert.Throws<OverflowException>(() => SafeHandleNative.GetHandleAndArray(out _, out _, value, out h));

            Assert.Equal(value, h.DangerousGetHandle());
        }
    }
}
