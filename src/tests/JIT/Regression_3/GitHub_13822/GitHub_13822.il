// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern mscorlib { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly GitHub_13822
{
}
.assembly extern xunit.core {}


// =============== CLASS MEMBERS DECLARATION ===================

.class public sequential ansi sealed beforefieldinit GCStruct
       extends [System.Runtime]System.ValueType
{
  .field public string str
  .field public int32 i
  .field public int32 j
  .field public int32 k
  .field public int32 l

  .method public hidebysig specialname rtspecialname
          instance void  .ctor(int32 i) cil managed aggressiveinlining
  {
    .maxstack  8
              ldarg.0
              ldstr      "Hello"
              stfld      string GCStruct::str
              ldarg.1
              brtrue.s   RETURN
              ldarg.0
              ldc.i4.1
              stfld      int32 GCStruct::i
    RETURN:   ret
  } // end of method GCStruct::.ctor

} // end of class GCStruct

  // Method A has tail-recursive call that the jit turns into a loop.
  // This test verifies that the temp struct returned by newobj is zero-initialized
  // before calling the constructor on each iteration of the loop.

.class public auto ansi beforefieldinit Test_GitHub_13822
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
              ldc.i4.0
              call       int32 Test_GitHub_13822::A(int32)
              ret
  } // end of method Test::Main

  .method public hidebysig static int32  A(int32 i) cil managed
  {
    .maxstack  2
    .locals init (int32 V_0)
              call       int32 Test_GitHub_13822::GetZero()
              stloc.0
              ldarg.0
              ldc.i4.3
              blt.s      L1
              ldc.i4.s   100
              ret
    L1:       ldarg.0
              newobj     instance void GCStruct::.ctor(int32)
              ldfld      int32 GCStruct::i
              ldarg.0
              mul
              brfalse L2
              ldc.i4.m1
              ret
    L2:       ldarg.0
              ldc.i4.1
              add
              call       int32 Test_GitHub_13822::A(int32)
              ret
  } // end of method Test::A

  .method public hidebysig static int32  GetZero() cil managed noinlining
  {
    .maxstack  8
              ldc.i4.0
              ret
  } // end of method Test::GetZero

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
              ldarg.0
              call       instance void [System.Runtime]System.Object::.ctor()
              ret
  } // end of method Test::.ctor

} // end of class Test


// =============================================================
