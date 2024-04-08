// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Auto, Size = 16)]
public struct Foo
{
    private int _field;

    [Fact]
    public static unsafe int TestEntryPoint()
    {
        return sizeof(Foo) == 4 ? 100 : -1;
    }
}
