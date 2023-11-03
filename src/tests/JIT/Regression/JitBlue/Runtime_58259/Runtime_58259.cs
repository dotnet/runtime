// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
public unsafe class Runtime_58259
{
    [Fact]
    public static void TestEntryPoint()
    {
        M(out _);
    }

    static delegate* unmanaged<out int, void> _f;

    internal static void M(out int index)
    {
        if (_f != null)
        {
            _f(out index);
            _f(out index);
        }
        else
        {
            index = 0;
        }
    }
}

