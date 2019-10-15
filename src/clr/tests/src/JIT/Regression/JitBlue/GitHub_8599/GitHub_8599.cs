// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

// TreeNodeInfoInitCmp attempts to eliminate the cast from cmp(cast<ubyte>(x), icon)
// by narrowing the compare to ubyte. This should only happen if the constant fits in
// a byte so it can be narrowed too, otherwise codegen produces an int sized compare.

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int GetValue() => 301;

    static void Escape(ref int x)
    {
    }

    static int Main()
    {
        if ((byte)GetValue() > 300)
        {
            return -1;
        }

        int x = GetValue();
        Escape(ref x);
        if ((byte)x > 300)
        {
            return -2;
        }

        if ((byte)(GetValue() | 2) > 300)
        {
            return -3;
        }

        return 100;
    }
}
