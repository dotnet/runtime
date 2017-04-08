using System;

interface IFoo
{
    int Foo(int a);
}

class IFoo_Impl
{
    int Foo(int a)
    {
        return a;
    }
}

interface IFoo2 : IFoo
{
}

class IFoo2_Impl : IFoo
{
    int IFoo.Foo(int a)
    {
        Console.WriteLine("At IFoo2.Foo");
        return a + 1;
    }        
}

interface IFooEx : IFoo
{
}

class IFooEx_Impl : IFoo
{
    int IFoo.Foo(int a)
    {
        Console.WriteLine("At IFooEx.Foo");
        return a + 2;
    }        
}

class FooClass : IFoo2, IFooEx
{
    // Dummy
    public int Foo(int a)
    {
        return 0;
    }
}

class Program
{
    public static int Main()
    {
        FooClass fooObj = new FooClass();
        IFoo foo = (IFoo) fooObj;

        Console.WriteLine("Calling IFoo.Foo on Foo - expecting exception.");
        try
        {
             foo.Foo(10);
             Test.Assert(false, "Expecting exception");
        }
        catch(Exception)
        {
        }

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

