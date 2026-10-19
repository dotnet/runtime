// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test is a reduced repro case for DevDiv VSO bug 278372.
// The failure mode is that the RyuJIT/x86 backend was not correctly
// handling the case of a variable involving a variable V such that:
//  - V is lvMustInit (therefore it must be undefined on some path)
//  - V lives in multiple registers, but never on the stack
//  - there is at least one other variable that is also lvMustInit, but that
//    has a stack location.
//
// In this case, genFnProlog was attempting to zero-init V on the stack.
//
// It was difficult to construct a repro; this repro requires that the test
// be run with DOTNET_JitStressRegs=0x200 (which changes the location of
// variables at block boundaries).


// Metadata version: v4.0.30319
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:0:0:0
}
.assembly DevDiv_278372
{
}
.assembly extern xunit.core {}

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit DevDiv_278372
       extends [System.Runtime]System.Object
{
  .method public hidebysig static bool  check(int32& dummy) cil managed noinlining
  {
            ldc.i4.1
            ret
  } // end of method DevDiv_278372::check

  .method public hidebysig static int32  getX() cil managed noinlining
  {
            ldc.i4.s   25
            ret
  } // end of method DevDiv_278372::getX

  .method public hidebysig static int32  getY() cil managed noinlining
  {
            ldc.i4.5
            ret
  } // end of method DevDiv_278372::getY

  .method public hidebysig static int32  Test(int32 x,
                                              int32 y,
                                              int32 x2,
                                              int32 y2,
                                              int32 x3,
                                              int32 y3) cil managed noinlining
  {
    .maxstack  2
    .locals init ([0] int32 z,
             [1] int32 returnVal,
             [2] int32 dummy,
             [3] int32 z2)

            // Initialize returnVal to 100
            ldc.i4.s   100
            stloc.1

            // Here we pass the address of "dummy" to ensure that we have a must-init on-stack variable.
            ldloca.s   dummy
            call       bool DevDiv_278372::check(int32&)
            brfalse.s  L1

            // Here we are conditionally defining "z", so that it will be must-init
            ldarg.0
            ldarg.1
            rem
            stloc.0
    L1:     ldloc.0
            brfalse.s  L2

            ldc.i4.m1
            stloc.1
    L2:     ldarg.2
            ldarg.3
            rem
            stloc.3
            ldarg.0
            ldarg.1
            add
            stloc.0
            ldloc.0
            ldc.i4.s   30
            beq.s      L3

            ldc.i4.m1
            stloc.1
    L3:     ldloc.3
            brfalse.s  L4

            ldc.i4.m1
            stloc.1
    L4:     ldloc.1
            ldc.i4.s   100
            bne.un.s   L5

            ldstr      "Pass"
            call       void [System.Console]System.Console::WriteLine(string)
            br.s       L6

    L5:     ldstr      "Fail"
            call       void [System.Console]System.Console::WriteLine(string)
    L6:     ldloc.1
            ret
  } // end of method DevDiv_278372::Test

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       16 (0x10)
    .maxstack  8
            ldc.i4.s   25
            ldc.i4.5
            ldc.i4.s   25
            ldc.i4.5
            ldc.i4.s   25
            ldc.i4.5
            call       int32 DevDiv_278372::Test(int32,
                                                int32,
                                                int32,
                                                int32,
                                                int32,
                                                int32)
            ret
  } // end of method DevDiv_278372::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
            ldarg.0
            call       instance void [System.Runtime]System.Object::.ctor()
            ret
  } // end of method DevDiv_278372::.ctor

} // end of class DevDiv_278372


// =============================================================
