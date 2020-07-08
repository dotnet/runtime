// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

interface IFoo
{
    int Foo1(int a); // { return a + 1 };
    int Foo2(int a); // { return a + 2 };
    int Foo3(int a);
    int Foo4(int a);
    int Foo5(int a);
    int Foo6(int a);
    int Foo7(int a);
    int Foo8(int a);
    int Foo9(int a);
}

interface IBar : IFoo
{
    // @OVERRIDE
    // IFoo.Foo1/2/3/4/5

    int Bar1(int b); // { return a + 11; }
    int Bar2(int b); // { return a + 22; } 
    int Bar3(int b); // { return a + 33; } 
    int Bar4(int b);
    int Bar5(int b);
    int Bar6(int b);
    int Bar7(int b);
    int Bar8(int b);
    int Bar9(int b);
}

interface IBlah : IBar
{
    // @OVERRIDE IFoo.Foo6/7/8/9
    // @OVERRIDE IBar.Bar6/7/8/9
    int Blah1(int c);
    int Blah2(int c);
    int Blah3(int c);
}

class IBarImpl : IBar
{
    // @REMOVE all implementation
    int IFoo.Foo1(int a)
    {
        Console.WriteLine("At IIFoo.Foo1");
        return a + 10;
    }

    int IFoo.Foo2(int a)
    {
        Console.WriteLine("At IIFoo.Foo1");
        return a + 20;
    }
    int IFoo.Foo3(int a)
    {
        Console.WriteLine("At IIFoo.Foo1");
        return a + 30;
    }
    int IFoo.Foo4(int a)
    {
        Console.WriteLine("At IIFoo.Foo1");
        return a + 40;
    }
    int IFoo.Foo5(int a)
    {
        Console.WriteLine("At IIFoo.Foo1");
        return a + 50;
    }
    int IFoo.Foo6(int a)
    {
        Console.WriteLine("At IIFoo.Foo1");
        return a + 60;
    }
    int IFoo.Foo7(int a)
    {
        Console.WriteLine("At IIFoo.Foo1");
        return a + 70;
    }
    int IFoo.Foo8(int a)
    {
        Console.WriteLine("At IIFoo.Foo1");
        return a + 80;
    }
    int IFoo.Foo9(int a)
    {
        Console.WriteLine("At IIFoo.Foo1");
        return a + 19;
    }

    int IBar.Bar1(int a)
    {
        Console.WriteLine("At IBar.Bar1");
        return a + 110;
    }

    int IBar.Bar2(int a)
    {
        Console.WriteLine("At IBar.Bar1");
        return a + 220;
    }
    int IBar.Bar3(int a)
    {
        Console.WriteLine("At IBar.Bar1");
        return a + 330;
    }
    int IBar.Bar4(int a)
    {
        Console.WriteLine("At IBar.Bar1");
        return a + 440;
    }
    int IBar.Bar5(int a)
    {
        Console.WriteLine("At IBar.Bar1");
        return a + 550;
    }
    int IBar.Bar6(int a)
    {
        Console.WriteLine("At IBar.Bar1");
        return a + 660;
    }
    int IBar.Bar7(int a)
    {
        Console.WriteLine("At IBar.Bar1");
        return a + 770;
    }
    int IBar.Bar8(int a)
    {
        Console.WriteLine("At IBar.Bar1");
        return a + 880;
    }
    int IBar.Bar9(int a)
    {
        Console.WriteLine("At IBar.Bar1");
        return a + 990;
    }      
}

class IBlahImpl : IBarImpl, IBlah
{
    // @REMOVE all implementation
    // @OVERRIDE IBlah2/3 with + 2220/3330
    int IBlah.Blah1(int c)
    {
        Console.WriteLine("At IBlah.Blah1");
        return c+111;
    }

    int IBlah.Blah2(int c)
    {
        Console.WriteLine("At IBlah.Blah2");
        return c+222;
    }

    int IBlah.Blah3(int c)
    {
        Console.WriteLine("At IBlah.Blah3");
        return c+333;
    }
}

interface IFooBarBlah : IFoo, IBar, IBlah
{
    // FooBarBlah1 .override IFoo.Foo1/IBar.Bar1/IBlah.Blah1 return 1+11111
    // FooBarBlah2 .override IFoo.Foo2/IBar.Bar2/IBlah.Blah2 return i+22222
    // FooBarBLah345 .override IFoo.Foo345/IBar.Bar345/IBlah.Blah3 return i+33333
}

