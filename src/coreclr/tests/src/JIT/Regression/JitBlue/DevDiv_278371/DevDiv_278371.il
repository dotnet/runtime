// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Test for a bug involving an initblk with a non-constant size,
// which uses rep movs on x86 (x64 uses a helper call).

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly DevDiv_278371 {}

.class public sequential ansi sealed beforefieldinit S
       extends [mscorlib]System.ValueType
{
  .field public uint32 val
} // end of class S

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  Init(valuetype S& s,
                                             int32 size) cil managed noinlining
  {
          nop
          ldarg.0
          ldc.i4     0xaa
          ldarg.1
          initblk
          ret
  } // end of method Program::Init

  .method private hidebysig static int32 
          Main() cil managed
  {
    .entrypoint
    .locals init (valuetype S V_0,
             bool V_1,
             int32 V_2)
              ldloca.s   V_0
              initobj    S
              ldloca.s   V_0
              ldc.i4     4
              call       void Program::Init(valuetype S&, int32)
              ldloc.0
              ldfld      uint32 S::val
              ldc.i4     0xaaaaaaaa
              ceq
              ldc.i4.0
              ceq
              stloc.1
              ldloc.1
              brfalse.s  L1

              ldstr      "Fail"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.m1
              stloc.2
              br.s       L2

    L1:       ldstr      "Pass"
              call       void [mscorlib]System.Console::WriteLine(string)
              nop
              ldc.i4.s   100
              stloc.2

    L2:       ldloc.2
              ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
              ldarg.0
              call       instance void [mscorlib]System.Object::.ctor()
              nop
              ret
  } // end of method Program::.ctor

} // end of class Program

