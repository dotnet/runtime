// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.InteropServices;
using Xunit;

// Represents a problem with contained nodes chains, that contain lclVar reads, that were moved through lclVar stores.
// Notice that the project file sets DOTNET_JitStressModeNames.

[StructLayout(LayoutKind.Explicit)]
internal class AA
{

    [FieldOffset(8)]
    public QQ q;


    public static AA[] a_init = new AA[101];

    public static AA[] a_zero = new AA[101];


    public AA(int qq)
    {

        this.q = new QQ(qq);
    }


    public static void reset()
    {
        AA.a_init[100] = new AA(1);
        AA.a_zero[100] = new AA(2);
    }
}

internal class QQ
{
    public int val;

    public QQ(int vv)
    {
        this.val = vv;
    }

    public int ret_code()
    {
        return 100;
    }
}

public class TestApp
{

    private static int test_2_2(int num)
    {
        int result;
        if (AA.a_init[num].q != AA.a_zero[num].q) 
        // Access field with contained IND instruction.
        // EQ marks its operands as contained too. 
        // AA.a_init[num].q and AA.a_zero[num].q are allocated to the same lclVar.
        // So we calculate AA.a_init[num].q and store as tmp0, use this temp to do nullCheck.
        // Then store AA.a_zero[num].q as tmp0, destroy the old value and try to do EQ thinking that 
        // tmp0 is AA.a_init[num].q.
        // It needs stress (DOTNET_JitStressModeNames=STRESS_NULL_OBJECT_CHECK, STRESS_MAKE_CSE)
        // to force the compiler to do implicit null checks and store values as local variables.
// Bad IL example, t53 is set as contained, t143 is set as contained, it means they will be calculated as part of their parent t9.
// But at that moment V02, that is read in t143 is already modified by [000056].
// N035 (  1,  1) [000035] ------------       t35 =    LCL_VAR   ref    V02 tmp0         u:3 eax (last use) REG eax <l:$149, c:$182>
//                                                 /--*  t35    ref
// N037 (???,???) [000143] -c----------      t143 = *  LEA(b+12) byref  REG NA
//                                                  /--*  t143   byref
// N039 (  4,  4) [000054] Rc---O------       t54 = *  IND       ref    REG NA <l:$155, c:$184> // This contained flag is invalid because 
//                                                                                              // the value will be read after the store 000056.
// *********************************************************************************************
//                                                  /--*  t117   ref
// N073 ( 18, 22) [000056] DA-XG-------             *  STORE_LCL_VAR ref    V02 tmp0         d:4 eax REG eax // the store that corrupts t54 value.
// N075 (  1,  1) [000057] ------------       t57 =    LCL_VAR   ref    V02 tmp0         u:4 eax REG eax <l:$160, c:$187>
//                                                  /--*  t57    ref
// N077 (  2,  2) [000058] ---X---N----             *  NULLCHECK byte   REG NA <l:$166, c:$165>
// N079 (  1,  1) [000060] ------------       t60 =    LCL_VAR   ref    V02 tmp0         u:4 eax REG eax <l:$160, c:$187>
//                                                  /--*  t60    ref
// N081 (???,???) [000146] -c----------      t146 = *  LEA(b+12) byref  REG NA
//                                                  /--*  t146   byref
// N083 (  4,  4) [000079] R----O------       t79 = *  IND       ref    REG ecx <l:$16b, c:$189>
//                                                  /--*  t79    ref
// N085 (  4,  4) [000121] DA---O------             *  STORE_LCL_VAR ref    V07 cse1          ecx REG ecx
// N087 (  1,  1) [000122] ------------      t122 =    LCL_VAR   ref    V07 cse1          ecx REG ecx <l:$16b, c:$189>
//                                                  /--*  t54    ref // reads V02.
//                                                  +--*  t122   ref
// N089 ( 48, 56) [000009] J--XGO-N----        t9 = *  EQ        int    REG NA <l:$1c5, c:$1c4>
        {
            result = 100;
        }
        else
        {
            result = AA.a_zero[num].q.val;
        }
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        AA.reset();
        int result;

        int r = TestApp.test_2_2(100);
        if (r != 100)
        {
            Console.WriteLine("Failed.");
            result = 101;
        }
        else
        {
            Console.WriteLine("Passed.");
            result = 100;
        }
        return result;
    }
}
