// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


// The test exposes an assertion in gtNewTempAssign on Arm64 when a method
//   1. returns a struct with one field of TYP_SIMD16 (e.g. Vector128<byte>) and
//   2. requires to have a single return basic block;
//      this condition becomes true when the method is
//        2.1. synchronized or
//        2.2. needs profiling ELT hooks or
//        2.3. has more than 4 return-points.
//
// The issue is due to:
//   1. importer transforming the return values of each GT_RETURN node
//        LCL_VAR   struct V00 loc0
//      into
//        LCL_FLD   simd16 V00 loc0         [+0]
//   2. morph merging the return values of "original" return basic blocks into
//      a temp that holds the return value in the new "designated" return basic block.
//      During the merge gtNewTempAssign does not expect the source value of TYP_SIMD16
//      and the destination value of TYP_STRUCT.

.assembly extern System.Console
{
}

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly extern System.Runtime.Intrinsics
{
}

.assembly GitHub_26491
{
}
.assembly extern xunit.core {}

.class public auto ansi beforefieldinit GitHub_26491 extends [System.Runtime]System.Object
{
  .class sequential ansi sealed nested private beforefieldinit ReturnType
         extends [System.Runtime]System.ValueType
  {
    .field private valuetype [System.Runtime.Intrinsics]System.Runtime.Intrinsics.Vector128`1<uint8> _simd16
  }

  .method private hidebysig static valuetype GitHub_26491/ReturnType MultipleReturns(int32 i) cil managed noinlining
  {
    // Code size       74 (0x4a)
    .maxstack  1
    .locals init (valuetype GitHub_26491/ReturnType V_0)
    IL_0000:  ldarg.0
    IL_0001:  switch     (IL_0018, IL_0022, IL_002c, IL_0036)
    IL_0016:  br.s       IL_0040

    IL_0018:  ldloca.s   V_0
    IL_001a:  initobj    GitHub_26491/ReturnType
    IL_0020:  ldloc.0
    IL_0021:  ret

    IL_0022:  ldloca.s   V_0
    IL_0024:  initobj    GitHub_26491/ReturnType
    IL_002a:  ldloc.0
    IL_002b:  ret

    IL_002c:  ldloca.s   V_0
    IL_002e:  initobj    GitHub_26491/ReturnType
    IL_0034:  ldloc.0
    IL_0035:  ret

    IL_0036:  ldloca.s   V_0
    IL_0038:  initobj    GitHub_26491/ReturnType
    IL_003e:  ldloc.0
    IL_003f:  ret

    IL_0040:  ldloca.s   V_0
    IL_0042:  initobj    GitHub_26491/ReturnType
    IL_0048:  ldloc.0
    IL_0049:  ret
  }

  .method private hidebysig static valuetype GitHub_26491/ReturnType SingleReturnSynchronized() cil managed synchronized noinlining
  {
    .maxstack  1
    .locals init (valuetype GitHub_26491/ReturnType V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    GitHub_26491/ReturnType
    IL_0008:  ldloc.0
    IL_0009:  ret
  }

  .method private hidebysig static valuetype GitHub_26491/ReturnType SingleReturn() cil managed noinlining
  {
    .maxstack  1
    .locals init (valuetype GitHub_26491/ReturnType V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    GitHub_26491/ReturnType
    IL_0008:  ldloc.0
    IL_0009:  ret
  }

  .method public hidebysig static int32 MultipleReturns() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    IL_000e:  ldc.i4     0x677b
    IL_0013:  call       valuetype GitHub_26491/ReturnType GitHub_26491::MultipleReturns(int32)
    IL_0018:  pop
    IL_0037:  ldc.i4.s   100
    IL_0039:  ret
  }

  .method public hidebysig static int32 SingleReturnSynchronized() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    IL_0029:  call       valuetype GitHub_26491/ReturnType GitHub_26491::SingleReturnSynchronized()
    IL_002e:  pop
    IL_0037:  ldc.i4.s   100
    IL_0039:  ret
  }

  .method public hidebysig static int32 SingleReturn() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    IL_0031:  call       valuetype GitHub_26491/ReturnType GitHub_26491::SingleReturn()
    IL_0036:  pop
    IL_0037:  ldc.i4.s   100
    IL_0039:  ret
  }

  .method public hidebysig static int32 Main() cil managed
  {
    .entrypoint
    .maxstack  8

    ldstr "MultipleReturns"
    call void [System.Console]System.Console::WriteLine(string)
    call int32 GitHub_26491::MultipleReturns()
    ldc.i4 100
    bne.un FAIL

    ldstr "SingleReturnSynchronized"
    call void [System.Console]System.Console::WriteLine(string)
    call int32 GitHub_26491::SingleReturnSynchronized()
    ldc.i4 100
    bne.un FAIL

    ldstr "SingleReturn"
    call void [System.Console]System.Console::WriteLine(string)
    call int32 GitHub_26491::SingleReturn()
    ldc.i4 100
    bne.un FAIL

    ldc.i4 100
    ret

  FAIL:
    ldc.i4 101
    ret
  }
}
