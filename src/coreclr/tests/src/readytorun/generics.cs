// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Collections.Generic;
using System.Text;

class Program
{
    static int Main()
    {
        // Run all tests 3x times to exercise both slow and fast paths work
        for (int i = 0; i < 3; i++)
            RunAllTests();

        Console.WriteLine(Assert.HasAssertFired ? "FAILED" : "PASSED");
        return Assert.HasAssertFired ? 1 : 100;
    }

    static void RunAllTests()
    {
        DateTime dt = new DateTime(1776, 7, 4);
        string dtString = dt.ToString();
        Assert.AreEqual(new GenClass1c<DateTime>(dt).ToStringEx(7), dtString + " 7");
        Assert.AreEqual(new GenClass1c<int>(1).ToStringEx(7), "1 7");
        Assert.AreEqual(new GenClass1c<long>(2).ToStringEx(7), "2 7");
        Assert.AreEqual(new GenClass1c<float>(3.14f).ToStringEx(7), "3.14 7");
        Assert.AreEqual(new GenClass1c<double>(4.13).ToStringEx(7), "4.13 7");
        Assert.AreEqual(new GenClass1c<int?>(9).ToString(), "9");

        Assert.AreEqual(new GenClass2<DateTime, double>(dt, 3.1416).ToString(), dtString + " 3.1416");
        Assert.AreEqual(new GenClass2<DateTime, double>(dt, 3.1416).ToStringEx(7, 8), dtString + " 3.1416 7 8");
        Assert.AreEqual(new GenClass2<object, string>(new object(), "3.1416").ToString(), "System.Object 3.1416");
        Assert.AreEqual(new GenClass2<object, string>(new object(), "3.1416").ToStringEx(7L, 8L), "System.Object 3.1416 7 8");
        Assert.AreEqual(GetString(7.0, 8.0), "7 8");

        var gen1a = new GenClass1a<object>();
        Assert.AreEqual(gen1a.CreateGenClass1b(), "GenClass1b`1[System.Object]");
        Assert.AreEqual(gen1a.CreateGenClass1bArray(), "GenClass1b`1[System.Object][]");

        var gen1aInt = new GenClass1a<int>();
        var gen1bInt = new GenClass1b<int>();
        var gen1bLong = new GenClass1b<long>();
        Assert.AreEqual(gen1bInt.IsGenClass1a(gen1aInt).ToString(), "True");
        Assert.AreEqual(gen1bLong.IsGenClass1a(gen1aInt).ToString(), "False");
        Assert.AreEqual(gen1bInt.AsGenClass1a(gen1aInt)?.ToString() ?? "null", gen1aInt.ToString());
        Assert.AreEqual(gen1bLong.AsGenClass1a(gen1aInt)?.ToString() ?? "null", "null");

        var gen1aString = new GenClass1a<string>();
        var gen1b = new GenClass1b<string>();
        Assert.AreEqual(gen1b.IsGenClass1a(gen1aString).ToString(), "True");
        Assert.AreEqual(gen1b.AsGenClass1a(gen1aString)?.ToString() ?? "null", gen1aString.ToString());
#if false // not yet supported
        Assert.AreEqual(GenClass1a<string>.CallVirtual(gen1b), "GenClass1b`1[System.String].VirtualMethod");
        Assert.AreEqual(GenClass1a<string>.CallInterface(gen1b), "GenClass1b`1[System.String].InterfaceMethod1");
        Assert.AreEqual(GenClass1a<string>.CallInterface(gen1b, "Test").ToString(), "GenClass1b`1[System.String]");
#endif

        NormalClass n = new NormalClass();
        Assert.AreEqual(CallGenVirtMethod<int>(n).ToString(), "GenClass1a`1[System.Int32]");
        Assert.AreEqual(CallGenVirtMethod<int>(n, 42).ToString(), "System.Int32[]");
#if false // not yet supported
        Assert.AreEqual(CallGenVirtMethod<string>(n).ToString(), "GenClass1a`1[System.String]");
        Assert.AreEqual(CallGenVirtMethod<string>(n, "forty-two").ToString(), "System.String[]");
#endif
    }

    static string GetString<X, Y>(X x, Y y)
    {
        return string.Join(" ", x, y);
    }

    static GenClass1a<T> CallGenVirtMethod<T>(NormalClass n)
    {
        return n.GetGenClass1a<T>();
    }

    static IEnumerable<T> CallGenVirtMethod<T>(NormalClass n, object o)
    {
        return n.GetEnumerable<T>(o);
    }
}

interface IGenInterface<T>
{
    string InterfaceMethod1();
    IGenInterface<T> InterfaceMethod2<U>(U u);
}

class GenClass1a<T>
{
    public string CreateGenClass1b()
    {
        var x = new GenClass1b<T>();
        return x.ToString();
    }
    public string CreateGenClass1bArray()
    {
        var x = new GenClass1b<T>[3];
        return x.ToString();
    }
    public static string CallVirtual(GenClass1b<T> x)
    {
        return x.VirtualMethod();
    }
    public static string CallInterface(IGenInterface<T> x)
    {
        return x.InterfaceMethod1();
    }
    public static IGenInterface<U> CallInterface<U, V>(IGenInterface<U> x, V v)
    {
        return x.InterfaceMethod2(v);
    }
}

class GenClass1b<T> : IGenInterface<T>
{
    public virtual string VirtualMethod()
    {
        return ToString() + ".VirtualMethod";
    }
    public virtual string InterfaceMethod1()
    {
        return ToString() + ".InterfaceMethod1";
    }
    public virtual IGenInterface<T> InterfaceMethod2<U>(U u)
    {
        return this;
    }
    public bool IsGenClass1a(object o)
    {
        return o is GenClass1a<T>;
    }
    public GenClass1a<T> AsGenClass1a(object o)
    {
        return o as GenClass1a<T>;
    }
}

class GenClass1c<T> where T : new()
{
    public T t;
    public GenClass1c()
    {
        t = new T();
    }
    public GenClass1c(T _t)
    {
        t = _t;
    }
    public void SetT(object x)
    {
        t = (T)x;
    }
    public override string ToString()
    {
        return t.ToString();
    }
    public string ToStringEx<X>(X x)
    {
        return string.Join(" ", t, x);
    }
}

class GenClass2<T, U>
{
    public GenClass2(T t, U u)
    {
        this.t = t;
        this.u = u;
    }
    public override string ToString()
    {
        return t.ToString() + " " + u.ToString();
    }
    public string ToStringEx<X, Y>(X x, Y y)
    {
        return string.Join(" ", t, u, x, y);
    }
    T t;
    U u;
}

class NormalClass
{
    public virtual GenClass1a<T> GetGenClass1a<T>()
    {
        return new GenClass1a<T>();
    }
    public virtual IEnumerable<T> GetEnumerable<T>(object o)
    {
        T[] array = new T[1];
        array[0] = (T)o;
        return array;
    }
}

public static class Assert
{
    public static bool HasAssertFired;

    public static void AreEqual(Object actual, Object expected)
    {
        if (!(actual == null && expected == null) && !actual.Equals(expected))
        {
            Console.WriteLine("Not equal!");
            Console.WriteLine("actual   = " + actual.ToString());
            Console.WriteLine("expected = " + expected.ToString());
            HasAssertFired = true;
        }
    }
}
