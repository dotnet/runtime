// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

public class MultipleAssembliesWithSamePInvokeTest
{
    [DllImport(@"MAWSPINative", CallingConvention = CallingConvention.StdCall)]
    private static extern int GetInt();

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void Test()
    {
        Assert.Equal(24, GetInt());
        Assert.Equal(24, ManagedDll1.Class1.GetInt());
        Assert.Equal(24, ManagedDll2.Class2.GetInt());
    }
}