class FooBarBlahImpl : 
    IBlahImpl,   // @REMOVE
    IFooBarBlah
{

}

class Program
{
    public static int Main()
    {
        SingleOverride();
        MultiOverride();
                              
        return Test.Ret();
    }

    private static void SingleOverride()
    {
        IBarImpl barImpl = new IBarImpl();
        IFoo foo = (IFoo) barImpl;

        Console.WriteLine("Calling IFoo.Foo methods on IBarImpl...");

        Test.Assert(foo.Foo1(1) == 11, "Calling IFoo.Foo1 on IBarImpl");
        Test.Assert(foo.Foo2(2) == 22, "Calling IFoo.Foo2 on IBarImpl");
        Test.Assert(foo.Foo3(3) == 33, "Calling IFoo.Foo3 on IBarImpl");
        Test.Assert(foo.Foo4(4) == 44, "Calling IFoo.Foo4 on IBarImpl");
        Test.Assert(foo.Foo5(5) == 55, "Calling IFoo.Foo5 on IBarImpl");
        Test.Assert(foo.Foo6(0) == 6, "Calling IFoo.Foo6 on IBarImpl");
        Test.Assert(foo.Foo7(0) == 7, "Calling IFoo.Foo7 on IBarImpl");
        Test.Assert(foo.Foo8(0) == 8, "Calling IFoo.Foo8 on IBarImpl");
        Test.Assert(foo.Foo9(0) == 9, "Calling IFoo.Foo9 on IBarImpl");

        IBar bar = (IBar) barImpl;

        Console.WriteLine("Calling IBar.Bar methods on IBarImpl...");

        Test.Assert(bar.Bar1(0) == 11, "Calling IBar.Bar1 on IBarImpl");
        Test.Assert(bar.Bar2(0) == 22, "Calling IBar.Bar2 on IBarImpl");
        Test.Assert(bar.Bar3(0) == 33, "Calling IBar.Bar3 on IBarImpl");
        Test.Assert(bar.Bar4(0) == 44, "Calling IBar.Bar4 on IBarImpl");
        Test.Assert(bar.Bar5(0) == 55, "Calling IBar.Bar5 on IBarImpl");
        Test.Assert(bar.Bar6(0) == 66, "Calling IBar.Bar6 on IBarImpl");
        Test.Assert(bar.Bar7(0) == 77, "Calling IBar.Bar7 on IBarImpl");
        Test.Assert(bar.Bar8(0) == 88, "Calling IBar.Bar8 on IBarImpl");
        Test.Assert(bar.Bar9(0) == 99, "Calling IBar.Bar9 on IBarImpl");

        IBlahImpl blahImpl = new IBlahImpl();
        foo = (IFoo) blahImpl;

        Test.Assert(foo.Foo1(1) == 11, "Calling IFoo.Foo1 on IBlahImpl");
        Test.Assert(foo.Foo2(2) == 22, "Calling IFoo.Foo2 on IBlahImpl");
        Test.Assert(foo.Foo3(3) == 33, "Calling IFoo.Foo3 on IBlahImpl");
        Test.Assert(foo.Foo4(4) == 44, "Calling IFoo.Foo4 on IBlahImpl");
        Test.Assert(foo.Foo5(5) == 55, "Calling IFoo.Foo5 on IBlahImpl");
        Test.Assert(foo.Foo6(6) == 66, "Calling IFoo.Foo6 on IBlahImpl");
        Test.Assert(foo.Foo7(7) == 77, "Calling IFoo.Foo7 on IBlahImpl");
        Test.Assert(foo.Foo8(8) == 88, "Calling IFoo.Foo8 on IBlahImpl");
        Test.Assert(foo.Foo9(9) == 99, "Calling IFoo.Foo9 on IBlahImpl");

        bar = (IBar) blahImpl;

        Console.WriteLine("Calling IBar.Bar methods on IBlahImpl...");

        Test.Assert(bar.Bar1(1) == 111, "Calling IBar.Bar1 on IBlahImpl");
        Test.Assert(bar.Bar2(2) == 222, "Calling IBar.Bar2 on IBlahImpl");
        Test.Assert(bar.Bar3(3) == 333, "Calling IBar.Bar3 on IBlahImpl");
        Test.Assert(bar.Bar4(4) == 444, "Calling IBar.Bar4 on IBlahImpl");
        Test.Assert(bar.Bar5(5) == 555, "Calling IBar.Bar5 on IBlahImpl");
        Test.Assert(bar.Bar6(0) == 66, "Calling IBar.Bar6 on IBlahImpl");
        Test.Assert(bar.Bar7(0) == 77, "Calling IBar.Bar7 on IBlahImpl");
        Test.Assert(bar.Bar8(0) == 88, "Calling IBar.Bar8 on IBlahImpl");
        Test.Assert(bar.Bar9(0) == 99, "Calling IBar.Bar9 on IBlahImpl");  

        IBlah blah = (IBlah) blahImpl;

        Console.WriteLine("Calling IBlah.Blah methods on IBlahImpl...");   

        Test.Assert(blah.Blah1(0) == 111, "Calling IBlah.Blah1 on IBlahImpl");
        Test.Assert(blah.Blah2(2) == 2222, "Calling IBlah.Blah1 on IBlahImpl");
        Test.Assert(blah.Blah3(3) == 3333, "Calling IBlah.Blah1 on IBlahImpl"); 
    }

