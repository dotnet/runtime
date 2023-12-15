// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

// Test case for fix 20838
// We are a missing check for ZeroOffsetFldSeq values on LclVar reads
// without the fix several cases in this test fail because value numbering
// gives us the old value instead of the modified value.
// 
// CoreRun.exe GitHub_18259.exe
// Failed - Test_e0_S2_F3_F1() - 640  -- 512       + 128
// Failed - Test_e0_S2_F4_F1() - 960  -- 512 + 256 + 128 + 64
// Failed - Test_e1_S2_F3_F1() - 640
// Failed - Test_e1_S2_F4_F1() - 960

struct S1
{
    public int F1;
    public int F2;

    public S1(int a1, int a2)  { F1 = a1; F2 = a2; }
}

struct S2
{
    public S1 F3;
    public S1 F4;

    public S2(S1 a1, S1 a2)    { F3 = a1;   F4 = a2;   }  
}

public class Program
{

    static S1   ss_S1a = new S1(101, 102); 
    static S1   ss_S1b = new S1(103, 104); 
    static S1   ss_S1c = new S1(105, 106); 
    static S1   ss_S1d = new S1(107, 108);
    static S2   ss_S2a = new S2(ss_S1a, ss_S1b);
    static S2   ss_S2b = new S2(ss_S1c, ss_S1d);
    static S2[] sa_S2  = new S2[] { ss_S2a, ss_S2b }; 

    static bool Test_e0_S2_F3_F1()
    {
        ref S2  ref_e0_S2       = ref sa_S2[0];
        ref S1  ref_e0_S2_F3    = ref sa_S2[0].F3;
        ref int ref_e0_S2_F3_F1 = ref sa_S2[0].F3.F1;
        
        int result = 0;
        
        if (sa_S2[0].F3.F1 != 101)
        {
            result |= 1;
        }
        
        if (ref_e0_S2_F3_F1 != 101)
        {
            result |= 2;
        }
        
        if (ref_e0_S2_F3.F1 != 101)
        {
            result |= 4;
        }
        
        if (ref_e0_S2.F3.F1 != 101)
        {
            result |= 8;
        }
        
        if (ref_e0_S2_F3.F1 != 101)
        {
            result |= 16;
        }
        
        ref_e0_S2_F3.F1 = 99;
            
        if (sa_S2[0].F3.F1 != 99)
        {
            result |= 32;
        }
        
        if (ref_e0_S2_F3_F1 != 99)
        {
            result |= 64;
        }
        
        if (ref_e0_S2_F3.F1 != 99)
        {
            result |= 128;
        }
        
        if (ref_e0_S2.F3.F1 != 99)
        {
            result |= 256;
        }
        
        if (ref_e0_S2_F3.F1 != 99)
        {
            result |= 512;
        }
        
        if (result != 0)
        {
            Console.WriteLine("Failed - Test_e0_S2_F3_F1() - " + result);
            return false;
        }

        return true;
    }

    static bool Test_e0_S2_F3_F2()
    {
        ref S2  ref_e0_S2       = ref sa_S2[0];
        ref S1  ref_e0_S2_F3    = ref sa_S2[0].F3;
        ref int ref_e0_S2_F3_F2 = ref sa_S2[0].F3.F2;
        
        int result = 0;
        
        if (sa_S2[0].F3.F2 != 102)
        {
            result |= 1;
        }
        
        if (ref_e0_S2_F3_F2 != 102)
        {
            result |= 2;
        }
        
        if (ref_e0_S2_F3.F2 != 102)
        {
            result |= 4;
        }
        
        if (ref_e0_S2.F3.F2 != 102)
        {
            result |= 8;
        }
        
        if (ref_e0_S2_F3.F2 != 102)
        {
            result |= 16;
        }
        
        ref_e0_S2_F3.F2 = 98;
            
        if (sa_S2[0].F3.F2 != 98)
        {
            result |= 32;
        }
        
        if (ref_e0_S2_F3_F2 != 98)
        {
            result |= 64;
        }
        
        if (ref_e0_S2_F3.F2 != 98)
        {
            result |= 128;
        }
        
        if (ref_e0_S2.F3.F2 != 98)
        {
            result |= 256;
        }
        
        if (ref_e0_S2_F3.F2 != 98)
        {
            result |= 512;
        }
        
        if (result != 0)
        {
            Console.WriteLine("Failed - Test_e0_S2_F3_F2() - " + result);
            return false;
        }

        return true;
    }

