// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is the C# skeleton that was used to build AmbiguousImplementationException.il. 
// The only difference is to change the BarClass and BarStruct types
// to implement IBaz instead of IBoring

using System;
using System.Runtime;
using Xunit;

class VirtualStaticMethodReabstraction
{
    static int Main()
    {
        Assert.Throws<AmbiguousImplementationException>(() => { Call<BarStruct>(); });
        Assert.Throws<AmbiguousImplementationException>(() => { Call<BarClass>(); });
        Assert.Throws<AmbiguousImplementationException>(() => { Call<BarStruct, BarClass>(); });
        Assert.Throws<AmbiguousImplementationException>(() => { Call<BarStruct, BarStruct>(); });
        Assert.Throws<AmbiguousImplementationException>(() => { Call<BarClass, BarClass>(); });
        Assert.Throws<AmbiguousImplementationException>(() => { Call<BarClass, BarStruct>(); });

        Assert.Throws<AmbiguousImplementationException>(() => { GetAction<BarStruct>()(); });
        Assert.Throws<AmbiguousImplementationException>(() => { GetAction<BarClass>()(); });
        Assert.Throws<AmbiguousImplementationException>(() => { GetAction<BarStruct, BarClass>()(); });
        Assert.Throws<AmbiguousImplementationException>(() => { GetAction<BarStruct, BarStruct>()(); });
        Assert.Throws<AmbiguousImplementationException>(() => { GetAction<BarClass, BarClass>()(); });
        Assert.Throws<AmbiguousImplementationException>(() => { GetAction<BarClass, BarStruct>()(); });

        return 100;
    }

    static void Call<T>() where T : IFoo => T.Frob();
    static void Call<T, U>() where T : IFoo => T.Frob<U>();

    static Action GetAction<T>() where T : IFoo => T.Frob;
    static Action GetAction<T, U>() where T : IFoo => T.Frob<U>;

    interface IFoo
    {
        static virtual void Frob() => throw null;
        static virtual void Frob<Z>() => throw null;
    }

    interface IBar : IFoo
    {
        static void IFoo.Frob() => throw null;
        static void IFoo.Frob<Z>() => throw null;
    }

    interface IBaz : IFoo
    {
        static void IFoo.Frob() => throw null;
        static void IFoo.Frob<Z>() => throw null;
    }

    interface IBoring
    {
    }

    class BarClass : IBar, IBoring
    {
    }

    struct BarStruct : IBar, IBoring
    {
    }
}
