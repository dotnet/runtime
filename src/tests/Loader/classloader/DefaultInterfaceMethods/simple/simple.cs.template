// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

interface IBlah
{
    int Blah(int c);    
}

// All methods go into IBlah
class IBlah_Impl
{
    public int Blah(int c)
    {
        Console.WriteLine("At IBlah.Blah"); 
        return c + Blah_Private_GetA() + Blah_Internal_GetB() + Blah_Protected_GetC();        
    }

    private int Blah_Private_GetA()
    {
        Console.WriteLine("At IBlah.Blah_Private_GetA");
        return 1;
    }

    internal int Blah_Internal_GetB()
    {
        Console.WriteLine("At IBlah.Blah_Internal_GetB"); 
        return 2;
    }

    protected int Blah_Protected_GetC()
    {
        Console.WriteLine("At IBlah.Blah_Protected_GetC"); 
        return 3;
    }
}

interface IFoo
{
    int Foo(int a);
}

interface IBar
{
    int Bar(int b);
}

class Base : IBlah
{
    public int Blah(int c)
    {
        // Dummy
        return 0;    
    }
}

class FooBar : Base, IFoo, IBar
{
    public int Foo(int a)
    {
        Console.WriteLine("At IFoo.Foo");
        return a+1;            
    }

    public int Bar(int b)
    {
        Console.WriteLine("At IBar.Bar");
        return b+10;
    }

    public int CallBlahProtected()
    {
        // change to IBlah.Blah_Protected_GetC();        
        return CallBlahProtected();
    }
}

class Program
{
    public static int Main()
    {
        FooBar fooBar = new FooBar();
        IFoo foo = (IFoo) fooBar;
        IBar bar = (IBar) fooBar;
        IBlah blah = (IBlah) fooBar;

        Console.WriteLine("Calling IFoo.Foo on FooBar - expecting default method on IFoo.Foo. ");
        Test.Assert(foo.Foo(10) == 11, "Calling IFoo.Foo on FooBar");

        Console.WriteLine("Calling IBar.Bar on FooBar - expecting default method on IBar.Bar. ");
        Test.Assert(bar.Bar(10) == 20, "Calling IBar.Bar on FooBar");

        Console.WriteLine("Calling IBlah.Blah on FooBar - expecting default method on IBlah.Blah from Base. ");
        Test.Assert(blah.Blah(10) == 16, "Calling IBlah.Blah on FooBar");

        Console.WriteLine("Calling FooBar.CallBlahProtected - expecting protected methods on interface can be called");
        Test.Assert(fooBar.CallBlahProtected() == 3, "Calling FooBar.CallBlahProtected");

        return Test.Ret();
    }
}

class Test
{
    private static bool Pass = true;

    public static int Ret()
    {
        return Pass? 100 : 101;
    }

    public static void Assert(bool cond, string msg)
    {
        if (cond)
        {
            Console.WriteLine("PASS");
        }
        else
        {
            Console.WriteLine("FAIL: " + msg);
            Pass = false;
        }
    }
}                    