    private static void MultiOverride()
    {        
        FooBarBlahImpl fooBarBlah = new FooBarBlahImpl();
        IFoo foo = (IFoo) fooBarBlah;

        Console.WriteLine("Calling IFoo.Foo methods on FooBarBlahImpl...");   
        Test.Assert(foo.Foo1(0) == 11111, "Calling IFoo.Foo1 on FooBarBlahImpl");
        Test.Assert(foo.Foo2(0) == 22222, "Calling IFoo.Foo2 on FooBarBlahImpl");
        Test.Assert(foo.Foo3(0) == 33333, "Calling IFoo.Foo3 on FooBarBlahImpl");
        Test.Assert(foo.Foo4(0) == 33333, "Calling IFoo.Foo4 on FooBarBlahImpl");
        Test.Assert(foo.Foo5(0) == 33333, "Calling IFoo.Foo5 on FooBarBlahImpl");
        Test.Assert(foo.Foo6(6) == 66, "Calling IFoo.Foo6 on FooBarBlahImpl");
        Test.Assert(foo.Foo7(7) == 77, "Calling IFoo.Foo7 on FooBarBlahImpl");
        Test.Assert(foo.Foo8(8) == 88, "Calling IFoo.Foo8 on FooBarBlahImpl");
        Test.Assert(foo.Foo9(9) == 99, "Calling IFoo.Foo9 on FooBarBlahImpl"); 

        IBar bar = (IBar) fooBarBlah;

        Console.WriteLine("Calling IBar.Bar methods on FooBarBlahImpl...");

        Test.Assert(bar.Bar1(0) == 11111, "Calling IBar.Bar1 on FooBarBlahImpl");
        Test.Assert(bar.Bar2(0) == 22222, "Calling IBar.Bar2 on FooBarBlahImpl");
        Test.Assert(bar.Bar3(0) == 33333, "Calling IBar.Bar3 on FooBarBlahImpl");
        Test.Assert(bar.Bar4(0) == 33333, "Calling IBar.Bar4 on FooBarBlahImpl");
        Test.Assert(bar.Bar5(0) == 33333, "Calling IBar.Bar5 on FooBarBlahImpl");
        Test.Assert(bar.Bar6(0) == 66, "Calling IBar.Bar6 on FooBarBlahImpl");
        Test.Assert(bar.Bar7(0) == 77, "Calling IBar.Bar7 on FooBarBlahImpl");
        Test.Assert(bar.Bar8(0) == 88, "Calling IBar.Bar8 on FooBarBlahImpl");
        Test.Assert(bar.Bar9(0) == 99, "Calling IBar.Bar9 on FooBarBlahImpl");            

        IBlah blah = (IBlah) fooBarBlah;
       
        Console.WriteLine("Calling IBlah.Blah methods on FooBarBlahImpl...");   

        Test.Assert(blah.Blah1(0) == 11111, "Calling IBlah.Blah1 on FooBarBlahImpl");
        Test.Assert(blah.Blah2(0) == 22222, "Calling IBlah.Blah1 on FooBarBlahImpl");
        Test.Assert(blah.Blah3(0) == 33333, "Calling IBlah.Blah1 on FooBarBlahImpl"); 
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


