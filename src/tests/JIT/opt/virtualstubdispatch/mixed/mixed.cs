// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
internal interface ITest1
{
    int f1();
}

internal interface ITest2
{
    int f2();
}

internal interface ITest3
{
    int f2();
}

internal interface IBase1
{
    int f3();
}
internal interface IDerived1 : IBase1
{
    int f4();
}
internal interface IDerived2 : IBase1
{
    int f5();
}

internal interface ITest4
{
    int f6();
}

internal interface IBase
{
    int f2a { get; }
    int f2b { get; }
    int f2c { get; }
}
internal interface IDerived : IBase
{
    new int f2a();
    new int f2b();
    new int f2c();
}

internal interface ITest5
{
    int f8();
    int f9();
}

public class C : ITest5
{
    private int _code;
    public C()
    {
        _code = this.GetHashCode();
    }
    public int f6() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 13; }
    public int f7() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 14; }
    public int f8() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 16; }
    public virtual int f9() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 18; }
}

public class CTest : C, ITest1, ITest2, ITest3, ITest4, IBase1, IDerived1, IDerived2, IDerived
{
    private int _code;
    public CTest()
    {
        _code = this.GetHashCode();
    }
    int ITest1.f1() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 1; }
    public int f1() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 2; }

    public int f2() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 3; }

    int IBase.f2a { get { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 4; } }
    int IDerived.f2a() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 5; }
    public int f2b { get { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 6; } }
    int IDerived.f2b() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 7; }
    public int f2c { get { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 8; } }
    int IDerived.f2c() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 9; }

    int IBase1.f3() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 10; }
    int IDerived1.f4() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 11; }
    int IDerived2.f5() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 12; }

    new public int f7() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 15; }

    new public int f8() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 17; }
    override public int f9() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); if (this.GetHashCode() != _code) return 999; else return 19; }

    [Fact]
    public static int TestEntryPoint()
    {
        CTest t = new CTest();
        if (t.f1() != 2)
        {
            Console.WriteLine("t.f1()!= 2");
            return 1;
        }
        if (((ITest1)t).f1() != 1)
        {
            Console.WriteLine("((ITest1)t).f1()!=1");
            return 1;
        }
        if (t.f2() != 3)
        {
            Console.WriteLine("t.f2()!=3");
            return 1;
        }
        if (((IBase1)t).f3() != 10)
        {
            Console.WriteLine("((IBase1)t).f3()!= 10");
            return 1;
        }
        if (((IDerived1)t).f4() != 11)
        {
            Console.WriteLine("((IDerived1)t).f4()!=11");
            return 1;
        }
        if (((IDerived2)t).f5() != 12)
        {
            Console.WriteLine("((IDerived2)t).f5()!=12");
            return 1;
        }
        if (t.f6() != 13)
        {
            Console.WriteLine("t.f6()!= 13");
            return 1;
        }
        if (t.f7() != 15)
        {
            Console.WriteLine("t.f7()!= 15");
            return 1;
        }
        if (((IBase)t).f2a != 4)
        {
            Console.WriteLine("((IBase)t).f2a!=4");
            return 1;
        }
        if (((IDerived)t).f2b() != 7)
        {
            Console.WriteLine("((IDerived)t).f2b()!=7");
            return 1;
        }
        if (t.f2c != 8)
        {
            Console.WriteLine("t.f2c!=8");
            return 1;
        }
        if (((IDerived)t).f2a() != 5)
        {
            Console.WriteLine("((IDerived)t).f2a()!=5");
            return 1;
        }
        if (t.f2b != 6)
        {
            Console.WriteLine("t.f2b!=6");
            return 1;
        }
        if (((IDerived)t).f2c() != 9)
        {
            Console.WriteLine("((IDerived)t).f2c()!=9");
            return 1;
        }

        C c = new C();
        ITest5 ic = c;
        ITest5 it = t;
        if (c.f8() != 16)
        {
            Console.WriteLine("c.f8()!=16");
            return 1;
        }
        if (t.f8() != 17)
        {
            Console.WriteLine("t.f8()!=17");
            return 1;
        }
        if (ic.f8() != 16)
        {
            Console.WriteLine("ic.f8()!=16");
            return 1;
        }
        if (it.f8() != 16)
        {
            Console.WriteLine("it.f8()!=16");
            return 1;
        }

        if (c.f9() != 18)
        {
            Console.WriteLine("c.f9()!=18");
            return 1;
        }

        if (t.f9() != 19)
        {
            Console.WriteLine("t.f9()!=19");
            return 1;
        }

        if (ic.f9() != 18)
        {
            Console.WriteLine("ic.f9()!=18");
            return 1;
        }

        if (it.f9() != 19)
        {
            Console.WriteLine("it.f9()!=19");
            return 1;
        }

        Console.WriteLine("PASS");
        return 100;
    }
}








