// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Runtime_72265;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class Runtime_72265
{
    [Fact]
    [SkipOnMono("https://github.com/dotnet/runtime/issues/72016")]
    public static int TestEntryPoint()
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
