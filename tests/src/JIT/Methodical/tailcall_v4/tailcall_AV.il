// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

// Metadata version: v4.0.AMD64chk
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly o
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module o.exe
// MVID: {E5624CCE-2F8F-498D-8C7E-6479B0B6CEC7}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000000000970000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit TCS.TailCallStructs
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  Head_0() cil managed noinlining
  {
    // Code size       33 (0x21)
    .maxstack  2
    .locals init (int32 V_0,
             valuetype CSCreate.MB1 V_1)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldloca.s   V_1
    IL_0004:  initobj    CSCreate.MB1
    IL_000a:  ldloca.s   V_1
    IL_000c:  ldc.i4.1
    IL_000d:  stfld      uint8 CSCreate.MB1::b0
    IL_0012:  ldloc.1
    IL_0013:  ldloca.s   V_0
    IL_0015:  call       void TCS.TailCallStructs::Caller_0(valuetype CSCreate.MB1,
                                                            int32&)
    IL_001a:  ldloc.0
    IL_001b:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0020:  ret
  } // end of method TailCallStructs::Head_0

  .method public hidebysig static void  Caller_0(valuetype CSCreate.MB1 mbs0,
                                                 int32& result) cil managed noinlining
  {
    // Code size       18 (0x12)
    .maxstack  2
    .locals init (typedref V_0)
    IL_0000:  ldarga.s   mbs0
    IL_0002:  mkrefany   CSCreate.MB1
    IL_0007:  stloc.0
    IL_0008:  ldarg.1
    IL_0009:  ldloc.0
    IL_000a:  tail.
    IL_000c:  call       void TCS.TailCallStructs::Callee_0(int32&,
                                                            typedref)
    IL_0011:  ret
  } // end of method TailCallStructs::Caller_0

  .method public hidebysig static void  Callee_0(int32& result,
                                                 typedref tr0) cil managed noinlining
  {
    // Code size       37 (0x25)
    .maxstack  3
    .locals init (valuetype CSCreate.MB1 V_0)
    IL_0000:  ldarg.1
    IL_0001:  refanyval  CSCreate.MB1
    IL_0006:  ldobj      CSCreate.MB1
    IL_000b:  stloc.0
    IL_000c:  ldloca.s   V_0
    IL_000e:  ldfld      uint8 CSCreate.MB1::b0
    IL_0013:  call       void [System.Console]System.Console::WriteLine(int32)
    IL_0018:  ldarg.0
    IL_0019:  dup
    IL_001a:  ldind.i4
    IL_001b:  ldloca.s   V_0
    IL_001d:  ldfld      uint8 CSCreate.MB1::b0
    IL_0022:  add
    IL_0023:  stind.i4
    IL_0024:  ret
  } // end of method TailCallStructs::Callee_0

  .method public hidebysig static int32 Main() cil managed
  {
    .entrypoint
    // Code size       26 (0x1a)
    .maxstack  8
    IL_0000:  call       void TCS.TailCallStructs::Head_0()
ldc.i4 100
    IL_0019:  ret
  } // end of method TailCallStructs::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method TailCallStructs::.ctor

} // end of class TCS.TailCallStructs

.class private sequential ansi sealed beforefieldinit CSCreate.MB1
       extends [mscorlib]System.ValueType
{
  .field public uint8 b0
} // end of class CSCreate.MB1

.class private sequential ansi sealed beforefieldinit CSCreate.MB2
       extends [mscorlib]System.ValueType
{
  .field public uint8 b0
  .field public uint8 b1
} // end of class CSCreate.MB2

.class private sequential ansi sealed beforefieldinit CSCreate.MB3
       extends [mscorlib]System.ValueType
{
  .field public uint8 b0
  .field public uint8 b1
  .field public uint8 b2
} // end of class CSCreate.MB3

.class private sequential ansi sealed beforefieldinit CSCreate.MB4
       extends [mscorlib]System.ValueType
{
  .field public uint8 b0
  .field public uint8 b1
  .field public uint8 b2
  .field public uint8 b3
} // end of class CSCreate.MB4

.class private sequential ansi sealed beforefieldinit CSCreate.MB5
       extends [mscorlib]System.ValueType
{
  .field public uint8 b0
  .field public uint8 b1
  .field public uint8 b2
  .field public uint8 b3
  .field public uint8 b4
} // end of class CSCreate.MB5

.class private sequential ansi sealed beforefieldinit CSCreate.MB6
       extends [mscorlib]System.ValueType
{
  .field public uint8 b0
  .field public uint8 b1
  .field public uint8 b2
  .field public uint8 b3
  .field public uint8 b4
  .field public uint8 b5
} // end of class CSCreate.MB6

.class private sequential ansi sealed beforefieldinit CSCreate.MB7
       extends [mscorlib]System.ValueType
{
  .field public uint8 b0
  .field public uint8 b1
  .field public uint8 b2
  .field public uint8 b3
  .field public uint8 b4
  .field public uint8 b5
  .field public uint8 b6
} // end of class CSCreate.MB7

.class private sequential ansi sealed beforefieldinit CSCreate.MB8
       extends [mscorlib]System.ValueType
{
  .field public uint8 b0
  .field public uint8 b1
  .field public uint8 b2
  .field public uint8 b3
  .field public uint8 b4
  .field public uint8 b5
  .field public uint8 b6
  .field public uint8 b7
} // end of class CSCreate.MB8


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