    static bool Test_e0_S2_F4_F1()
    {
        ref S2  ref_e0_S2       = ref sa_S2[0];
        ref S1  ref_e0_S2_F4    = ref sa_S2[0].F4;
        ref int ref_e0_S2_F4_F1 = ref sa_S2[0].F4.F1;
        
        int result = 0;
        
        if (sa_S2[0].F4.F1 != 103)
        {
            result |= 1;
        }
        
        if (ref_e0_S2_F4_F1 != 103)
        {
            result |= 2;
        }
        
        if (ref_e0_S2_F4.F1 != 103)
        {
            result |= 4;
        }
        
        if (ref_e0_S2.F4.F1 != 103)
        {
            result |= 8;
        }
        
        if (ref_e0_S2_F4.F1 != 103)
        {
            result |= 16;
        }
        
        ref_e0_S2_F4.F1 = 97;
            
        if (sa_S2[0].F4.F1 != 97)
        {
            result |= 32;
        }
        
        if (ref_e0_S2_F4_F1 != 97)
        {
            result |= 64;
        }
        
        if (ref_e0_S2_F4.F1 != 97)
        {
            result |= 128;
        }
        
        if (ref_e0_S2.F4.F1 != 97)
        {
            result |= 256;
        }
        
        if (ref_e0_S2_F4.F1 != 97)
        {
            result |= 512;
        }
        
        if (result != 0)
        {
            Console.WriteLine("Failed - Test_e0_S2_F4_F1() - " + result);
            return false;
        }

        return true;
    }

    static bool Test_e0_S2_F4_F2()
    {
        ref S2  ref_e0_S2       = ref sa_S2[0];
        ref S1  ref_e0_S2_F4    = ref sa_S2[0].F4;
        ref int ref_e0_S2_F4_F2 = ref sa_S2[0].F4.F2;
        
        int result = 0;
        
        if (sa_S2[0].F4.F2 != 104)
        {
            result |= 1;
        }
        
        if (ref_e0_S2_F4_F2 != 104)
        {
            result |= 2;
        }
        
        if (ref_e0_S2_F4.F2 != 104)
        {
            result |= 4;
        }
        
        if (ref_e0_S2.F4.F2 != 104)
        {
            result |= 8;
        }
        
        if (ref_e0_S2_F4.F2 != 104)
        {
            result |= 16;
        }
        
        ref_e0_S2_F4.F2 = 96;
            
        if (sa_S2[0].F4.F2 != 96)
        {
            result |= 32;
        }
        
        if (ref_e0_S2_F4_F2 != 96)
        {
            result |= 64;
        }
        
        if (ref_e0_S2_F4.F2 != 96)
        {
            result |= 128;
        }
        
        if (ref_e0_S2.F4.F2 != 96)
        {
            result |= 256;
        }
        
        if (ref_e0_S2_F4.F2 != 96)
        {
            result |= 512;
        }
        
        if (result != 0)
        {
            Console.WriteLine("Failed - Test_e0_S2_F4_F2() - " + result);
            return false;
        }

        return true;
    }

    static bool Test_e1_S2_F3_F1()
    {
        ref S2  ref_e1_S2       = ref sa_S2[1];
        ref S1  ref_e1_S2_F3    = ref sa_S2[1].F3;
        ref int ref_e1_S2_F3_F1 = ref sa_S2[1].F3.F1;
        
        int result = 0;
        
        if (sa_S2[1].F3.F1 != 105)
        {
            result |= 1;
        }
        
        if (ref_e1_S2_F3_F1 != 105)
        {
            result |= 2;
        }
        
        if (ref_e1_S2_F3.F1 != 105)
        {
            result |= 4;
        }
        
        if (ref_e1_S2.F3.F1 != 105)
        {
            result |= 8;
        }
        
        if (ref_e1_S2_F3.F1 != 105)
        {
            result |= 16;
        }
        
        ref_e1_S2_F3.F1 = 95;
            
        if (sa_S2[1].F3.F1 != 95)
        {
            result |= 32;
        }
        
        if (ref_e1_S2_F3_F1 != 95)
        {
            result |= 64;
        }
        
        if (ref_e1_S2_F3.F1 != 95)
        {
            result |= 128;
        }
        
        if (ref_e1_S2.F3.F1 != 95)
        {
            result |= 256;
        }
        
        if (ref_e1_S2_F3.F1 != 95)
        {
            result |= 512;
        }
        
        if (result != 0)
        {
            Console.WriteLine("Failed - Test_e1_S2_F3_F1() - " + result);
            return false;
        }

        return true;
    }

