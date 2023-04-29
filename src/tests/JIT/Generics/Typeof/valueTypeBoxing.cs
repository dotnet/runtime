// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

/* Unboxing where a parameter is types as System.ValueType, or System.Enum, and then is unboxed to its scalar type 
 */
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

interface X
{
    void incCount();
}

interface A : X
{
}

interface B : X
{
}

struct C : A
{
    public static int c_count = 1;
    public void incCount() { c_count *= 23; }
}

struct D : B
{
    public static int d_count = 1;
    public void incCount() { d_count *= 31; }
}

struct CSS : A
{
    public static int cs_count = 1;
    public void incCount() { cs_count *= 37; }
    public void readInput(int input) { cs_count = cs_count += input; }
}

struct DSS : B
{
    public static int ds_count = 1;
    public void incCount() { ds_count *= 41; }
    public void readInput(int input) { ds_count = ds_count += input; }
}

enum CS
{
    Day = 0, Night = 1
};

enum DS
{
    Day = 1, Night = 0
};

public struct mainMethod
{
    public static bool failed = false;
    public static void checkGetTypeValueType(System.ValueType x)
    {
        if (x.GetType() == typeof(D)) (new D()).incCount();
        if (x.GetType() == typeof(C)) (new C()).incCount();
    }

    public static void checkIsValueType(System.ValueType x)
    {
        if (x is C) (new C()).incCount();
        if (x is D) (new D()).incCount();
    }

    public static void checkGetTypeValueTypeCast(System.ValueType x)
    {
        if (x.GetType() == typeof(D)) ((D)x).incCount();
        if (x.GetType() == typeof(C)) ((C)x).incCount();
    }

    public static void checkIsValueTypeCast(System.ValueType x)
    {
        if (x is C) ((C)x).incCount();
        if (x is D) ((D)x).incCount();
    }

    public static void checkGetTypeEnum(System.Enum x)
    {
        if (x.GetType() == typeof(DS)) (new DSS()).incCount();
        if (x.GetType() == typeof(CS)) (new CSS()).incCount();
    }

    public static void checkIsEnum(System.Enum x)
    {
        if (x is CS) (new CSS()).incCount();
        if (x is DS) (new DSS()).incCount();
    }

    public static void checkGetTypeEnumCast(System.Enum x)
    {
        if (x.GetType() == typeof(DS)) (new DSS()).readInput(Convert.ToInt32(DS.Day));
        if (x.GetType() == typeof(CS)) (new CSS()).readInput(Convert.ToInt32(CS.Night));
    }

    public static void checkIsEnumCast(System.Enum x)
    {
        if (x is CS) (new CSS()).readInput(Convert.ToInt32(CS.Night));
        if (x is DS) (new DSS()).readInput(Convert.ToInt32(DS.Day));
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

    public static void checkAllCounts(ref int x_actual, int ds, int cs, int d, int c, string dsm, string csm, string dm, string cm)
    {

        /*
int tmp = 1;
        printCount(ref DSS.ds_count, ds, dsm);
        printCount(ref CSS.cs_count, cs, csm);
        printCount(ref D.d_count, d, dm);
        printCount(ref C.c_count, c, cm);
        printCount(ref tmp, b, bm);
        printCount(ref tmp, a, am);
        printCount(ref tmp, x, xm);
        //printCount(ref B.b_count, b, bm);
        //printCount(ref A.a_count, a, am);
        //printCount(ref x_actual, x, xm);

*/
        checkCount(ref DSS.ds_count, ds, dsm);
        checkCount(ref CSS.cs_count, cs, csm);
        checkCount(ref D.d_count, d, dm);
        checkCount(ref C.c_count, c, cm);
        //checkCount(ref B.b_count, b, bm);
        //checkCount(ref A.a_count, a, am);
        //checkCount(ref x_actual, x, xm);
    }

    public static void printCount(ref int actual, int expected, string message)
    {
        Console.Write("{0}, ", actual);
        actual = 1;
    }

    public static void callCheckGetTypeValueType()
    {
        int i = 0;
        int dummy = 1;
        checkGetTypeValueType(new C());
        checkAllCounts(ref dummy, 1, 1, 1, 23, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeValueType(new D());
        checkAllCounts(ref dummy, 1, 1, 31, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeEnum(new CS());
        checkAllCounts(ref dummy, 1, 37, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeEnum(new DS());
        checkAllCounts(ref dummy, 41, 1, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof");
        Console.WriteLine("-----------{0}", i++);
    }

    public static void callCheckIsValueType()
    {
        int i = 0;
        int dummy = 1;
        checkIsValueType(new C());
        checkAllCounts(ref dummy, 1, 1, 1, 23, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is");
        Console.WriteLine("-----------{0}", i++);
        checkIsValueType(new D());
        checkAllCounts(ref dummy, 1, 1, 31, 1, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is");
        Console.WriteLine("-----------{0}", i++);
        checkIsEnum(new CS());
        checkAllCounts(ref dummy, 1, 37, 1, 1, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is");
        Console.WriteLine("-----------{0}", i++);
        checkIsEnum(new DS());
        checkAllCounts(ref dummy, 41, 1, 1, 1, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is");
        Console.WriteLine("-----------{0}", i++);
    }


    public static void callCheckGetTypeValueTypeCast()
    {
        int i = 0;
        int dummy = 1;
        checkGetTypeValueTypeCast(new C());
        checkAllCounts(ref dummy, 1, 1, 1, 23, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeValueTypeCast(new D());
        checkAllCounts(ref dummy, 1, 1, 31, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeEnumCast(new CS());
        checkAllCounts(ref dummy, 1, 2, 1, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
        checkGetTypeEnumCast(new DS());
        checkAllCounts(ref dummy, 2, 1, 1, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast");
        Console.WriteLine("-----------{0}", i++);
    }

    public static void callCheckIsValueTypeCast()
    {
        int i = 0;
        int dummy = 1;
        checkIsValueTypeCast(new C());
        checkAllCounts(ref dummy, 1, 1, 1, 23, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkIsValueTypeCast(new D());
        checkAllCounts(ref dummy, 1, 1, 31, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkIsEnumCast(new CS());
        checkAllCounts(ref dummy, 1, 2, 1, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
        checkIsEnumCast(new DS());
        checkAllCounts(ref dummy, 2, 1, 1, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ");
        Console.WriteLine("-----------{0}", i++);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        callCheckGetTypeValueType();
        callCheckIsValueType();
        callCheckGetTypeValueTypeCast();
        callCheckIsValueTypeCast();
        if (failed) return 101; else return 100;
    }
}
