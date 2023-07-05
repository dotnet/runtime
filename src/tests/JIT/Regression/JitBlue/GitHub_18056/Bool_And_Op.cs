// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// The test originally exposed the issue with predecessors lists:
// 1. fgReplacePred did not keep the sorted order (GitHub_13295);
// 2. fgAddRefPred did not find the existing occurrence if the order was not sorted;
// 3. fgReplacePred had several occurrences of the same block and when it became dead it updated only the first link;
// 4. The link to the dead block exposed noway_assert in flowgraph.
// The repro required JitStress=2 or DOTNET_jitstressmodenames=STRESS_BB_PROFILE;
// STRESS_BB_PROFILE uses file name hash to set weights, so this file can't be renamed.

#pragma warning disable

using System;
using Xunit;
public class testout1
{
    static bool static_field_bool;
    static bool sfb_false;
    static bool sfb_true;
    bool mfb;
    bool mfb_false;
    bool mfb_true;
    static bool simple_func_bool()
    {
        return true;
    }
    static bool func_sb_true()
    {
        return true;
    }
    static bool func_sb_false()
    {
        return false;
    }

    static int Sub_Funclet_411()
    {
        int True_Sum = 0;
        int False_Sum = 0;
        int index = 1;
        bool local_bool = true;
        testout1 t1_i = new testout1();
        bool[] ab_false = new bool[3];
        bool[] ab_true = new bool[3];
        ab_true[0] = true;
        ab_true[1] = true;
        ab_true[2] = true;

        static_field_bool = true;
        sfb_false = false;
        sfb_true = true;

        t1_i.mfb = true;
        t1_i.mfb_false = false;
        t1_i.mfb_true = true;
        if (ab_true[index] && func_sb_true() ? static_field_bool : t1_i.mfb) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? static_field_bool : simple_func_bool()) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? static_field_bool : ab_true[index]) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? static_field_bool : ab_false[index]) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? t1_i.mfb : true) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? t1_i.mfb : false) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? t1_i.mfb : local_bool) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? t1_i.mfb : static_field_bool) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? t1_i.mfb : t1_i.mfb) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? t1_i.mfb : simple_func_bool()) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? t1_i.mfb : ab_true[index]) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? t1_i.mfb : ab_false[index]) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? simple_func_bool() : true) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? simple_func_bool() : false) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? simple_func_bool() : local_bool) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? simple_func_bool() : static_field_bool) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? simple_func_bool() : t1_i.mfb) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? simple_func_bool() : simple_func_bool()) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? simple_func_bool() : ab_true[index]) True_Sum++; else False_Sum++;
        if (ab_true[index] && func_sb_true() ? simple_func_bool() : ab_false[index]) True_Sum++; else False_Sum++;
        return (True_Sum * 2) - False_Sum;
    }
  
    [Fact]
    public static int TestEntryPoint()
    {
        int Sum = 0;
       
        Sum += Sub_Funclet_411();
      
        if (Sum == 40)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return -1;
        }

    }
}
