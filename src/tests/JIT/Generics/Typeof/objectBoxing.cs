// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

/* unboxing where a parameter is types as object and then is unboxed to its scalar type 
 */
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

class X
{
    public static int x_count = 1;
    public virtual void incCount()
    {
        x_count *= 2;
    }
}

class A : X
{
    public static int a_count = 1;
    public override void incCount() { a_count *= 17; }
}

class B : X
{
    public static int b_count = 1;
    public override void incCount() { b_count *= 19; }
}

class C : A
{
    public static int c_count = 1;
    public override void incCount() { c_count *= 23; }
}

class D : B
{
    public static int d_count = 1;
    public override void incCount() { d_count *= 31; }
}

sealed class CS : A
{
    public static int cs_count = 1;
    public override void incCount() { cs_count *= 37; }
}

sealed class DS : B
{
    public static int ds_count = 1;
    public override void incCount() { ds_count *= 41; }
}

public class mainMethod
{
    public static bool failed = false;
    public static void checkGetType(System.Object x)
    {
        if (x.GetType() == typeof(DS)) (new DS()).incCount();
        if (x.GetType() == typeof(CS)) (new CS()).incCount();
        if (x.GetType() == typeof(D)) (new D()).incCount();
        if (x.GetType() == typeof(C)) (new C()).incCount();
        if (x.GetType() == typeof(B)) (new B()).incCount();
        if (x.GetType() == typeof(A)) (new A()).incCount();
        if (x.GetType() == typeof(X)) (new X()).incCount();
        if (x.GetType() == null) (new X()).incCount();
    }

    public static void checkIs(System.Object x)
    {
        if (x is X) (new X()).incCount();
        if (x is A) (new A()).incCount();
        if (x is B) (new B()).incCount();
        if (x is C) (new C()).incCount();
        if (x is D) (new D()).incCount();
        if (x is CS) (new CS()).incCount();
        if (x is DS) (new DS()).incCount();
    }

    public static void checkAs(System.Object x)
    {
        X x1 = x as X;
        if (x1 != null) (new X()).incCount();
        A a = x as A;
        if (a != null) (new A()).incCount();
        B b = x as B;
        if (b != null) (new B()).incCount();
        C c = x as C;
        if (c != null) (new C()).incCount();
        D d = x as D;
        if (d != null) (new D()).incCount();
        CS cs = x as CS;
        if (cs != null) (new CS()).incCount();
        DS ds = x as DS;
        if (ds != null) (new DS()).incCount();
    }

    public static void checkGetTypeObjectCast(System.Object x)
    {
        if (x.GetType() == typeof(DS)) ((DS)x).incCount();
        if (x.GetType() == typeof(CS)) ((CS)x).incCount();
        if (x.GetType() == typeof(D)) ((D)x).incCount();
        if (x.GetType() == typeof(C)) ((C)x).incCount();
        if (x.GetType() == typeof(B)) ((B)x).incCount();
        if (x.GetType() == typeof(A)) ((A)x).incCount();
        if (x.GetType() == typeof(X)) ((X)x).incCount();
        if (x.GetType() == null) ((X)x).incCount();
    }

    public static void checkIsObjectCast(System.Object x)
    {
        if (x is X) ((X)x).incCount();
        if (x is A) ((A)x).incCount();
        if (x is B) ((B)x).incCount();
        if (x is C) ((C)x).incCount();
        if (x is D) ((D)x).incCount();
        if (x is CS) ((CS)x).incCount();
        if (x is DS) ((DS)x).incCount();
    }

    public static void checkAsObjectCast(System.Object x)
    {
        X x2 = x as X;
        if (x2 != null) ((X)x).incCount();
        A a = x as A;
        if (a != null) ((A)x).incCount();
        B b = x as B;
        if (b != null) ((B)x).incCount();
        C c = x as C;
        if (c != null) ((C)x).incCount();
        D d = x as D;
        if (d != null) ((D)x).incCount();
        CS cs = x as CS;
        if (cs != null) ((CS)x).incCount();
        DS ds = x as DS;
        if (ds != null) ((DS)x).incCount();
    }

    public static void checkCount(ref int actual, int expected, string message)
    {
        if (actual != expected)
        {
            Console.WriteLine("FAILED: {0}", message);
            failed = true;
        }
        actual = 1;
    }

    public static void checkAllCounts(ref int x_actual, int ds, int cs, int d, int c, int b, int a, int x, string dsm, string csm, string dm, string cm, string bm, string am, string xm)
    {

        /*
        printCount(ref DS.ds_count, ds, dsm);
        printCount(ref CS.cs_count, cs, csm);
        printCount(ref D.d_count, d, dm);
        printCount(ref C.c_count, c, cm);
        printCount(ref B.b_count, b, bm);
        printCount(ref A.a_count, a, am);
        printCount(ref x_actual, x, xm);

*/
        checkCount(ref DS.ds_count, ds, dsm);
        checkCount(ref CS.cs_count, cs, csm);
        checkCount(ref D.d_count, d, dm);
        checkCount(ref C.c_count, c, cm);
        checkCount(ref B.b_count, b, bm);
        checkCount(ref A.a_count, a, am);
        checkCount(ref x_actual, x, xm);
    }

    public static void printCount(ref int actual, int expected, string message)
    {
        Console.Write("{0}, ", actual);
        actual = 1;
    }

