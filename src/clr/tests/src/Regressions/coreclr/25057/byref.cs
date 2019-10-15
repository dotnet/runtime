// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
ref struct InvalidRefStruct
{
    [FieldOffset(2)]
    public Span<int> Y;
}

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static Type LoadInvalidRefStruct()
    {
        return typeof(InvalidRefStruct);
    }

    static int Main()
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
