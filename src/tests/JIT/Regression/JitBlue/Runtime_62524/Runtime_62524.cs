// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_62524
{
    [Fact]
    public static int TestEntryPoint()
    {
        return Problem(new() { Value = 1 }) == 1 ? 100 : 101;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Problem(StructWithIndex a)
    {
        bool k = a.Value == 1;
        a = default;
        if (k)
        {
            return 1;
        }

        return a.Index;
    }
}

public struct StructWithIndex
{
    public int Index;
    public int Value;
}
