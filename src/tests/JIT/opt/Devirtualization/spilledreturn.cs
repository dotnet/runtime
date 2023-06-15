// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Examples where methods potentially return multiple types
// but the jit can prune the set down to one type during
// importation, which then triggers late devirtualization.

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

public class Derived2 : Base
{
    public override sealed void Foo() { Console.WriteLine("Derived2:Foo"); }
    public override void Bar() { Console.WriteLine("Derived2:Bar"); }
}

public class Test
{
    static bool vague;

    // Constant prop
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Base M(int x)
    {
        if (x > 0)
        {
            return new Derived();
        }
        else 
        {
            return new Derived2();
        }
    }

    // All returns agree on type
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Base N(bool b)
    {
        if (b)
        {
            Console.WriteLine("b true");
            return new Derived();
        }
        else 
        {
            Console.WriteLine("b false");
            return new Derived();
        }
    }

    // Type specialization
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Base G<T>()
    {
        if (typeof(T) == typeof(int))
        {
            return new Derived();
        }
        else
        {
            return new Derived2();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestEntry(bool vague)
    {
        M(0).Foo();
        M(0).Bar();
        M(1).Foo();
        M(1).Bar();

        N(vague).Foo();
        N(!vague).Bar();

        G<int>().Foo();
        G<byte>().Foo();
        G<string>().Foo();

        return 100;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return TestEntry(false);
    }
}


        
    
