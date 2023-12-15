// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is the C# skeleton that was used to build Reabstraction.il. 
// The only difference is to change the BarClass and BarStruct types
// to implement IBaz instead of IBar

using System;
using Xunit;

class VirtualStaticMethodReabstraction
{
    static int Main()
    {
        Assert.Throws<EntryPointNotFoundException>(() => { Call<BarStruct>(); });
        Assert.Throws<EntryPointNotFoundException>(() => { Call<BarClass>(); });
        Assert.Throws<EntryPointNotFoundException>(() => { Call<BarStruct, BarClass>(); });
        Assert.Throws<EntryPointNotFoundException>(() => { Call<BarStruct, BarStruct>(); });
        Assert.Throws<EntryPointNotFoundException>(() => { Call<BarClass, BarClass>(); });
        Assert.Throws<EntryPointNotFoundException>(() => { Call<BarClass, BarStruct>(); });

        Assert.Throws<EntryPointNotFoundException>(() => { GetAction<BarStruct>()(); });
        Assert.Throws<EntryPointNotFoundException>(() => { GetAction<BarClass>()(); });
        Assert.Throws<EntryPointNotFoundException>(() => { GetAction<BarStruct, BarClass>()(); });
        Assert.Throws<EntryPointNotFoundException>(() => { GetAction<BarStruct, BarStruct>()(); });
        Assert.Throws<EntryPointNotFoundException>(() => { GetAction<BarClass, BarClass>()(); });
        Assert.Throws<EntryPointNotFoundException>(() => { GetAction<BarClass, BarStruct>()(); });

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
        static abstract void IFoo.Frob();
        static abstract void IFoo.Frob<Z>();
    }

    class BarClass : IBar
    {
    }

    struct BarStruct : IBar
    {
    }
}
