// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

unsafe class Runtime_72265
{
    private static int Main()
    {
        var unmanaged = ((delegate* unmanaged<StructWithIndex>)&GetStructUnmanaged)();
        var managed = GetStructManaged();

        return !unmanaged.Equals(managed) ? 101 : 100;
    }

    [UnmanagedCallersOnly]
    private static StructWithIndex GetStructUnmanaged()
    {
        return GetStructManaged();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static StructWithIndex GetStructManaged() => new StructWithIndex { Index = 10, Value = 11 };

    struct StructWithIndex
    {
        public int Index;
        public int Value;
    }
}
