// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}





// Metadata version: v4.0.AMD64chk
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly TailCallCases
{

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 03 00 00 00 00 00 ) 

  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 4:0:20225:0
}
// MVID: {E63A46BE-A0B6-41DA-82A6-56B3E895EC8C}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00000000000B0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private sequential ansi sealed beforefieldinit ETW.threeByteStruct
       extends [mscorlib]System.ValueType
{
  .field public uint8 x
  .field public uint8 y
  .field public uint8 z
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(uint8 s,
                               uint8 t,
                               uint8 u) cil managed
  {
    // Code size       22 (0x16)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  stfld      uint8 ETW.threeByteStruct::x
    IL_0007:  ldarg.0
    IL_0008:  ldarg.2
    IL_0009:  stfld      uint8 ETW.threeByteStruct::y
    IL_000e:  ldarg.0
    IL_000f:  ldarg.3
    IL_0010:  stfld      uint8 ETW.threeByteStruct::z
    IL_0015:  ret
  } // end of method threeByteStruct::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(valuetype ETW.threeByteStruct s) cil managed
  {
    // Code size       40 (0x28)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarga.s   s
    IL_0003:  ldfld      uint8 ETW.threeByteStruct::x
    IL_0008:  stfld      uint8 ETW.threeByteStruct::x
    IL_000d:  ldarg.0
    IL_000e:  ldarga.s   s
    IL_0010:  ldfld      uint8 ETW.threeByteStruct::y
    IL_0015:  stfld      uint8 ETW.threeByteStruct::y
    IL_001a:  ldarg.0
    IL_001b:  ldarga.s   s
    IL_001d:  ldfld      uint8 ETW.threeByteStruct::z
    IL_0022:  stfld      uint8 ETW.threeByteStruct::z
    IL_0027:  ret
  } // end of method threeByteStruct::.ctor

} // end of class ETW.threeByteStruct

.class public auto ansi beforefieldinit ETW.TailCallETW
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          optimized(int32 x) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  tail. call       int32 ETW.TailCallETW::recursiveHelper(int32,
                                                                int32)
    IL_0007:  ret
  } // end of method TailCallETW::optimized

  .method private hidebysig static int32 
          recursive(int32 x) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  tail. call       int32 ETW.TailCallETW::recursiveHelper(int32,
                                                                int32)
    IL_0007:  ret
  } // end of method TailCallETW::recursive

  .method private hidebysig static int32 
          recursiveHelper(int32 x,
                          int32 result) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  beq.s      IL_0010

    IL_0004:  ldarg.0
    IL_0005:  ldc.i4.1
    IL_0006:  sub
    IL_0007:  ldarg.0
    IL_0008:  ldarg.1
    IL_0009:  mul
    IL_000a:  tail. call       int32 ETW.TailCallETW::recursiveHelper(int32,
                                                                int32)
    IL_000f:  ret

    IL_0010:  ldarg.1
    IL_0011:  ret
  } // end of method TailCallETW::recursiveHelper

  .method private hidebysig static int32 
          helper(valuetype ETW.threeByteStruct s) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  tail. call       int32 ETW.TailCallETW::helperHelper(valuetype ETW.threeByteStruct,
                                                             int32)
    IL_0007:  ret
  } // end of method TailCallETW::helper

  .method private hidebysig static int32 
          helperHelper(valuetype ETW.threeByteStruct s,
                       int32 result) cil managed
  {
    // Code size       52 (0x34)
    .maxstack  3
    .locals init ([0] valuetype ETW.threeByteStruct sMinus1)
    IL_0000:  ldloca.s   sMinus1
    IL_0002:  ldarg.0
    IL_0003:  call       instance void ETW.threeByteStruct::.ctor(valuetype ETW.threeByteStruct)
    IL_0008:  ldloca.s   sMinus1
    IL_000a:  dup
    IL_000b:  ldfld      uint8 ETW.threeByteStruct::x
    IL_0010:  ldc.i4.1
    IL_0011:  sub
    IL_0012:  conv.u1
    IL_0013:  stfld      uint8 ETW.threeByteStruct::x
    IL_0018:  ldarga.s   s
    IL_001a:  ldfld      uint8 ETW.threeByteStruct::x
    IL_001f:  ldc.i4.1
    IL_0020:  beq.s      IL_0032

    IL_0022:  ldloc.0
    IL_0023:  ldarga.s   s
    IL_0025:  ldfld      uint8 ETW.threeByteStruct::x
    IL_002a:  ldarg.1
    IL_002b:  mul
    IL_002c:  tail. call       int32 ETW.TailCallETW::helperHelper(valuetype ETW.threeByteStruct,
                                                             int32)
    IL_0031:  ret

    IL_0032:  ldarg.1
    IL_0033:  ret
  } // end of method TailCallETW::helperHelper

  .method private hidebysig static int32 
          failed(int32 x) cil managed synchronized
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  tail. call       int32 ETW.TailCallETW::failedHelper(int32,
                                                             int32)
    IL_0007:  ret
  } // end of method TailCallETW::failed

  .method private hidebysig static int32 
          failedHelper(int32 x,
                       int32 result) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  beq.s      IL_0010

    IL_0004:  ldarg.0
    IL_0005:  ldc.i4.1
    IL_0006:  sub
    IL_0007:  ldarg.0
    IL_0008:  ldarg.1
    IL_0009:  mul
    IL_000a:  tail. call       int32 ETW.TailCallETW::failedHelper(int32,
                                                             int32)
    IL_000f:  ret

    IL_0010:  ldarg.1
    IL_0011:  ret
  } // end of method TailCallETW::failedHelper

  .method public hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       103 (0x67)
    .maxstack  4
    .locals init ([0] int32 x,
             [1] valuetype ETW.threeByteStruct s)
    IL_0000:  ldc.i4.s   10
    IL_0002:  stloc.0
    IL_0003:  ldloca.s   s
    IL_0005:  ldc.i4.s   10
    IL_0007:  ldc.i4.s   11
    IL_0009:  ldc.i4.s   12
    IL_000b:  call       instance void ETW.threeByteStruct::.ctor(uint8,
                                                                  uint8,
                                                                  uint8)
    IL_0010:  ldstr      "Recursive gives: {0}"
    IL_0015:  ldloc.0
    IL_0016:  call       int32 ETW.TailCallETW::recursive(int32)
    IL_001b:  box        [mscorlib]System.Int32
    IL_0020:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0025:  ldstr      "Optimized gives: {0}"
    IL_002a:  ldloc.0
    IL_002b:  call       int32 ETW.TailCallETW::optimized(int32)
    IL_0030:  box        [mscorlib]System.Int32
    IL_0035:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_003a:  ldstr      "Helper gives: {0}"
    IL_003f:  ldloc.1
    IL_0040:  call       int32 ETW.TailCallETW::helper(valuetype ETW.threeByteStruct)
    IL_0045:  box        [mscorlib]System.Int32
    IL_004a:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_004f:  ldstr      "Failed gives: {0}"
    IL_0054:  ldloc.0
    IL_0055:  call       int32 ETW.TailCallETW::failed(int32)
    IL_005a:  box        [mscorlib]System.Int32
    IL_005f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0064:  ldc.i4.s   100
    IL_0066:  ret
  } // end of method TailCallETW::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method TailCallETW::.ctor

} // end of class ETW.TailCallETW


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
