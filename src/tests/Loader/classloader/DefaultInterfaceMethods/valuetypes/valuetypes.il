// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly valuetypes
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {E191F723-B724-4D70-B3A8-CEA89FD033D3}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01170000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IValue
{
  .method public hidebysig newslot abstract virtual 
          instance int32  GetValue() cil managed
  {
  } // end of method IValue::GetValue

  .method public hidebysig newslot abstract virtual 
          instance void  SetValue(int32 a) cil managed
  {
  } // end of method IValue::SetValue

  .method public hidebysig newslot virtual 
          instance int32  Add(int32 a) cil managed
  {
    // Code size       26 (0x1a)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  callvirt   instance int32 IValue::GetValue()
    IL_0007:  stloc.0
    IL_0008:  ldloc.0
    IL_0009:  ldarg.1
    IL_000a:  add
    IL_000b:  stloc.0
    IL_000c:  ldarg.0
    IL_000d:  ldloc.0
    IL_000e:  callvirt   instance void IValue::SetValue(int32)
    IL_0013:  nop
    IL_0014:  ldloc.0
    IL_0015:  stloc.1
    IL_0016:  br.s       IL_0018

    IL_0018:  ldloc.1
    IL_0019:  ret
  } // end of method IValue::Add

} // end of class IValue

.class private sequential ansi sealed beforefieldinit FooBarStruct
       extends [mscorlib]System.ValueType
       implements IValue
{
  .field public int32 _val
  .method public hidebysig newslot virtual final 
          instance int32  GetValue() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      int32 FooBarStruct::_val
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method FooBarStruct::GetValue

  .method public hidebysig newslot virtual final 
          instance void  SetValue(int32 val) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  stfld      int32 FooBarStruct::_val
    IL_0008:  ret
  } // end of method FooBarStruct::SetValue
} // end of class FooBarStruct

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init (valuetype FooBarStruct V_0,
             class IValue V_1,
             int32 V_2)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    FooBarStruct
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldc.i4.s   10
    IL_000d:  stfld      int32 FooBarStruct::_val
    IL_0012:  ldloc.0
    IL_0013:  box        FooBarStruct
    IL_0018:  stloc.1
    IL_0019:  ldstr      "Calling IFoo.Foo on FooBarStruct"
    IL_001e:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0023:  nop
    IL_0024:  ldloc.1
    IL_0025:  ldc.i4.s   10
    IL_0027:  callvirt   instance int32 IValue::Add(int32)
    IL_002c:  ldc.i4.s   20
    IL_002e:  ceq
    IL_0030:  ldstr      "Calling default method IValue.Add on FooBarStruct "
    + "failed"
    IL_0035:  call       void Test::Assert(bool,
                                           string)
    IL_003a:  nop
    IL_003b:  ldloca     V_0
              callvirt   instance int32 FooBarStruct::GetValue()
              ldc.i4.s   10
              ceq
              ldstr      "FooBarStruct value should remain unchanged"
              call       void Test::Assert(bool,
                                           string)
              nop
              call       int32 Test::Ret()
              ret
  } // end of method Program::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Program::.ctor

} // end of class Program

.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .field private static bool Pass
  .method public hidebysig static int32  Ret() cil managed
  {
    // Code size       19 (0x13)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldsfld     bool Test::Pass
    IL_0006:  brtrue.s   IL_000c

    IL_0008:  ldc.i4.s   101
    IL_000a:  br.s       IL_000e

    IL_000c:  ldc.i4.s   100
    IL_000e:  stloc.0
    IL_000f:  br.s       IL_0011

    IL_0011:  ldloc.0
    IL_0012:  ret
  } // end of method Test::Ret

  .method public hidebysig static void  Assert(bool cond,
                                               string msg) cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0004:  brfalse.s  IL_0015

    IL_0006:  nop
    IL_0007:  ldstr      "PASS"
    IL_000c:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0011:  nop
    IL_0012:  nop
    IL_0013:  br.s       IL_002e

    IL_0015:  nop
    IL_0016:  ldstr      "FAIL: "
    IL_001b:  ldarg.1
    IL_001c:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0021:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0026:  nop
    IL_0027:  ldc.i4.0
    IL_0028:  stsfld     bool Test::Pass
    IL_002d:  nop
    IL_002e:  ret
  } // end of method Test::Assert

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method Test::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  stsfld     bool Test::Pass
    IL_0006:  ret
  } // end of method Test::.cctor

} // end of class Test


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file valuetypes.res
