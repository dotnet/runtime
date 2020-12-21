// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

interface IFoo<T>
{
    Type Foo(T a);
}

interface IBar<in T>
{
    Type Bar(T b);
}

class FooBar<T, U> : IFoo<T>, IBar<U>
{
    public Type Foo(T a)
    {
        Console.WriteLine("At IFoo.Foo:Arg={0}, TypeOf(T)={1}", a.ToString(), typeof(T));
        return typeof(T);            
    }

    public Type Bar(U b)
    {
        Console.WriteLine("At IBar.Bar:Arg={0}, TypeOf(T)={1}", b.ToString(), typeof(U));
        return typeof(U);
    }
}

class Program
{
    public static int Main()
    {
        FooBar<string, object> fooBar = new FooBar<string, object>();
        IFoo<string> foo = (IFoo<string>) fooBar;
        IBar<string[]> bar = (IBar<string[]>) fooBar;

        Console.WriteLine("Calling IFoo<string>.Foo on FooBar<string, object> - expecting default method IFoo<string>.Foo");
        Test.Assert(foo.Foo("ABC") == typeof(string), "Calling IFoo<string>.Foo on FooBar<string, object>");

        Console.WriteLine("Calling IBar<string[]>.Foo on FooBar<string, object> - expecting default method IBar<object>.Foo");
        Test.Assert(bar.Bar(new string[] { "ABC" }) == typeof(object), "Calling IBar<object>.Bar on FooBar<string, object>");

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

