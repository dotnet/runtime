// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// GitHub 25027: Tail call stress shouldn't cause asserts 
// in the presence of invalid IL

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern mscorlib { auto }
.assembly GitHub_25027 {}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit X
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init ([0] int32 result)
    ldc.i4.m1
    stloc.0

    .try
    {
      call       int32 X::F()
      stloc.0
      leave.s    join

    }
    catch [System.Runtime]System.InvalidProgramException 
    {
      pop
      ldc.i4.s   100
      stloc.0
      leave.s    join
    }

    join:

    ldloc.0
    ret
  }

  // F is intentionally made big enough so that it doesn't get inlined into
  // Main without needing to be marked "noinlining"

  .method private hidebysig static int32 
          F() cil managed
  {
    .locals init ([0] int32 r,
             [1] int32 i)
    ldc.i4.0
    stloc.0
    ldc.i4.0
    stloc.1
    br.s test

    loop:  

    ldloc.0
    ldloc.1
    ldloc.1
    mul
    add
    stloc.0
    ldloc.1
    ldc.i4.1
    add
    stloc.1

    test:

    ldloc.1
    ldc.i4.s   10
    ble.s      loop

    ldloc.0

    // invalid IL and invalid tail call site

    dup
    dup
    call       void X::G(int32)
    ret
  }

  .method private hidebysig static void
          G(int32 x) cil managed
  {
    ret
  }

}
