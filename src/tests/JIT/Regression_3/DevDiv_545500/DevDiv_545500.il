// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The bug that this test captures was a case where a call to a divide helper
// was being removed as dead code, but the GT_FIELD_LIST node for the int64
// argument was left. Such nodes must always be contained, and if they become
// dead, all of their children must be marked as unused.

.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { auto }

.assembly DevDiv_545500 { }
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit DevDiv_545500
       extends [System.Runtime]System.Object
{
  .method public hidebysig static uint8 
          Test(float32 f, int32 i, int64 l) cil managed noinlining
  {
    .locals init ([0] float64 d, bool b, int64 l2)
        ldc.r8       100.0
        conv.r4     
        conv.ovf.u8.un
        ldc.i8       0x70B9
        not         
        not         
        neg         
        add.ovf     
        not         
        ldarg        0x1
        ldarg        0x1
        div         
        ldarg        0x1
        ldarg        0x1
        and         
        and         
        shr.un      
        conv.u8     
        conv.r8     
        nop         
        conv.ovf.i8.un
        ldc.i8       0xB03F
        ldloc.s      0x0
        ckfinite    
        conv.ovf.u8 
        ldc.i4       0x9260
        conv.i8     
        or          
        conv.ovf.u8 
        clt.un      
        neg         
        conv.i8     
        conv.ovf.u4 
        shr         
        conv.ovf.u.un
        brtrue       L1
        ldloc.s      0x1
        not         
        conv.r4     
        ldloc        0x0
        add         
        ldarg        0x0
        ldloc.s      0x0
        mul         
        conv.r4     
        conv.r8     
        add         
        ldarg        0x0
        ldloc        0x0
        cgt.un      
        neg         
        conv.r8     
        neg         
        conv.r4     
        ldloc.s      0x2
        neg         
        conv.ovf.i2.un
        ldc.i4       0xAAC7
        conv.r8     
        conv.ovf.i1 
        xor         
        conv.r.un   
        conv.r8     
        ldc.r8       -2.9830006130986561e+135
        starg        0x0
        neg         
        mul         
        stloc        0x0
        conv.ovf.i8 
        nop         
        starg.s      0x2
L1:     ldarg.s      0x1
        neg         
        ldloc.s      0x2
        conv.r.un   
        ldarg.s      0x0
        ldarg.s      0x0
        cgt         
        conv.r.un   
        cgt         
        neg         
        add
        conv.r.un   
        ckfinite    
        conv.u8     
        conv.i  
        conv.i2     
        ldarg        0x1
        neg         
        ldloc.s      0x1
        ldloc.s      0x0
        conv.ovf.i4.un
        shl         
        add         
        conv.u8     
        ldarg        0x2
        ldc.i8       0x5ACB
        ldarg.s      0x2
        sub    
        add
        ldc.i8       0x9B4E
        div.un      
        conv.u8
        cgt         
        shr.un      
        neg         
        ret 
  } // end of method DevDiv_545500:Test

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init ([0] int32 retVal, [1] int32 testResult)

         ldc.i4     100
         stloc.0
         ldc.r4     1.0
         ldc.i4     1
         ldc.i8     3
         call       uint8 DevDiv_545500::Test(float32 f, int32 i, int64 l)
         stloc.1
         ldloc.1
         ldc.i4.1
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
  } // end of method DevDiv_545500:Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
         ldarg.0
         call       instance void [System.Runtime]System.Object::.ctor()
         ret
  } // end of method DevDiv_545500:.ctor

} // end of class DevDiv_545500

