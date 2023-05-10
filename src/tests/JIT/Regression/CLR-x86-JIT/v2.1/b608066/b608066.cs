// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//The test checks for a gchole and an assert failure.
//The test should print out 33 and 3 if it passed the gchole and assert tests.
using System;
using System.Runtime.CompilerServices;
using Xunit;

class IntWrapper
{
    public int value;
}

public class ReproTwo
{
    static IntWrapper Add36(int ecx, int edx, int i3, int i4, int i5, int i6,
                                              int i7, int i8, int i9, int i10,
                                              int i11, int i12, int i13, int i14,
                                              int i15, int i16, int i17, int i18,
                                              int i19, int i20, int i21, int i22,
                                              int i23, int i24, int i25, int i26,
                                              int i27, int i28, int i29, int i30,
                                              int i31, int i32,
                                              IntWrapper o33,
                                              int i34, int i35, int i36)
    {
        int result_int = 0;
        IntWrapper result_obj = new IntWrapper();
        try  // To disable inlining
        {
            result_int = o33.value;
        }
        finally
        {
            result_obj.value = result_int;
        }
        return result_obj;
    }

    static IntWrapper Add35(int ecx, int edx, IntWrapper o3,
                                                       int i4, int i5, int i6,
                                              int i7, int i8, int i9, int i10,
                                              int i11, int i12, int i13, int i14,
                                              int i15, int i16, int i17, int i18,
                                              int i19, int i20, int i21, int i22,
                                              int i23, int i24, int i25, int i26,
                                              int i27, int i28, int i29, int i30,
                                              int i31, int i32, int i33, int i34,
                                              int i35)
    {
        int result_int = 0;
        IntWrapper result_obj = new IntWrapper();
        try  // To disable inlining
        {
            result_int = o3.value;
        }
        finally
        {
            result_obj.value = result_int;
        }
        return result_obj;
    }

    static int ident(int i)
    {
        int result = 0;
        try  // To disable inlining
        {
            GC.Collect();
            if (i == 0)
                throw new Exception();
        }
        finally
        {
            result = i;
        }
        return i;
    }

    static IntWrapper GetObj(int i)
    {
        int result = 0;
        try  // To disable inlining
        {
            if (i == 0)
                throw new Exception();
        }
        finally
        {
            result = i;
        }
        IntWrapper res = new IntWrapper();
        res.value = i;
        return res;
    }

    static bool Bug(int which)
    {

        IntWrapper enreg1 = new IntWrapper();
        IntWrapper enreg2 = new IntWrapper();

        enreg1.value = 0;
        enreg2.value = 0;
        bool passgcHole = false;
        bool passAssert = false;

        if ((which == 1) || (which == 0))
        {
            IntWrapper gcHoleFailure = Add36(1, 2,
                                              3, 4, 5, 6,
                                              7, 8, 9, 10,
                                              11, 12, 13, 14,
                                              15, 16, 17, 18,
                                              19, 20, 21, 22,
                                              23, 24, 25, 26,
                                              27, 28, 29, 30,
                                              31, 32,
                                              GetObj(ident(33)),
                                              ident(ident(34)),
                                              ident(ident(35)),
                                              ident(ident(36)));
            Console.WriteLine(gcHoleFailure.value);
            if (gcHoleFailure.value == 33) passgcHole = true;
        }


        if ((which == 2) || (which == 0))
        {
            IntWrapper assertFailure = Add35(1, 2,
                                              GetObj(3),
                                              4, 5, 6,
                                              7, 8, 9, 10,
                                              11, 12, 13, 14,
                                              15, 16, 17, 18,
                                              19, 20, 21, 22,
                                              23, 24, 25, 26,
                                              27, 28, 29, 30,
                                              31, 32,
                                              ident(33),
                                              ident(34),
                                              ident(30) + ident(5));
            Console.WriteLine(assertFailure.value);
            if (assertFailure.value == 3) passAssert = true;
        }

        for (int i = 0; i < 100; i++)
        {
            enreg1.value += i;
            enreg2.value += i;
        }

        if (passgcHole && passAssert)
        {
            return true;
        }
        else
        {
            return false;
        }

    }

    [Fact]
    public static int TestEntryPoint() => Run(0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Run(int val)
    {
        try
        {
            bool bugResult = Bug(val);
            if (bugResult) return 100;
            else return 101;

        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}
