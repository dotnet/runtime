// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_76051
{
    [Fact]
    public static int TestEntryPoint()
    {
        GetIndex(1);
        return 100;
    }

    // This tests an assertion failure (debug)/segfault (release) in
    // fgMorphModToSubMulDiv due to the use of gtClone that fails for the
    // complex tree seen for the address-of expression.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe int GetIndex(uint cellCount) => (int)((ulong)&cellCount % cellCount);
}
