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
        return 100;
    }

    static void Call<T>() where T : IFoo => T.Frob();

    interface IFoo
    {
        static virtual void Frob() => throw null;
    }

    interface IBar : IFoo
    {
        static void IFoo.Frob() => throw null;
    }

    interface IBaz : IFoo
    {
        static abstract void IFoo.Frob();
    }

    class BarClass : IBar
    {
    }

    struct BarStruct : IBar
    {
    }
}
