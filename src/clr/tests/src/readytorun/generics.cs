// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

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
        RunTest1();
        RunTest2();
        RunTest3();
        RunTest4();
        RunTest5();
        RunTest6();
        RunTest7();
    }

    static void RunTest1()
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
        Assert.AreEqual(GenClass1a<string>.CallVirtual(gen1b), "GenClass1b`1[System.String].VirtualMethod");
        Assert.AreEqual(GenClass1a<string>.CallInterface(gen1b), "GenClass1b`1[System.String].InterfaceMethod1");
        Assert.AreEqual(GenClass1a<string>.CallInterface(gen1b, "Test").ToString(), "GenClass1b`1[System.String]");

        NormalClass n = new NormalClass();
        Assert.AreEqual(CallGenVirtMethod<int>(n).ToString(), "GenClass1a`1[System.Int32]");
        Assert.AreEqual(CallGenVirtMethod<int>(n, 42).ToString(), "System.Int32[]");
        Assert.AreEqual(CallGenVirtMethod<string>(n).ToString(), "GenClass1a`1[System.String]");
        Assert.AreEqual(CallGenVirtMethod<string>(n, "forty-two").ToString(), "System.String[]");
    }

    static void RunTest2()
    {
        var mi = new GenBase<MyClass0, int>();
        var ol = new GenBase<object, long>();

        // LDTOKEN OF TYPE PARAMETERS TEST
        Assert.AreEqual(mi.GetT(), "MyClass0");
        Assert.AreEqual(mi.GetU(), "System.Int32");
        Assert.AreEqual(ol.GetT(), "System.Object");
        Assert.AreEqual(ol.GetU(), "System.Int64");
        Assert.AreEqual(mi.GetT(), "MyClass0");
        Assert.AreEqual(mi.GetU(), "System.Int32");

        Assert.AreEqual(mi.GetTArray(), "MyClass0[]");
        Assert.AreEqual(ol.GetTArray(), "System.Object[]");
        Assert.AreEqual(mi.GetTArray(), "MyClass0[]");
        Assert.AreEqual(mi.GetTBasedInst(), "MyGenClass2`1[MyGenClass1`1[MyClass0]]");
        Assert.AreEqual(ol.GetTBasedInst(), "MyGenClass2`1[MyGenClass1`1[System.Object]]");
        Assert.AreEqual(mi.GetTBasedInst(), "MyGenClass2`1[MyGenClass1`1[MyClass0]]");
    }

    static void RunTest3()
    {
        var mi = new GenBase<MyClass0, int>();
        var ol = new GenBase<object, long>();

        // GENERIC INTERFACE CALL AND CASTING TEST
        Assert.AreEqual(mi.IFaceCallTest(mi), "IFaceCallTest = IFooFunc - GenBase`2[MyClass0,System.Int32]");
        Assert.AreEqual(ol.IFaceCallTest(ol), "IFaceCallTest = IFooFunc - GenBase`2[System.Object,System.Int64]");
        Assert.AreEqual(mi.IFaceCallTest(mi), "IFaceCallTest = IFooFunc - GenBase`2[MyClass0,System.Int32]");

        // LDTOKEN TEST
        Assert.AreEqual(mi.LdTokenTest(), "LdTokenTest - System.Collections.Generic.Dictionary`2[MyClass0,System.Int32]");
        Assert.AreEqual(ol.LdTokenTest(), "LdTokenTest - System.Collections.Generic.Dictionary`2[System.Object,System.Int64]");
        Assert.AreEqual(mi.LdTokenTest(), "LdTokenTest - System.Collections.Generic.Dictionary`2[MyClass0,System.Int32]");

        // DICTIONARY ACCESS FROM STATIC METHOD
        Assert.AreEqual(GenBase<MyClass0, int>.StaticNonGenMethod(), "StaticNonGenMethod - System.Collections.Generic.List`1[MyClass0]");
        Assert.AreEqual(GenBase<object, long>.StaticNonGenMethod(), "StaticNonGenMethod - System.Collections.Generic.List`1[System.Object]");
        Assert.AreEqual(GenBase<MyClass0, int>.StaticNonGenMethod(), "StaticNonGenMethod - System.Collections.Generic.List`1[MyClass0]");
        Assert.AreEqual(GenBase<MyClass0, int>.StaticGenMethod<Type>(), "StaticGenMethod - System.Collections.Generic.Dictionary`2[System.Type,MyClass0]");
        Assert.AreEqual(GenBase<object, long>.StaticGenMethod<Type>(), "StaticGenMethod - System.Collections.Generic.Dictionary`2[System.Type,System.Object]");
        Assert.AreEqual(GenBase<MyClass0, int>.StaticGenMethod<Type>(), "StaticGenMethod - System.Collections.Generic.Dictionary`2[System.Type,MyClass0]");

        // NEW TEST
        Assert.AreEqual(mi.NewTest(), "NewTest - MyClass0 - MyGenClass1`1[MyClass0] - MyClass0[] - MyClass0[,] - MyGenClass3`1[MyClass0][] - MyGenClass3`1[MyClass0][,]");
        Assert.AreEqual(ol.NewTest(), "NewTest - System.Object - MyGenClass1`1[System.Object] - System.Object[] - System.Object[,] - MyGenClass3`1[System.Object][] - MyGenClass3`1[System.Object][,]");
        Assert.AreEqual(mi.NewTest(), "NewTest - MyClass0 - MyGenClass1`1[MyClass0] - MyClass0[] - MyClass0[,] - MyGenClass3`1[MyClass0][] - MyGenClass3`1[MyClass0][,]");
    }

    static void RunTest4()
    {
        // FIELDS TEST
        var fobj1 = new GenBase<MyIdClass0, int>();
        var fobj2 = new GenBase<MyIdClass1, int>();
        GenBase<MyIdClass0, int>.SetFieldsTest(fobj1, new MyIdClass0("1"), new MyIdClass0("2"), new MyIdClass0("3"), 1, 2, 3);
        GenBase<MyIdClass1, int>.SetFieldsTest(fobj2, new MyIdClass1("1"), new MyIdClass1("2"), new MyIdClass1("3"), 1, 2, 3);

        GenBase<MyIdClass0, int>.GetFieldsTest(fobj1, "MyIdClass0=1", "MyIdClass0=2", "MyIdClass0=3", 1, 2, 3);
        GenBase<MyIdClass1, int>.GetFieldsTest(fobj2, "MyIdClass1=1", "MyIdClass1=2", "MyIdClass1=3", 1, 2, 3);

        Thread t = new Thread(new ThreadStart(() =>
        {
            GenBase<MyIdClass0, int>.SetFieldsTest(fobj1, new MyIdClass0("11"), new MyIdClass0("22"), new MyIdClass0("33"), 11, 22, 33);
            GenBase<MyIdClass1, int>.SetFieldsTest(fobj2, new MyIdClass1("11"), new MyIdClass1("22"), new MyIdClass1("33"), 11, 22, 33);

            GenBase<MyIdClass0, int>.GetFieldsTest(fobj1, "MyIdClass0=11", "MyIdClass0=22", "MyIdClass0=33", 11, 22, 33);
            GenBase<MyIdClass1, int>.GetFieldsTest(fobj2, "MyIdClass1=11", "MyIdClass1=22", "MyIdClass1=33", 11, 22, 33);
        }));
        t.Start();
        t.Join();

        GenBase<MyIdClass0, int>.GetFieldsTest(fobj1, "MyIdClass0=11", "MyIdClass0=22", "MyIdClass0=3", 11, 22, 3);
        GenBase<MyIdClass1, int>.GetFieldsTest(fobj2, "MyIdClass1=11", "MyIdClass1=22", "MyIdClass1=3", 11, 22, 3);
    }

    static void RunTest5()
    {
        // DELEGATES TEST
        var fobj1 = new GenBase<MyIdClass0, int>();
        var fobj2 = new GenBase<MyIdClass1, int>();

        Func<MyIdClass0, int, string>[] del1 = fobj1.GetDelegateTest();
        Func<MyIdClass1, int, string>[] del2 = fobj2.GetDelegateTest();
        Assert.AreEqual(del1[0](new MyIdClass0("1"), 1), "InstanceDelMethod(GenBase`2[MyIdClass0,System.Int32] - MyIdClass0=1 - 1)");
        Assert.AreEqual(del1[1](new MyIdClass0("2"), 2), "StaticDelMethod(MyIdClass0=2 - 2)");
        Assert.AreEqual(del2[0](new MyIdClass1("3"), 3), "InstanceDelMethod(GenBase`2[MyIdClass1,System.Int32] - MyIdClass1=3 - 3)");
        Assert.AreEqual(del2[1](new MyIdClass1("4"), 4), "StaticDelMethod(MyIdClass1=4 - 4)");
        Assert.AreEqual(del1[0](new MyIdClass0("5"), 5), "InstanceDelMethod(GenBase`2[MyIdClass0,System.Int32] - MyIdClass0=5 - 5)");
        Assert.AreEqual(del1[1](new MyIdClass0("6"), 6), "StaticDelMethod(MyIdClass0=6 - 6)");
    }

    static void RunTest6()
    {
        // BOXING AND NULLABLE TEST
        var mi = new GenBase<MyClass0, int>();
        var ol = new GenBase<object, long>();

        Assert.AreEqual(mi.BoxingAndNullableTest(
            new MyGenClass1<KeyValuePair<MyClass0, int>>(),
            new MyGenStruct1<Dictionary<MyClass0, int>>(),
            new MyGenStruct1<Dictionary<MyClass0, int>>()),
            "BoxingAndNullableTest - GenBase`2[MyClass0,System.Int32]::(MyGenClass1`1[System.Collections.Generic.KeyValuePair`2[MyClass0,System.Int32]] - MyGenStruct1`1[System.Collections.Generic.Dictionary`2[MyClass0,System.Int32]] - MyGenStruct1`1[System.Collections.Generic.Dictionary`2[MyClass0,System.Int32]])");
        Assert.AreEqual(ol.BoxingAndNullableTest(
            new MyGenClass1<KeyValuePair<object, long>>(),
            new MyGenStruct1<Dictionary<object, long>>(),
            new MyGenStruct1<Dictionary<object, long>>()),
            "BoxingAndNullableTest - GenBase`2[System.Object,System.Int64]::(MyGenClass1`1[System.Collections.Generic.KeyValuePair`2[System.Object,System.Int64]] - MyGenStruct1`1[System.Collections.Generic.Dictionary`2[System.Object,System.Int64]] - MyGenStruct1`1[System.Collections.Generic.Dictionary`2[System.Object,System.Int64]])");
    }

    static void RunTest7()
    {
        // GENERIC METHOD TEST

        Base b = new Base();
        var obj1 = new GenBase<MyClass1, int>();
        var obj2 = new GenBase<MyClass2, long>();

        // LDTOKEN OF TYPE PARAMETERS TEST
        Assert.AreEqual(b.GetT<string>().ToString(), "System.String");
        Assert.AreEqual(b.GetT<object>().ToString(), "System.Object");
        Assert.AreEqual(b.GetTArray<string>().ToString(), "System.String[]");
        Assert.AreEqual(b.GetTArray<object>().ToString(), "System.Object[]");
        Assert.AreEqual(b.GetTBasedInst<string>().ToString(), "MyGenClass2`1[MyGenClass1`1[System.String]]");
        Assert.AreEqual(b.GetTBasedInst<object>().ToString(), "MyGenClass2`1[MyGenClass1`1[System.Object]]");

        Assert.AreEqual(b.GetT<MyClass1, int>(), "MyClass1");
        Assert.AreEqual(b.GetU<MyClass1, int>(), "System.Int32");
        Assert.AreEqual(b.GetT<MyClass2, long>(), "MyClass2");
        Assert.AreEqual(b.GetU<MyClass2, long>(), "System.Int64");

        // GENERIC INTERFACE CALL AND CASTING TEST
        Assert.AreEqual(b.IFaceCallTest<MyClass1, int>(obj1), "IFaceCallTest = IFooFunc - GenBase`2[MyClass1,System.Int32]");
        Assert.AreEqual(b.IFaceCallTest<MyClass2, long>(obj2), "IFaceCallTest = IFooFunc - GenBase`2[MyClass2,System.Int64]");

        // LDTOKEN TEST
        Assert.AreEqual(b.LdTokenTest<MyClass1, int>(), "System.Collections.Generic.Dictionary`2[MyClass1,System.Int32]");
        Assert.AreEqual(b.LdTokenTest<MyClass2, long>(), "System.Collections.Generic.Dictionary`2[MyClass2,System.Int64]");

        // DICTIONARY ACCESS FROM STATIC METHOD
        Assert.AreEqual(Base.StaticGenMethod<float, MyClass1>(), "StaticGenMethod - System.Collections.Generic.Dictionary`2[MyClass1,System.Single]");
        Assert.AreEqual(Base.StaticGenMethod<float, MyClass2>(), "StaticGenMethod - System.Collections.Generic.Dictionary`2[MyClass2,System.Single]");

        // NEW TEST
        Assert.AreEqual(b.NewTest<MyClass1, MyClass2>(),
            "NewTest - MyClass1 - MyGenClass1`1[MyClass1] - MyClass1[] - MyClass1[,] - MyGenClass2`1[MyClass1][] - MyGenClass2`1[MyClass1][,]");
        Assert.AreEqual(b.NewTest<MyClass2, MyClass1>(),
            "NewTest - MyClass2 - MyGenClass1`1[MyClass2] - MyClass2[] - MyClass2[,] - MyGenClass2`1[MyClass2][] - MyGenClass2`1[MyClass2][,]");

        // BOXING AND NULLABLE TEST
        Assert.AreEqual(b.BoxingAndNullableTest<MyClass0, int>(
            new MyGenClass1<KeyValuePair<MyClass0, int>>(),
            new MyGenStruct1<Dictionary<MyClass0, int>>(),
            new MyGenStruct1<Dictionary<MyClass0, int>>()),
            "BoxingAndNullableTest - Base::(MyGenClass1`1[System.Collections.Generic.KeyValuePair`2[MyClass0,System.Int32]] - MyGenStruct1`1[System.Collections.Generic.Dictionary`2[MyClass0,System.Int32]] - MyGenStruct1`1[System.Collections.Generic.Dictionary`2[MyClass0,System.Int32]])");
        Assert.AreEqual(b.BoxingAndNullableTest<object, int>(
            new MyGenClass1<KeyValuePair<object, int>>(),
            new MyGenStruct1<Dictionary<object, int>>(),
            new MyGenStruct1<Dictionary<object, int>>()),
            "BoxingAndNullableTest - Base::(MyGenClass1`1[System.Collections.Generic.KeyValuePair`2[System.Object,System.Int32]] - MyGenStruct1`1[System.Collections.Generic.Dictionary`2[System.Object,System.Int32]] - MyGenStruct1`1[System.Collections.Generic.Dictionary`2[System.Object,System.Int32]])");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string GetString<X, Y>(X x, Y y)
    {
        return string.Join(" ", x, y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static GenClass1a<T> CallGenVirtMethod<T>(NormalClass n)
    {
        return n.GetGenClass1a<T>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
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
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string CreateGenClass1b()
    {
        var x = new GenClass1b<T>();
        return x.ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string CreateGenClass1bArray()
    {
        var x = new GenClass1b<T>[3];
        return x.ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string CallVirtual(GenClass1b<T> x)
    {
        return x.VirtualMethod();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string CallInterface(IGenInterface<T> x)
    {
        return x.InterfaceMethod1();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IGenInterface<U> CallInterface<U, V>(IGenInterface<U> x, V v)
    {
        return x.InterfaceMethod2(v);
    }
}

class GenClass1b<T> : IGenInterface<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual string VirtualMethod()
    {
        return ToString() + ".VirtualMethod";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual string InterfaceMethod1()
    {
        return ToString() + ".InterfaceMethod1";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual IGenInterface<T> InterfaceMethod2<U>(U u)
    {
        return this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public bool IsGenClass1a(object o)
    {
        return o is GenClass1a<T>;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GenClass1a<T> AsGenClass1a(object o)
    {
        return o as GenClass1a<T>;
    }
}

class GenClass1c<T> where T : new()
{
    public T t;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GenClass1c()
    {
        t = new T();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GenClass1c(T _t)
    {
        t = _t;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void SetT(object x)
    {
        t = (T)x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override string ToString()
    {
        return t.ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string ToStringEx<X>(X x)
    {
        return string.Join(" ", t, x);
    }
}

class GenClass2<T, U>
{
    T t;
    U u;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public GenClass2(T t, U u)
    {
        this.t = t;
        this.u = u;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public override string ToString()
    {
        return t.ToString() + " " + u.ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string ToStringEx<X, Y>(X x, Y y)
    {
        return string.Join(" ", t, u, x, y);
    }
}

class NormalClass
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual GenClass1a<T> GetGenClass1a<T>()
    {
        return new GenClass1a<T>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual IEnumerable<T> GetEnumerable<T>(object o)
    {
        T[] array = new T[1];
        array[0] = (T)o;
        return array;
    }
}

public interface IFoo<T> { string IFooFunc(); }
public class MyClass0 { }
public class MyClass1 { }
public class MyClass2 { }
public class MyGenClass1<T> { public override string ToString() { return this.GetType().ToString(); } }
public class MyGenClass2<T> { public override string ToString() { return this.GetType().ToString(); } }
public class MyGenClass3<T> { public override string ToString() { return this.GetType().ToString(); } }
public struct MyGenStruct1<T> { public override string ToString() { return this.GetType().ToString(); } }
public class MyIdClass0 { string _id; public MyIdClass0() { } public MyIdClass0(string id) { _id = id; } public override string ToString() { return "MyIdClass0=" + _id; } }
public class MyIdClass1 { string _id; public MyIdClass1() { } public MyIdClass1(string id) { _id = id; } public override string ToString() { return "MyIdClass1=" + _id; } }

public class GenBase<T, U> : IFoo<T> where T : new()
{
    public T m_fieldT;
    public U m_fieldU;
    public static T s_fieldT;
    public static U s_fieldU;
    [ThreadStatic]
    public static T st_fieldT;
    [ThreadStatic]
    public static U st_fieldU;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SetFieldsTest(GenBase<T, U> obj, T t1, T t2, T t3, U u1, U u2, U u3)
    {
        obj.m_fieldT = t1;
        GenBase<T, U>.s_fieldT = t2;
        GenBase<T, U>.st_fieldT = t3;

        obj.m_fieldU = u1;
        GenBase<T, U>.s_fieldU = u2;
        GenBase<T, U>.st_fieldU = u3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void GetFieldsTest(GenBase<T, U> obj, string s1, string s2, string s3, U u1, U u2, U u3)
    {
        Assert.AreEqual(obj.m_fieldT.ToString(), s1);
        Assert.AreEqual(GenBase<T, U>.s_fieldT.ToString(), s2);
        Assert.AreEqual(GenBase<T, U>.st_fieldT.ToString(), s3);

        Assert.AreEqual(obj.m_fieldU, u1);
        Assert.AreEqual(GenBase<T, U>.s_fieldU, u2);
        Assert.AreEqual(GenBase<T, U>.st_fieldU, u3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    // BUG BUG BUG: bad codegen when method is private
    public string InstanceDelMethod(T t, U u)
    {
        return "InstanceDelMethod(" + this + " - " + t + " - " + u + ")";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    // BUG BUG BUG: bad codegen when method is private
    public static string StaticDelMethod(T t, U u)
    {
        return "StaticDelMethod(" + t + " - " + u + ")";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Func<T, U, string>[] GetDelegateTest()
    {
        Func<T, U, string> del1 = this.InstanceDelMethod;
        Func<T, U, string> del2 = StaticDelMethod;
        return new Func<T, U, string>[] { del1, del2 };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string IFooFunc()
    {
        return "IFooFunc - " + this.ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string IFaceCallTest(object ifoo)
    {
        IFoo<T> i = (IFoo<T>)ifoo;
        return "IFaceCallTest = " + i.IFooFunc();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetT()
    {
        return typeof(T).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetU()
    {
        return typeof(U).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetTArray()
    {
        return typeof(T[]).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetTBasedInst()
    {
        return typeof(MyGenClass2<MyGenClass1<T>>).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string LdTokenTest()
    {
        return "LdTokenTest - " + typeof(Dictionary<T, U>).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string StaticNonGenMethod()
    {
        return "StaticNonGenMethod - " + typeof(List<T>).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string StaticGenMethod<V>()
    {
        return "StaticGenMethod - " + typeof(Dictionary<V, T>).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string NewTest()
    {
        var a = new T();
        var b = new MyGenClass1<T>();
        var c = new T[10];
        var d = new T[30,30];
        var e = new MyGenClass3<T>[5];
        var f = new MyGenClass3<T>[5,13];
        return "NewTest - " + a + " - " + b + " - " + c + " - " + d + " - " + e + " - " + f;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string BoxingAndNullableTest(MyGenClass1<KeyValuePair<T,U>> t, MyGenStruct1<Dictionary<T,U>> u, MyGenStruct1<Dictionary<T,U>>? u2)
    {
        return "BoxingAndNullableTest - " + this + "::(" + t + " - " + u + " - " + u2 + ")";
    }
}

public class Base
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetT<T>()
    {
        return typeof(T).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetT<T, U>()
    {
        return typeof(T).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetU<T, U>()
    {
        return typeof(U).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetTArray<T>()
    {
        return typeof(T[]).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string GetTBasedInst<T>()
    {
        return typeof(MyGenClass2<MyGenClass1<T>>).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string IFaceCallTest<T, U>(object ifoo)
    {
        IFoo<T> i = (IFoo<T>)ifoo;
        return "IFaceCallTest = " + i.IFooFunc();
    }


    [MethodImpl(MethodImplOptions.NoInlining)]
    public string LdTokenTest<T, U>()
    {
        return typeof(Dictionary<T, U>).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string StaticGenMethod<T, U>()
    {
        return "StaticGenMethod - " + typeof(Dictionary<U, T>).ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string NewTest<T, U>() where T : new()
    {
        var a = new T();
        var b = new MyGenClass1<T>();
        var c = new T[10];
        var d = new T[30, 30];
        var e = new MyGenClass2<T>[5];
        var f = new MyGenClass2<T>[5, 13];
        return "NewTest - " + a + " - " + b + " - " + c + " - " + d + " - " + e + " - " + f;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public string BoxingAndNullableTest<T, U>(MyGenClass1<KeyValuePair<T, U>> t, MyGenStruct1<Dictionary<T, U>> u, MyGenStruct1<Dictionary<T, U>>? u2)
    {
        return "BoxingAndNullableTest - " + this + "::(" + t + " - " + u + " - " + u2 + ")";
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