    public static void callCheckGetType()
    {
        int i = 0;
        checkGetType(new System.Object());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
        checkGetType(new X());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
        checkGetType(new A());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 17, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
        checkGetType(new B());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 19, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
        checkGetType(new C());
        checkAllCounts(ref X.x_count, 1, 1, 1, 23, 1, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
        checkGetType(new D());
        checkAllCounts(ref X.x_count, 1, 1, 31, 1, 1, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
        checkGetType(new CS());
        checkAllCounts(ref X.x_count, 1, 37, 1, 1, 1, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
        checkGetType(new DS());
        checkAllCounts(ref X.x_count, 41, 1, 1, 1, 1, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
    }

    public static void callCheckIs()
    {
        int i = 0;
        checkIs(new System.Object());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 1, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
        Console.WriteLine("-----------{0}", i++);
        checkIs(new X());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
        Console.WriteLine("-----------{0}", i++);
        checkIs(new A());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 17, 2, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
        Console.WriteLine("-----------{0}", i++);
        checkIs(new B());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 19, 1, 2, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
        Console.WriteLine("-----------{0}", i++);
        checkIs(new C());
        checkAllCounts(ref X.x_count, 1, 1, 1, 23, 1, 17, 2, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
        Console.WriteLine("-----------{0}", i++);
        checkIs(new D());
        checkAllCounts(ref X.x_count, 1, 1, 31, 1, 19, 1, 2, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
        Console.WriteLine("-----------{0}", i++);
        checkIs(new CS());
        checkAllCounts(ref X.x_count, 1, 37, 1, 1, 1, 17, 2, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
        Console.WriteLine("-----------{0}", i++);
        checkIs(new DS());
        checkAllCounts(ref X.x_count, 41, 1, 1, 1, 19, 1, 2, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
        Console.WriteLine("-----------{0}", i++);
    }

    public static void callCheckAs()
    {
        int i = 0;
        checkAs(new System.Object());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 1, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
        Console.WriteLine("-----------{0}", i++);
        checkAs(new X());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
        Console.WriteLine("-----------{0}", i++);
        checkAs(new A());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 17, 2, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
        Console.WriteLine("-----------{0}", i++);
        checkAs(new B());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 19, 1, 2, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
        Console.WriteLine("-----------{0}", i++);
        checkAs(new C());
        checkAllCounts(ref X.x_count, 1, 1, 1, 23, 1, 17, 2, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
        Console.WriteLine("-----------{0}", i++);
        checkAs(new D());
        checkAllCounts(ref X.x_count, 1, 1, 31, 1, 19, 1, 2, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
        Console.WriteLine("-----------{0}", i++);
        checkAs(new CS());
        checkAllCounts(ref X.x_count, 1, 37, 1, 1, 1, 17, 2, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
        Console.WriteLine("-----------{0}", i++);
        checkAs(new DS());
        checkAllCounts(ref X.x_count, 41, 1, 1, 1, 19, 1, 2, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
        Console.WriteLine("-----------{0}", i++);
    }

    public static void callCheckGetTypeObjectCast()
    {
        int i = 0;
        checkGetTypeObjectCast(new System.Object());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeObjectCast(new X());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeObjectCast(new A());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 17, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeObjectCast(new B());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 19, 1, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeObjectCast(new C());
        checkAllCounts(ref X.x_count, 1, 1, 1, 23, 1, 1, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeObjectCast(new D());
        checkAllCounts(ref X.x_count, 1, 1, 31, 1, 1, 1, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeObjectCast(new CS());
        checkAllCounts(ref X.x_count, 1, 37, 1, 1, 1, 1, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeObjectCast(new DS());
        checkAllCounts(ref X.x_count, 41, 1, 1, 1, 1, 1, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
    }

    public static void callCheckIsObjectCast()
    {
        int i = 0;
        checkIsObjectCast(new System.Object());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkIsObjectCast(new X());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkIsObjectCast(new A());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 289, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkIsObjectCast(new B());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 361, 1, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkIsObjectCast(new C());
        checkAllCounts(ref X.x_count, 1, 1, 1, 12167, 1, 1, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkIsObjectCast(new D());
        checkAllCounts(ref X.x_count, 1, 1, 29791, 1, 1, 1, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkIsObjectCast(new CS());
        checkAllCounts(ref X.x_count, 1, 50653, 1, 1, 1, 1, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkIsObjectCast(new DS());
        checkAllCounts(ref X.x_count, 68921, 1, 1, 1, 1, 1, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
    }

    public static void callCheckAsObjectCast()
    {
        int i = 0;
        checkAsObjectCast(new System.Object());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 1, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkAsObjectCast(new X());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkAsObjectCast(new A());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 1, 289, 1, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkAsObjectCast(new B());
        checkAllCounts(ref X.x_count, 1, 1, 1, 1, 361, 1, 1, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkAsObjectCast(new C());
        checkAllCounts(ref X.x_count, 1, 1, 1, 12167, 1, 1, 1, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkAsObjectCast(new D());
        checkAllCounts(ref X.x_count, 1, 1, 29791, 1, 1, 1, 1, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkAsObjectCast(new CS());
        checkAllCounts(ref X.x_count, 1, 50653, 1, 1, 1, 1, 1, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkAsObjectCast(new DS());
        checkAllCounts(ref X.x_count, 68921, 1, 1, 1, 1, 1, 1, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
        Console.WriteLine("-----------{0}", i++);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        callCheckGetType();
        callCheckIs();
        callCheckAs();
        callCheckGetTypeObjectCast();
        callCheckIsObjectCast();
        callCheckAsObjectCast();
        if (failed) return 101; else return 100;
    }
}
