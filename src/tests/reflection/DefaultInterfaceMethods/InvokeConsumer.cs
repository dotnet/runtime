// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

class Program
{
    static int Main()
    {
        if ((int)typeof(IFoo).GetMethod("StaticMethod").Invoke(null, new object[] { 1 }) != 31)
            return 1;

        if ((int)typeof(IFoo).GetMethod("DefaultMethod").Invoke(new Fooer(), new object[] { 1 }) != 51)
            return 2;

        if ((int)typeof(IFoo).GetMethod("InstanceMethod").Invoke(new Fooer(), new object[] { 1 }) != 21)
            return 3;

        if (!((RuntimeTypeHandle)typeof(IFoo<Fooer>).GetMethod("StaticMethod").Invoke(null, new object[] { })).Equals(typeof(Fooer[,]).TypeHandle))
            return 11;

        if (!((RuntimeTypeHandle)typeof(IFoo<Fooer>).GetMethod("DefaultMethod").Invoke(new Fooer(), new object[] { })).Equals(typeof(Fooer).TypeHandle))
            return 12;

        if (!((RuntimeTypeHandle)typeof(IFoo<Fooer>).GetMethod("InstanceMethod").Invoke(new Fooer(), new object[] { })).Equals(typeof(Fooer[]).TypeHandle))
            return 13;

        if ((int)typeof(IFoo).GetMethod("DefaultMethod").Invoke(new ValueFooer(), new object[] { 1 }) != 51)
            return 22;

        if ((int)typeof(IFoo).GetMethod("InstanceMethod").Invoke(new ValueFooer(), new object[] { 1 }) != 21)
            return 23;

        if (!((RuntimeTypeHandle)typeof(IFoo<Fooer>).GetMethod("DefaultMethod").Invoke(new ValueFooer(), new object[] { })).Equals(typeof(Fooer).TypeHandle))
            return 32;

        if (!((RuntimeTypeHandle)typeof(IFoo<Fooer>).GetMethod("InstanceMethod").Invoke(new ValueFooer(), new object[] { })).Equals(typeof(Fooer[]).TypeHandle))
            return 33;

        try
        {
            typeof(IFoo).GetMethod("DefaultMethod").Invoke(new Reabstractor(), new object[] { 1 });
            return 501;
        }
        catch (TargetInvocationException ie) when (ie.InnerException is EntryPointNotFoundException)
        {
        }

        return 100;
    }
}
