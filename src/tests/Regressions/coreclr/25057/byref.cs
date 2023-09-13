// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

[StructLayout(LayoutKind.Explicit)]
ref struct InvalidRefStruct
{
    [FieldOffset(2)]
    public Span<int> Y;
}

public class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static Type LoadInvalidRefStruct()
    {
        return typeof(InvalidRefStruct);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            LoadInvalidRefStruct();
            return -1;
        }
        catch (TypeLoadException)
        {
            return 100;
        }
    }
}
