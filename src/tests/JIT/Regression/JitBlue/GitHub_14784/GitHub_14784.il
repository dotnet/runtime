// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test for potentially incorrect optimizization of
//
//    'dup stloc.x'
//
// into
//
//    'stloc.x ldloc.x ldloc.x'
//
// when the stloc truncates.

.assembly extern mscorlib {auto}
.assembly extern xunit.core {}
.assembly extern System.Console {auto}
.assembly GitHub_14784 {}

.class public auto ansi abstract sealed beforefieldinit Program
extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main () cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .maxstack 8
    .entrypoint

    call int32 Program::InlineAssignByte()
    ret
  } // end of method Main

  .method public static int32 InlineAssignByte()
  {
    .locals init (
      int8 local,
      object b
    )
    ldstr "InlineAssignByte: WriteLine(local = {0})"
    ldc.i4 300
    dup
    stloc.0
    box int32
    stloc.1
    ldloc.1
    call void [mscorlib]System.Console::WriteLine(string, object)
    ldstr "InlineAssignByte: local is {0}"
    ldloc.0
    box int32
    call void [mscorlib]System.Console::WriteLine(string, object)
    ldloc.1
    unbox.any int32
    ldc.i4 200
    sub
    ret
  }
}
