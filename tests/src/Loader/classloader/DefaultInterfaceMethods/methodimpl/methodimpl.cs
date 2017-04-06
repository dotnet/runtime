using System;

interface IFoo
{
    int Foo(int a);
}

interface IBar
{
    int Bar(int b);
}

interface IFooBar : IFoo, IBar
{
    int Foo(int a);
}

class Temp : IFoo
{
    int IFoo.Foo(int a)
    {
        Console.WriteLine("At IFooBar::IFoo.Foo explicit methodimpl");
        return a + 30;
    }
}

class FooBar : IFooBar
{
    public int Foo(int a)
    {
        Console.WriteLine("At IFoo::Foo");
        return a+10;            
    }

    public int Bar(int b)
    {
        Console.WriteLine("At IBar::Bar");
        return b+20;
    }
}

class Program
{
    public static int Main()
    {
        FooBar fooBar = new FooBar();
        IFoo foo = (IFoo) fooBar;
        IBar bar = (IBar) fooBar;

        Console.WriteLine("Calling IFoo.Foo on FooBar - expecting IFooBar::IFoo.Bar");
        Test.Assert(foo.Foo(10) == 40, "Calling IFoo.Foo on FooBar");

        Console.WriteLine("Calling IBar.Bar on FooBar - expecting IBar::Bar");
        Test.Assert(bar.Bar(10) == 30, "Calling IBar.Bar on FooBar");

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


