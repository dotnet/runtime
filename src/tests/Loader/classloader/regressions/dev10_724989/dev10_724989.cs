// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Xunit;

//====================  Cases of nested classes  ====================//
class Outer1
{
    protected int field;

    public class Inner
    {
        public int Method(Outer1 param) { return (++param.field); }
    }
}
class Outer2
{
    protected int field;

    public class Inner<U> where U : Outer2
    {
        public int Method(U param) { return (++param.field); }
    }
}
class Outer3<T>
{
    protected int field;

    public class Inner
    {
        public int Method(Outer3<T> param) { return (++param.field); }
    }
}
class Outer4<T>
{
    protected int field;

    public class Inner<U> where U : Outer4<T>
    {
        public int Method(U param) { return (++param.field); }
    }
}



//====================  Cases of derived classes  ====================//
class Base1
{
    protected int field;
}
class Derived1 : Base1
{
    public class Inner
    {
        public int Method(Derived1 param) { return (++param.field); }
    }
}
class Base2
{
    protected int field;
}
class Derived2<T> : Base2
{
    public class Inner
    {
        public int Method(Derived2<T> param) { return (++param.field); }
    }
}
class Base3<T>
{
    protected int field;
}
class Derived3 : Base3<string>
{
    public class Inner
    {
        public int Method(Derived3 param) { return (++param.field); }
    }
}
class Base4<T>
{
    protected int field;
}
class Derived4<T> : Base4<T>
{
    public class Inner
    {
        public int Method(Derived4<T> param) { return (++param.field); }
    }
}


public class Test_dev10_724989
{
    static bool Success = true;

    static void NestedClassesTest()
    {
        int res;

        //====================  Cases of nested classes  ====================//
        Outer1.Inner inner1 = new Outer1.Inner();
        res = inner1.Method(new Outer1());
        Console.WriteLine("Outer1.Inner.Method(new Outer1()) = " + res);
        if (res != 1) Success = false;

        Outer2.Inner<Outer2> inner2 = new Outer2.Inner<Outer2>();
        res = inner2.Method(new Outer2());
        Console.WriteLine("Outer2.Inner<Outer2>.Method(new Outer2()) = " + res);
        if (res != 1) Success = false;

        Outer3<string>.Inner inner3 = new Outer3<string>.Inner();
        res = inner3.Method(new Outer3<string>());
        Console.WriteLine("Outer3<string>.Inner.Method(new Outer3<string>()) = " + res);
        if (res != 1) Success = false;

        Outer4<string>.Inner<Outer4<string>> inner4 = new Outer4<string>.Inner<Outer4<string>>();
        res = inner4.Method(new Outer4<string>());
        Console.WriteLine("Outer4<string>.Inner<Outer4<string>>.Method(new Outer4<string>()) = " + res);
        if (res != 1) Success = false;
    }

    static void DerivedClassesTest()
    {
        int res;

        //====================  Cases of derived classes  ====================//
        Derived1.Inner inner1 = new Derived1.Inner();
        res = inner1.Method(new Derived1());
        Console.WriteLine("Derived1.Inner.Method(new Derived1()) = " + res);
        if (res != 1) Success = false;

        Derived2<string>.Inner inner2 = new Derived2<string>.Inner();
        res = inner2.Method(new Derived2<string>());
        Console.WriteLine("Derived2<string>.Inner.Method(new Derived2<string>()) = " + res);
        if (res != 1) Success = false;

        Derived3.Inner inner3 = new Derived3.Inner();
        res = inner3.Method(new Derived3());
        Console.WriteLine("Derived3.Inner.Method(new Derived3()) = " + res);
        if (res != 1) Success = false;

        Derived4<string>.Inner inner4 = new Derived4<string>.Inner();
        res = inner4.Method(new Derived4<string>());
        Console.WriteLine("Derived4<string>.Inner.Method(new Derived4<string>()) = " + res);
        if (res != 1) Success = false;
    }


    [Fact]
    public static void TestEntryPoint()
    {
        NestedClassesTest();
        Console.WriteLine();

        DerivedClassesTest();
        Console.WriteLine();

        Assert.True(Success);
    }
}
