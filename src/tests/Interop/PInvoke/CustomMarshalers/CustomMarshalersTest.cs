// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace PInvokeTests
{
    static class CustomMarshalersNative
    {
        [DllImport(nameof(CustomMarshalersNative))]
        public static extern void Unsupported(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Runtime.InteropServices.CustomMarshalers.TypeToTypeInfoMarshaler")]
            Type type
        );

        [DllImport(nameof(CustomMarshalersNative))]
        public static extern void Unsupported(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "System.Runtime.InteropServices.CustomMarshalers.ExpandoToDispatchExMarshaler")]
            IReflect expando
        );
    }

    public static class CustomMarshalersTests
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        [SkipOnMono("These custom marshallers were never built-in to the runtime on Mono")]
        [ActiveIssue("https://github.com/dotnet/runtimelab/issues/155", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
        public static int TestEntryPoint()
        {
            try
            {
                Assert.Throws<PlatformNotSupportedException>(() => CustomMarshalersNative.Unsupported(typeof(object)));
                Assert.Throws<PlatformNotSupportedException>(() => CustomMarshalersNative.Unsupported((IReflect)typeof(object)));
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.ToString());
                return 101;
            }

            return 100;
        }
    }
}
