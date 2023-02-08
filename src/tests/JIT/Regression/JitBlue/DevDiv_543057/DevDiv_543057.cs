// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This tests passing variables in registers, which result in pass-through
// "specialPutArg" Intervals, and then passing a struct with GT_FIELD_LIST
// that also uses a number of registers.
// This case exposed a bug in the stress-limiting of registers where it
// wasn't taking these live "specialPutArg" Intervals into account.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Struct with 4 fields
public struct MyStruct
{
    public int f1;
    public int f2;
    public int f3;
    public int f4;
}

public class TestClass
{
    public const int Pass = 100;
    public const int Fail = -1;

    public TestClass()
    {
    }

    // This is just here to set our lclVar to a non-constant.
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public int Dummy1()
    {
        return 1;
    }

    // This is here to preference our lclVars to the appropriate parameter registers.
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public void Dummy2(int p1, int p2, int p3, int p4)
    {
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public int Check1(int i1, int i2, MyStruct s1, int i3, int i4)
    {
        if ((s1.f1 != i1) || (s1.f2 != i2) || (s1.f3 != i3) || (s1.f4 != i4))
        {
            Console.WriteLine("Check1: FAIL");
            return Fail;
        }
        Console.WriteLine("Check1: PASS");
        return Pass;
    }

    // For this repro, we want two parameters that are "specialPutArg" - that is,
    // they are passing lclVars that are live in the register they are wanted in,
    // and we have to keep them live, because there's no way to mark it as spilled,
    // in case it is used as another parameter prior to being killed by the call.
    // To do this, we set up 'this' and 'a' such that they are preferenced to
    // the first and second parameter registers, but we increase register pressure
    // so that they have to be loaded just prior to the call, causing them to
    // be put into the argument registers they are preferenced to. Then we
    // pass a split struct (ARM) that uses 4 registers for its GT_FIELD_LIST.
    // This exceeds the stress limit without any compensation for the "specialPutArg"s.
    //
    public int TestStruct()
    {
        MyStruct s1;
        s1.f1 = 1; s1.f2 = 2; s1.f3 = 3; s1.f4 = 4;

        int a = Dummy1();

        // Call an instance method. This gets the value number for 'this' into the 'curSsaNames' in copyprop.
        // Also, pass it a bunch of parameters to increase the register pressure here.
        Dummy2(a, 2, 3, 4);

        // Pass the struct as split ('this' is in the first arg register).
        int retVal = Check1(a, 2, s1, 3, 4);

        // Use 'this' again so our previous call isn't the last use.
        retVal = Check1(a, 2, s1, 3, 4);

        return retVal;
    }
}

public class DevDiv_543057
{
    [Fact]
    public static int TestEntryPoint()
    {
        int retVal = TestClass.Pass;
        TestClass c = new TestClass();
        if (c.TestStruct() != TestClass.Pass)
        {
            retVal = TestClass.Fail;
        }
        return retVal;
    }
}
