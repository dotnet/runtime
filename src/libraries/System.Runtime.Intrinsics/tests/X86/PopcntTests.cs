// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class PopcntTests
{
    private static bool RunTests => Popcnt.IsSupported;
    private static bool Run32BitTests => RunTests && PlatformDetection.Is32BitProcess;
    private static bool Run64BitTests => RunTests && PlatformDetection.Is64BitProcess;

    [Fact]
    public void TestReflectionCalling()
    {
        if (RunTests)
        {
            ReflectionTester.Test(typeof(Popcnt));
        }
        else
        {
            Assert.Throws<PlatformNotSupportedException>(() => ReflectionTester.Test(typeof(Popcnt)));
        }
    }
}
