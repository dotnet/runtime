// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { auto }

.assembly DevDiv_544985 { }
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit DevDiv_544985
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32
          Test(int32 argI0, int32 argI1, int32 argI2, int16 argS3, int16 argS4, int16 argS5) cil managed noinlining
  {
    .locals init ([0] int64 locL0,   int32 locI1,  int8 locB2, int64 locL3, uint8 locUB4, uint16 locUS5,
                      uint16 locUS6, uint8 locUB7, int16 locS8)
        ldc.i8       1
        stloc.0
        ldc.i4       1
        stloc.1
        ldc.i4       1
        stloc.2
        ldc.i8       1
        stloc.3
        ldc.i4       1
        stloc        4
        ldc.i4       1
        stloc        5
        ldc.i4       1
        stloc        6
        ldc.i4       1
        stloc        7
        ldc.i4       1
        stloc        8

        ldc.i8       1
        ldarg.s      0x5
        stloc        0x4
        pop         
        ldc.r8       2.7998176471286444e+172
        ldarg.s      0x5
        stloc.s      0x2
        ldarg.s      0x3
        stloc        0x8
        ldc.i8       0x330BB4E2
        ldloc        0x0
        ldc.i8       0x9C6A2B5
        and         
        not         
        ceq         
        neg         
        ldloc.s      0x4
        ldloc        0x3
        conv.i1     
        or          
        dup         
        neg         
        sub
        nop         
        ldc.i8       0x27AF808A
        pop         
        and         
        stloc.s      0x6
        ldloc.s      0x0
        ldloc        0x7
        shr         
        conv.i8     
        conv.r4     
        ldloc        0x5
        conv.u1     
        starg        0x3
        neg         
        ldc.r8       3.7091607632075579e+195
        conv.i8     
        neg         
        neg         
        ldloc        0x7
        ldloc.s      0x2
        cgt.un      
        ldloc        0x0
        ldc.i8       0xBCF8E9DB
        conv.i8     
        rem.un      
        conv.ovf.i4 
        div         
        shr         
        not         
        conv.r8     
        div         
        neg         
        ldc.r8       -1.8469687730371883e+160
        ckfinite    
        ldc.r8       6.7655721550212406e+3 // 6.7655721550212406e+303
        conv.r4     
        ldarg.s      0x4
        conv.r4     
        rem         
        neg         
        conv.r4     
        rem         
        conv.ovf.i8 
        ldc.r8       -2.8239060779835572e+06 // -2.8239060779835572e+056
        ckfinite    
        conv.i8 
        add         
        ldloc.s      0x0
        ldloc        0x0
        add
        pop         
        pop         
        ldc.i8       0x2AEA9007
        ldloc.s      0x3
        conv.r.un   
        conv.i8
        div.un      
        neg         
        ldloc        0x4
        conv.i8 
        ldarg.s      0x4
        shl         
        clt.un      
        conv.r8     
        conv.r4     
        nop         
        mul         
        neg         
        clt         
        ldarg        0x4
        conv.r4     
        ckfinite    
        ckfinite    
        pop         
        ret         

  } // end of method DevDiv_544985:Test

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init ([0] int32 retVal, [1] int32 testResult)

         ldc.i4     100
         stloc.0
         ldc.i4     1
         ldc.i4     2
         ldc.i4     3
         ldc.i4     4
         ldc.i4     5
         ldc.i4     6
         call       int32 DevDiv_544985::Test(int32 argI0, int32 argI1, int32 argI2, int16 argS3, int16 argS4, int16 argS5)
         stloc.1
         ldloc.1
         ldc.i4.0
         ceq
         brtrue.s  L1

         ldstr      "Test Result = "
         call       void [System.Console]System.Console::Write(string)
         ldloc.1
         call       void [System.Console]System.Console::WriteLine(int32)
         ldstr      "FAIL"
         call       void [System.Console]System.Console::WriteLine(string)
         ldc.i4.m1
         stloc.0
         br        L2

    L1:  ldstr      "PASS"
         call       void [System.Console]System.Console::WriteLine(string)

    L2:  ldloc.0
         ret
  } // end of method DevDiv_544985:Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
         ldarg.0
         call       instance void [System.Runtime]System.Object::.ctor()
         ret
  } // end of method DevDiv_544985:.ctor

} // end of class DevDiv_544985

