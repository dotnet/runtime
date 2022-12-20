// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

interface IFoo
{
    Type Foo<T>();
}

interface IBar<T>
{
    Type Bar1<P>();
    Type Bar2<K>();
    void Bar3<P, K>(out Type t, out Type u);
}

class FooBar<V> : IFoo, IBar<V>
{
    public Type Foo<T>()
    {
        Console.WriteLine("At IFoo<T>::Foo<T>: TypeOf(T) = {0}", typeof(T));
        return typeof(T);
    }

    public Type Bar1<P>()
    {
        Console.WriteLine("At IBar<T>::Foo<P>: TypeOf(P) = {0}", typeof(P));
        return typeof(P);
    }

    public Type Bar2<K>()
    {
        Console.WriteLine("At IBar<T>::Bar2<K>: TypeOf(K) = {0}", typeof(K));
        return typeof(K);
    }

    public void Bar3<P, K>(out Type t, out Type u)
    {
        Console.WriteLine("At IBar<T>::Bar3<P, K>: TypeOf(P) = {0}, TypeOf(K) = {1}", typeof(P), typeof(K));
        t = typeof(P);
        u = typeof(K);
    }
}


class Program
{
    static int Main(string[] args)
    {
        FooBar<object> fooBar = new FooBar<object>();
        IFoo foo = (IFoo) fooBar;
        IBar<object> bar = (IBar<object>) fooBar;

        Console.WriteLine("Calling IFoo.Foo<String> on FooBar<Object> - expecting IFoo::Foo<string>() returning typeof(string)");
        Test.Assert(foo.Foo<string>() == typeof(string), "Calling IFoo.Foo<String> on FooBar<Object>");

        Console.WriteLine("Calling IBar.Bar1<String> on FooBar<object> - expecting bar.Bar1<string>() returning typeof(string)");
        Test.Assert(bar.Bar1<string>() == typeof(string), "Calling IBar.Bar1<String> on FooBar<object>");

        Console.WriteLine("Calling IBar.Bar2<String[]> on FooBar<object> - expecting bar.Bar2<string[]>() returning typeof(string[])");
        Test.Assert(bar.Bar2<string[]>() == typeof(string[]), "Calling IBar.Bar2<String[]> on FooBar<object>");

        Type p, k;
        Console.WriteLine("Calling IBar.Bar3<String, String[]> - expecting bar.Bar3<string>() returning typeof(string), typeof(string[])");
        bar.Bar3<string, string[]>(out p, out k);
        Test.Assert(p == typeof(string) && k == typeof(string[]), "Calling IBar.Bar3<String, String[]>");

        return Test.Ret();
    }
}

class Test
{
    private static bool Pass = true;

    public static int Ret()
    {
        return Pass ? 100 : 101;
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

