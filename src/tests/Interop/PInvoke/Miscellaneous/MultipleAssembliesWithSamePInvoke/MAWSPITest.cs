// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using TestLibrary;

public class MultipleAssembliesWithSamePInvokeTest
{
    [DllImport(@"MAWSPINative", CallingConvention = CallingConvention.StdCall)]
    private static extern int GetInt();

    [ActiveIssue("Needs coreclr build", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoFULLAOT))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/82859", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMiniJIT), nameof(PlatformDetection.IsArm64Process))]
    [ActiveIssue("needs triage", TestPlatforms.Android)]
    [ActiveIssue("missing assembly", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]
    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void Test()
    {
        Assert.Equal(24, GetInt());
        Assert.Equal(24, ManagedDll1.Class1.GetInt());
        Assert.Equal(24, ManagedDll2.Class2.GetInt());
    }
}
