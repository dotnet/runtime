// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

class B
{
    public virtual int F() => 33;
}

sealed class D : B
{
    public override int F() => 44;
}

class X
{
    volatile static bool p;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static B GB() => new B();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static D GD() => new D();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static B G() => p ? GD() : GB();

    public static int Main()
    {
        p = false;
        // After inlining G(), the jit must not update
        // the type of the return spill temp for G(), or it 
        // may incorrectly devirtualize the call to F()
        return G().F() + 67;
    }
}
