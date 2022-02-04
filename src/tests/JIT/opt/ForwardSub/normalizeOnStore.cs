// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Note: This test file is the source of the normalizeOnStore.il file. It requires
// InlineIL.Fody to compile. It is not used as anything but a reference of that
// IL file.

using System;
using System.Runtime.CompilerServices;
using InlineIL;

class ForwardSubNormalizeOnStore
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Problem(int a)
    {
        IL.DeclareLocals(new LocalVar(typeof(short)));

        IL.Emit.Ldarg(0);
        IL.Emit.Stloc(0);

        IL.Emit.Ldloc(0);
        return IL.Return<int>();
    }

    public static int Main()
    {
        return Problem(0xF0000 + 100);
    }
}
