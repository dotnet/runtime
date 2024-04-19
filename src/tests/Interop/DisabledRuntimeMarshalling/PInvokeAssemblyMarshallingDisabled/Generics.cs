// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;
using static DisabledRuntimeMarshallingNative;

namespace DisabledRuntimeMarshalling;

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
public unsafe class Generics
{
    [Fact]
    public static void BlittableGeneric_NotSupported()
    {
        Assert.Throws<MarshalDirectiveException>(() =>
        {
            Nullable<int> s = default;
            DisabledRuntimeMarshallingNative.CallWith(s);
        });
        Assert.Throws<MarshalDirectiveException>(() =>
        {
            Span<int> s = default;
            DisabledRuntimeMarshallingNative.CallWith(s);
        });
        Assert.Throws<MarshalDirectiveException>(() =>
        {
            ReadOnlySpan<int> ros = default;
            DisabledRuntimeMarshallingNative.CallWith(ros);
        });
        Assert.Throws<MarshalDirectiveException>(() =>
        {
            Vector64<int> v = default;
            DisabledRuntimeMarshallingNative.CallWith(v);
        });
        Assert.Throws<MarshalDirectiveException>(() =>
        {
            Vector128<int> v = default;
            DisabledRuntimeMarshallingNative.CallWith(v);
        });
        Assert.Throws<MarshalDirectiveException>(() =>
        {
            Vector256<int> v = default;
            DisabledRuntimeMarshallingNative.CallWith(v);
        });
        Assert.Throws<MarshalDirectiveException>(() =>
        {
            Vector<int> v = default;
            DisabledRuntimeMarshallingNative.CallWith(v);
        });
    }
}
