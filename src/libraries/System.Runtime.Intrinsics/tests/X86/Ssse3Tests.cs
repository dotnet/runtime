// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Ssse3Tests
{
    private static bool RunTests => Ssse3.IsSupported;
    private static bool Run32BitTests => RunTests && PlatformDetection.Is32BitProcess;
    private static bool Run64BitTests => RunTests && PlatformDetection.Is64BitProcess;

    [Fact]
    public void TestReflectionCalling()
    {
        if (RunTests)
        {
            ReflectionTester.Test(typeof(Ssse3));
        }
        else
        {
            Assert.Throws<PlatformNotSupportedException>(() => ReflectionTester.Test(typeof(Ssse3)));
        }
    }
}
