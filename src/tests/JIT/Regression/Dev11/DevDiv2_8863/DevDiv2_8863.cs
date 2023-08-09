// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Base
{
    public virtual Base Get()
    {
        return null;
    }
}

public class Derived : Base
{
    public int i;
    public int j;
    public int k;
}

public class MyDerived : Derived
{
    public override Base Get()
    {
        return new MyBase();
    }
}

public class MyBase : Base
{
    string foo = "foo";
    int track = 0x44444444;

    ~MyBase()
    {
        Console.WriteLine("\tDestructor. Foo: 0x{0:X8}", foo.Length);
    }
}

public class Program
{
    static void x64_JIT_Bug(Derived d)
    {
        Base b = d;
    loop:
        if (b != null)
        {
            if (b is Derived)
            {
                Oops((Derived)b);
            }
            b = b.Get();
            goto loop;
        }
    }

    static void Oops(Derived d)
    {
        Console.WriteLine(d);
        Console.WriteLine(d.i);
        Console.WriteLine(d.j);
        Console.WriteLine(d.k);
        d.i = 0x77777777;
        d.j = 0x77777777;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        /* This issue is caused by CSE and trying to pull a typecheck out of a loop.
         * We used to do this incorrectly and this could allow a type to call methods
         * from it's "cousin" types...
         * 
         * This example will AV...
         * */
        x64_JIT_Bug(new MyDerived());
        return 100; // Well, we made it here... should be good.
    }
}

