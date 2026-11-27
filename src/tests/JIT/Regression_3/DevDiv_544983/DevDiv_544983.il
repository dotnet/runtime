// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The bug that this test captures was a case where odd float register became free,
// the corresponding even register was already free, and there was a double interval
// waiting for the even/odd pair, which wasn't added yet because the odd half was still occupied.

.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { auto }

.assembly DevDiv_544983 { }
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit DevDiv_544983
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 
          Test(int32 i) cil managed noinlining
  {
    .locals init ([0] int32 dummy)
        ldloc.0
        not         
        ldc.r8       -4.5508785095998289e-253
        conv.r8     
        conv.ovf.u4.un
        conv.ovf.u8.un
        conv.r.un   
        ldarg        0x0
        conv.r.un   
        dup         
        cgt         
        conv.r.un   
        div         
        conv.u1     
        conv.ovf.i8 
        pop  
        ret
  } // end of method DevDiv_544983:Test

  .method public hidebysig static int16
          Test2(uint16 a, float32 b, int32 c, uint16 d, uint8 e, int16 f, int32 g, int64 h)
  {
    .locals init ([0] int64 i)

        ldc.i8       2
        stloc.0
        ldarg.s      0x2
        ldarg.s      0x5
        div         
        conv.ovf.u2.un
        conv.ovf.u2.un
        ldarg        0x1
        conv.ovf.i4 
        conv.ovf.i8.un
        ldc.i4       0x669FC58A
        neg         
        shl         
        conv.r.un   
        ldloc.s      0x0
        conv.ovf.i8.un
        conv.r.un   
        ldloc        0x0
        conv.r.un   
        div         
        ceq         
        cgt.un      
        conv.r8     
        conv.r.un
        pop         
        ldloc        0x0
        neg         
        not         
        conv.i2
        ret 
   }

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init ([0] int32 retVal, [1] int32 testResult)

         ldc.i4.s   100
         stloc.0
         ldc.i4.1
         call       int32 DevDiv_544983::Test(int32)
         stloc.1
         ldloc.1
         ldc.i4.m1
         ceq
         brtrue.s  L2

         ldstr      "Test Result = "
         call       void [System.Console]System.Console::Write(string)
         ldloc.1
         call       void [System.Console]System.Console::WriteLine(int32)
         ldstr      "FAIL"
         call       void [System.Console]System.Console::WriteLine(string)
         ldc.i4.m1
         stloc.0

    L2:  ldc.i4.1
         ldc.r4     2.0
         ldc.i4.3
         ldc.i4.4
         ldc.i4     5
         ldc.i4.6
         ldc.i4.7
         ldc.i8     8
         call       int16 DevDiv_544983::Test2(uint16, float32, int32, uint16, uint8, int16, int32, int64)
         stloc.1
         ldloc.1
         ldc.i4.1
         ceq
         brtrue.s  L3

         ldstr      "Test2 Result = "
         call       void [System.Console]System.Console::Write(string)
         ldloc.1
         call       void [System.Console]System.Console::WriteLine(int32)
         ldstr      "FAIL"
         call       void [System.Console]System.Console::WriteLine(string)
         ldc.i4.m1
         stloc.0
         br         L4

    L3:  ldloc.0
         ldc.i4.s   100
         ceq
         brfalse.s  L4

         ldstr      "PASS"
         call       void [System.Console]System.Console::WriteLine(string)

    L4:  ldloc.0
         ret
  } // end of method DevDiv_544983:Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
         ldarg.0
         call       instance void [System.Runtime]System.Object::.ctor()
         ret
  } // end of method DevDiv_544983:.ctor

} // end of class DevDiv_544983

