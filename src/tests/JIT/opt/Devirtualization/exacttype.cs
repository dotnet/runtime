// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Base 
{
    public virtual void Foo() { Console.WriteLine("Base:Foo"); }
    public virtual void Bar() { Console.WriteLine("Base:Bar"); }
}

public class Derived : Base
{
    public override sealed void Foo() { Console.WriteLine("Derived:Foo"); }
    public override void Bar() { Console.WriteLine("Derived:Bar"); }
}

// The jit should to be able to devirtualize all calls to Bar since the
// exact type is knowable.
//
// The jit should to be able to devirtualize calls to Foo when the
// type is known to be at least Derived.
//
// Currently the jit misses some of these cases, either because it has
// lost the more precise type or lost the fact that the type was
// exact.

public class Test_exacttype
{
    public static Base M()
    {
        return new Derived();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // Declared type of 'd' has final method Foo(), so calls to
        // Foo() will devirtualize.
        //
        // However the jit does not know that d's type is exact so
        // currently the calls to Bar() will not devirtualize.
        Derived d = new Derived();
        d.Foo();
        d.Bar();

        // M should inline and expose an exact return type
        // which will trigger late devirt for both Foo() and Bar().
        M().Foo();
        M().Bar();

        // Copy via 'b' currently inhibits devirt
        Base b = M();
        b.Foo();
        b.Bar();

        // Direct use of newobj gives exact type so all these
        // will devirtualize
        new Base().Foo();
        new Base().Bar();
        new Derived().Foo();
        new Derived().Bar();

        return 100;
    }
}


        
    
