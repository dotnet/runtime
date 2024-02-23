// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace SafeHandleTests
{
    public class InvalidSafeHandleMarshallingTests
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/48084", TestRuntimes.Mono)]
        public static void RunTest()
        {
            if (TestLibrary.PlatformDetection.IsBuiltInComEnabled)
            {
                // The interface marshaller is only available when COM interop is
                // enabled. The interface marshaller is what initiates the COM
                // interop system which is what subsequently defines defined exception
                // type to throw - matches .NET Framework behavior.
                Assert.Throws<InvalidOperationException>(() => MarshalSafeHandleAsInterface());
            }
            else
            {
                // When the interface marshaller is not available we fallback to
                // the marshalling system which will throw a different exception.
                Assert.Throws<MarshalDirectiveException>(() => MarshalSafeHandleAsInterface());
            }

            Assert.Throws<MarshalDirectiveException>(() => SafeHandleNative.SafeHandle_Invalid(new TestSafeHandle[1]));
            Assert.Throws<TypeLoadException>(() => SafeHandleNative.SafeHandle_Invalid(new SafeHandleNative.StructWithSafeHandleArray()));
        }

        static void MarshalSafeHandleAsInterface()
        {
            SafeHandleNative.SafeHandle_Invalid(new TestSafeHandle());
        }
    }
}
