// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Do an IL jmp instruction in a method with a big local frame,
// where SP restore needs a free register to free local frame (sp += frameSize).
// In the past codegen was always using R3 as a temp without checking that it could
// be occupied by an argument value.

// asm example for arm32 windows:
//
// movw    r10, 0x1040	// [V01 arg0] <- restore 8 byte V01 arg (r2, r2) from the stack.
// str     r2, [sp+r10]	// [V01 arg0]
// movw    r10, 0x1044	// [V01 arg0+0x04]
// str     r3, [sp+r10]	// [V01 arg0+0x04] 
// __epilog:
// movw    r3, frameSize
// add     sp, r3 <- corrupt r3 value to free local frame.
// pop     {r10,lr}
// bx      jumpAddress

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Diagnostics.Debug {}
.assembly GitHub_27678 {}
.assembly extern xunit.core {}

.class private sequential ansi sealed beforefieldinit AStruct
       extends [System.Runtime]System.ValueType
{
  .field public int64 a
  .field public int64 b
  .field public int64 c
  .field public int64 d
} // end of class AStruct

.class private sequential ansi sealed beforefieldinit BStruct
       extends [System.Runtime]System.ValueType
{
  .field public valuetype AStruct a
  .field public valuetype AStruct b
  .field public valuetype AStruct c
  .field public valuetype AStruct d
} // end of class BStruct

.class private sequential ansi sealed beforefieldinit CStruct
       extends [System.Runtime]System.ValueType
{
  .field public valuetype BStruct a
  .field public valuetype BStruct b
  .field public valuetype BStruct c
  .field public valuetype BStruct d
} // end of class CStruct

.class private sequential ansi sealed beforefieldinit DStruct
       extends [System.Runtime]System.ValueType
{
  .field public valuetype CStruct a
  .field public valuetype CStruct b
  .field public valuetype CStruct c
  .field public valuetype CStruct d
} // end of class DStruct

.class private sequential ansi sealed beforefieldinit EStruct
       extends [System.Runtime]System.ValueType
{
  .field public valuetype DStruct a
  .field public valuetype DStruct b
  .field public valuetype DStruct c
  .field public valuetype DStruct d
} // end of class EStruct

.class private sequential ansi sealed beforefieldinit FStruct
       extends [System.Runtime]System.ValueType
{
  .field public valuetype EStruct a
  .field public valuetype EStruct b
  .field public valuetype EStruct c
  .field public valuetype EStruct d
} // end of class FStruct

