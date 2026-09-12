// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class TestTLS
{
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/133538", RuntimeConfiguration.Checked)]
    [Fact]
    public static int TestEntryPoint()
    {
        return CallTest(null);
    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Test")]
    extern static int CallTest([UnsafeAccessorType("TlsTest, TestTLSNative")] object? a);
}
