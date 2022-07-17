// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test for a bug involving an initblk with a non-constant size,
// for which the DYN_BLK(ADDR(lcl)) can't be optimized away.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly dynblk {}
.assembly extern xunit.core {}

.namespace Test_dynblk_il
{
.class public sequential ansi sealed beforefieldinit S8
       extends [mscorlib]System.ValueType
{
  .field public uint64 val
} // end of class Test_dynblk_il.S8

.class public auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  GetSize3() cil managed noinlining
  {
          ldc.i4     3
          ret
  } // end of method Test_dynblk_il.Program::Init

  .method public hidebysig static int32  GetSize5() cil managed noinlining
  {
          ldc.i4     5
          ret
  } // end of method Test_dynblk_il.Program::Init

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (valuetype Test_dynblk_il.S8 S, int32 retVal, int32 size)
              ldloca.s   S
              ldc.i4     0
              ldc.i4     8
              initblk

              call       int32 Test_dynblk_il.Program::GetSize5()
              stloc.s    size

              ldloca.s   S
              ldc.i4     0x55
              ldloc.s    size
              initblk

              call       int32 Test_dynblk_il.Program::GetSize3()
              stloc.s    size

              ldloca.s   S
              ldc.i4     0x33
              ldloc.s    size
              initblk

              ldloca.s   S
              ldfld      uint64 Test_dynblk_il.S8::val
              ldc.i8     0x5555333333
              ceq
              brtrue.s  L1

              ldstr      "Fail"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.m1
              stloc.s    retVal
              br.s       L2

    L1:       ldstr      "Pass"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   100
              stloc.s    retVal

    L2:       ldloc.s    retVal
              ret
  } // end of method Test_dynblk_il.Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
              ldarg.0
              call       instance void [mscorlib]System.Object::.ctor()
              nop
              ret
  } // end of method Test_dynblk_il.Program::.ctor

} // end of class Test_dynblk_il.Program

}
