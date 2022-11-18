// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class Runtime_57606
{
    public struct CompositeType16Bytes
    {
        public ulong _0;
        public ulong _1;
    }

    public struct CompositeTypeMoreThan16Bytes
    {
        public ulong _0;
        public ulong _1;
        public byte _2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static CompositeTypeMoreThan16Bytes ReturnsViaBuffer(int x1, int x2, int x3, int x4, int x5, int x6, CompositeType16Bytes x7Stack, __arglist)
    {
        // Note that VarArgHnd is passed in register x0
        // When allocating a parameter of CompositeType16Bytes to registers and stack
        // NGRN = 7 and since the value can not be allocated to a single GP register
        // the JIT splits the value between x7 and stack.

        CompositeTypeMoreThan16Bytes r = default;
        r._2 = (byte)(x1 + x2 + x3 + x4 + x5 + x6 + (int)x7Stack._0 + (int)x7Stack._1 + 79);
        return r;
    }

    public static int Main()
    {
        CompositeTypeMoreThan16Bytes r = ReturnsViaBuffer(1, 2, 3, 4, 5, 6, default(CompositeType16Bytes), __arglist());
        return r._2;
    }
}
