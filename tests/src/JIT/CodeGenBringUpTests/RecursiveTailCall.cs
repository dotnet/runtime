// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

public struct Struct1
{
    public bool bfield;

    public Struct1(bool b)
    {
        bfield = b;
    }
}

public struct Struct8
{
    public double dfield;

    public Struct8(double d)
    {
        dfield = d;
    }
}

public class Test
{
    // Test a recursive tail call with a 1-byte struct parameter.
    static bool TestStruct1Param(Struct1 str1, int count)
    {
        Console.WriteLine(count);
        Console.WriteLine(count);
        Console.WriteLine(count);
        Console.WriteLine(count);
        if (!CheckStruct1(str1))
        {
            return false;
        }

        if (count <= 0)
        {
            return true;
        }

        return TestStruct1Param(str1, count - 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CheckStruct1(Struct1 str1)
    {
        return str1.bfield;
    }

    // Test a recursive tail call with an 8-byte struct parameter.
    static bool TestStruct8Param(Struct8 str8, int count)
    {
        Console.WriteLine(count);
        Console.WriteLine(count);
        Console.WriteLine(count);
        Console.WriteLine(count);
        if (!CheckStruct8(str8))
        {
            return false;
        }

        if (count <= 0)
        {
            return true;
        }

        return TestStruct8Param(str8, count - 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CheckStruct8(Struct8 str8)
    {
        return (str8.dfield == 1.0);
    }

    // Test a recursive tail call to a method with generic sharing.
    bool TestGenericSharing<T>()
    {
        if (typeof(T) == typeof(string))
        {
            return true;
        }
        else
        {
            return TestGenericSharing<string>();
        }
    }

    // Test a recursive tail call to a method that has a 'this' parameter
    // and a parameter passed on the stack.
    bool TestStackParam(int i1, int i2, int i3, int i4)
    {
        if ((i3 != 5) || (i4 != 7))
        {
            return false;
        }
        if (i1 == 0)
        {
            return true;
        }
        return TestStackParam(i1 - 1, i2, i3, i4);
    }

    public static int Main()
    {
        const int Pass = 100;
        const int Fail = -1;

        Struct1 str1 = new Struct1(true);

        if (!TestStruct1Param(str1, 4))
        {
            return Fail;
        }

        Struct8 str8 = new Struct8(1.0);

        if (!TestStruct8Param(str8, 4))
        {
            return Fail;
        }

        Test test = new Test();

        if (!test.TestGenericSharing<object>())
        {
            return Fail;
        }

        if (!test.TestStackParam(1, 0, 5, 7))
        {
            return Fail;
        }

        return Pass;
    }
}