    static bool Test_e1_S2_F3_F2()
    {
        ref S2  ref_e1_S2       = ref sa_S2[1];
        ref S1  ref_e1_S2_F3    = ref sa_S2[1].F3;
        ref int ref_e1_S2_F3_F2 = ref sa_S2[1].F3.F2;
        
        int result = 0;
        
        if (sa_S2[1].F3.F2 != 106)
        {
            result |= 1;
        }
        
        if (ref_e1_S2_F3_F2 != 106)
        {
            result |= 2;
        }
        
        if (ref_e1_S2_F3.F2 != 106)
        {
            result |= 4;
        }
        
        if (ref_e1_S2.F3.F2 != 106)
        {
            result |= 8;
        }
        
        if (ref_e1_S2_F3.F2 != 106)
        {
            result |= 16;
        }
        
        ref_e1_S2_F3.F2 = 94;
            
        if (sa_S2[1].F3.F2 != 94)
        {
            result |= 32;
        }
        
        if (ref_e1_S2_F3_F2 != 94)
        {
            result |= 64;
        }
        
        if (ref_e1_S2_F3.F2 != 94)
        {
            result |= 128;
        }
        
        if (ref_e1_S2.F3.F2 != 94)
        {
            result |= 256;
        }
        
        if (ref_e1_S2_F3.F2 != 94)
        {
            result |= 512;
        }
        
        if (result != 0)
        {
            Console.WriteLine("Failed - Test_e1_S2_F3_F2() - " + result);
            return false;
        }

        return true;
    }

    static bool Test_e1_S2_F4_F1()
    {
        ref S2  ref_e1_S2       = ref sa_S2[1];
        ref S1  ref_e1_S2_F4    = ref sa_S2[1].F4;
        ref int ref_e1_S2_F4_F1 = ref sa_S2[1].F4.F1;
        
        int result = 0;
        
        if (sa_S2[1].F4.F1 != 107)
        {
            result |= 1;
        }
        
        if (ref_e1_S2_F4_F1 != 107)
        {
            result |= 2;
        }
        
        if (ref_e1_S2_F4.F1 != 107)
        {
            result |= 4;
        }
        
        if (ref_e1_S2.F4.F1 != 107)
        {
            result |= 8;
        }
        
        if (ref_e1_S2_F4.F1 != 107)
        {
            result |= 16;
        }
        
        ref_e1_S2_F4.F1 = 93;
            
        if (sa_S2[1].F4.F1 != 93)
        {
            result |= 32;
        }
        
        if (ref_e1_S2_F4_F1 != 93)
        {
            result |= 64;
        }
        
        if (ref_e1_S2_F4.F1 != 93)
        {
            result |= 128;
        }
        
        if (ref_e1_S2.F4.F1 != 93)
        {
            result |= 256;
        }
        
        if (ref_e1_S2_F4.F1 != 93)
        {
            result |= 512;
        }
        
        if (result != 0)
        {
            Console.WriteLine("Failed - Test_e1_S2_F4_F1() - " + result);
            return false;
        }

        return true;
    }

    static bool Test_e1_S2_F4_F2()
    {
        ref S2  ref_e1_S2       = ref sa_S2[1];
        ref S1  ref_e1_S2_F4    = ref sa_S2[1].F4;
        ref int ref_e1_S2_F4_F2 = ref sa_S2[1].F4.F2;
        
        int result = 0;
        
        if (sa_S2[1].F4.F2 != 108)
        {
            result |= 1;
        }
        
        if (ref_e1_S2_F4_F2 != 108)
        {
            result |= 2;
        }
        
        if (ref_e1_S2_F4.F2 != 108)
        {
            result |= 4;
        }
        
        if (ref_e1_S2.F4.F2 != 108)
        {
            result |= 8;
        }
        
        if (ref_e1_S2_F4.F2 != 108)
        {
            result |= 16;
        }
        
        ref_e1_S2_F4.F2 = 92;
            
        if (sa_S2[1].F4.F2 != 92)
        {
            result |= 32;
        }
        
        if (ref_e1_S2_F4_F2 != 92)
        {
            result |= 64;
        }
        
        if (ref_e1_S2_F4.F2 != 92)
        {
            result |= 128;
        }
        
        if (ref_e1_S2.F4.F2 != 92)
        {
            result |= 256;
        }
        
        if (ref_e1_S2_F4.F2 != 92)
        {
            result |= 512;
        }
        
        if (result != 0)
        {
            Console.WriteLine("Failed - Test_e1_S2_F4_F2() - " + result);
            return false;
        }

        return true;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool isPassing = true;

        isPassing &= Test_e0_S2_F3_F1();

        isPassing &= Test_e0_S2_F3_F2();

        isPassing &= Test_e0_S2_F4_F1();

        isPassing &= Test_e0_S2_F4_F2();

        isPassing &= Test_e1_S2_F3_F1();

        isPassing &= Test_e1_S2_F3_F2();

        isPassing &= Test_e1_S2_F4_F1();

        isPassing &= Test_e1_S2_F4_F2();

        if (isPassing)
        {
            Console.WriteLine("Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Failed");
            return 101;
        }
    }
}
