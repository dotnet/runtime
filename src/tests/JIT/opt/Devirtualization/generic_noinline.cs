// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class Program
{
    static int Main()
    {
        MyStruct<Atom> s = default;

        // RyuJIT can devirtualize this, but NoInlining prevents the inline
        // This checks that we properly pass the instantiation context to the shared generic method.
        return ((IFoo)s).GetTheType() == typeof(Atom) ? 100 : -1;
    }
}

interface IFoo
{
    Type GetTheType();
}

struct MyStruct<T> : IFoo
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    Type IFoo.GetTheType() => typeof(T);
}

class Atom { }
