// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

unsafe class Runtime_73821
{
    public struct S
    {
        public int F;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static S TestMethod(int val)
    {
        S s;
        int size = sizeof(S);
        Unsafe.CopyBlockUnaligned(&s, &val, (uint)size);
        return s;
    }

    static int Main()
    {
        return TestMethod(100).F;
    }
}