.class public auto ansi beforefieldinit GitHub_27678
       extends [System.Runtime]System.Object
{
  .method private hidebysig static bool  JumpTarget(int64 a,
                                                    int64 b,
                                                    valuetype FStruct c) cil managed noinlining
  {
    // Code size       108 (0x6c)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  conv.i8
    IL_0003:  ceq
    IL_0005:  call       void [System.Diagnostics.Debug]System.Diagnostics.Debug::Assert(bool)
    IL_000a:  ldarg.1
    IL_000b:  ldc.i4.2
    IL_000c:  conv.i8
    IL_000d:  ceq
    IL_000f:  call       void [System.Diagnostics.Debug]System.Diagnostics.Debug::Assert(bool)
    IL_0014:  ldarg.2
    IL_0015:  ldfld      valuetype EStruct FStruct::a
    IL_001a:  ldfld      valuetype DStruct EStruct::a
    IL_001f:  ldfld      valuetype CStruct DStruct::a
    IL_0024:  ldfld      valuetype BStruct CStruct::a
    IL_0029:  ldfld      valuetype AStruct BStruct::a
    IL_002e:  ldfld      int64 AStruct::a
    IL_0033:  ldc.i4.3
    IL_0034:  conv.i8
    IL_0035:  ceq
    IL_0037:  call       void [System.Diagnostics.Debug]System.Diagnostics.Debug::Assert(bool)
    IL_003c:  ldarg.0
    IL_003d:  ldc.i4.1
    IL_003e:  conv.i8
    IL_003f:  bne.un.s   IL_006a

    IL_0041:  ldarg.1
    IL_0042:  ldc.i4.2
    IL_0043:  conv.i8
    IL_0044:  bne.un.s   IL_006a

    IL_0046:  ldarg.2
    IL_0047:  ldfld      valuetype EStruct FStruct::a
    IL_004c:  ldfld      valuetype DStruct EStruct::a
    IL_0051:  ldfld      valuetype CStruct DStruct::a
    IL_0056:  ldfld      valuetype BStruct CStruct::a
    IL_005b:  ldfld      valuetype AStruct BStruct::a
    IL_0060:  ldfld      int64 AStruct::a
    IL_0065:  ldc.i4.3
    IL_0066:  conv.i8
    IL_0067:  ceq
    IL_0069:  ret

    IL_006a:  ldc.i4.0
    IL_006b:  ret
  } // end of method GitHub_27678::JumpTarget

  .method private hidebysig static bool  Test(int64 a,
                                              int64 b,
                                              valuetype FStruct c) cil managed noinlining
  {
    // Code size       63 (0x3f)
    .maxstack  3
    .locals init ([0] valuetype FStruct d)
    IL_0000:  ldarg.2
    IL_0001:  stloc.0
    IL_0002:  ldloca.s   d
    IL_0004:  ldloc.0
    IL_0005:  ldfld      valuetype EStruct FStruct::a
    IL_000a:  stfld      valuetype EStruct FStruct::b
    IL_000f:  ldloca.s   d
    IL_0011:  ldloc.0
    IL_0012:  ldfld      valuetype EStruct FStruct::d
    IL_0017:  stfld      valuetype EStruct FStruct::c
    IL_001c:  ldloca.s   d
    IL_001e:  ldloc.0
    IL_001f:  ldfld      valuetype EStruct FStruct::b
    IL_0024:  stfld      valuetype EStruct FStruct::a
    IL_0029:  ldarga.s   c
    IL_002b:  ldloc.0
    IL_002c:  ldfld      valuetype EStruct FStruct::a
    IL_0031:  stfld      valuetype EStruct FStruct::a

    IL_0039:  jmp       bool GitHub_27678::JumpTarget(int64,
                                                       int64,
                                                       valuetype FStruct)
    IL_003e:  ret
  } // end of method GitHub_27678::Test

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       62 (0x3e)
    .maxstack  3
    .locals init ([0] valuetype FStruct a,
             [1] bool r)
    IL_0000:  ldloca.s   a
    IL_0002:  initobj    FStruct
    IL_0008:  ldloca.s   a
    IL_000a:  ldflda     valuetype EStruct FStruct::a
    IL_000f:  ldflda     valuetype DStruct EStruct::a
    IL_0014:  ldflda     valuetype CStruct DStruct::a
    IL_0019:  ldflda     valuetype BStruct CStruct::a
    IL_001e:  ldflda     valuetype AStruct BStruct::a
    IL_0023:  ldc.i4.3
    IL_0024:  conv.i8
    IL_0025:  stfld      int64 AStruct::a
    IL_002a:  ldc.i4.1
    IL_002b:  conv.i8
    IL_002c:  ldc.i4.2
    IL_002d:  conv.i8
    IL_002e:  ldloc.0
    IL_002f:  call       bool GitHub_27678::Test(int64,
                                                 int64,
                                                 valuetype FStruct)
    IL_0034:  stloc.1
    IL_0035:  ldloc.1
    IL_0036:  brfalse.s  IL_003b

    IL_0038:  ldc.i4.s   100
    IL_003a:  ret

    IL_003b:  ldc.i4.s   101
    IL_003d:  ret
  } // end of method GitHub_27678::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Runtime]System.Object::.ctor()
    IL_0006:  ret
  } // end of method GitHub_27678::.ctor

} // end of class GitHub_27678
