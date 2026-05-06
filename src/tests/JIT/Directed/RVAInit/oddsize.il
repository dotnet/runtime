// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'oddsize'
{
}
// MVID: {3DA3EDA0-F1A4-42F9-8E0B-F269420E91A5}

.file alignment 512

// Image base: 0x034f0000
.class public sequential ansi sealed beforefieldinit Test_oddsize
       extends [mscorlib]System.ValueType
{
  .field public static valuetype Test_oddsize test1 at bytearray2
  .field public int32 m_v1
  .method private hidebysig static void Dump(valuetype Test_oddsize &arg) cil managed
  {
    .maxstack  8
    ldarg.0
    ldfld int32 Test_oddsize::m_v1
    ldc.i4 0x04030201
    sub
    call void [System.Console]System.Console::Write(int32)
    call void [System.Console]System.Console::WriteLine()
    ret
  }
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  1
    .locals (int32 V_0)
    ldsflda valuetype Test_oddsize Test_oddsize::test1
    call void Test_oddsize::Dump(valuetype Test_oddsize &arg)
    ldc.i4.s   100
    ret
  } // end of method Test::Main

} // end of class Test
.data bytearray1 = bytearray ( 0 0 0 )
.data bytearray2 = bytearray ( 1 2 3 4 )
