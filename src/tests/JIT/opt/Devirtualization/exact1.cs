// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

// The jit should be able to inline M() if it gets the
// exact context G<string>

class G<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual bool M() => typeof(T) == typeof(string);
}

class Program
{
    static int Main() => new G<string>().M() ? 100 : -1;
}
