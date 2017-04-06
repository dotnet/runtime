
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.6.1055.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly methodimpl
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module methodimpl.exe
// MVID: {F249A85F-3FD2-488F-B084-97B542AD68D6}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02FD0000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IFoo
{
  .method public hidebysig newslot virtual 
          instance int32  Foo(int32 a) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFoo::Foo"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   10
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IFoo::Foo

} // end of class IFoo

.class interface private abstract auto ansi IBar
{
  .method public hidebysig newslot virtual 
          instance int32  Bar(int32 b) cil managed
  {
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IBar::Bar"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   20
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method IBar::Bar

} // end of class IBar

.class interface private abstract auto ansi IFooBar
       implements IFoo,
                  IBar
{
  .method private hidebysig newslot virtual final 
          instance int32  IFoo.Foo(int32 a) cil managed
  {
    .override IFoo::Foo
    // Code size       21 (0x15)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldstr      "At IFooBar::IFoo.Foo explicit methodimpl"
    IL_0006:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldarg.1
    IL_000d:  ldc.i4.s   30
    IL_000f:  add
    IL_0010:  stloc.0
    IL_0011:  br.s       IL_0013

    IL_0013:  ldloc.0
    IL_0014:  ret
  } // end of method Temp::IFoo.Foo

} // end of class IFooBar

.class private auto ansi beforefieldinit FooBar
       extends [mscorlib]System.Object
       implements IFooBar,
                  IFoo,
                  IBar
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method FooBar::.ctor

} // end of class FooBar

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       89 (0x59)
    .maxstack  2
    .locals init (class FooBar V_0,
             class IFoo V_1,
             class IBar V_2,
             int32 V_3)
    IL_0000:  nop
    IL_0001:  newobj     instance void FooBar::.ctor()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  stloc.1
    IL_0009:  ldloc.0
    IL_000a:  stloc.2
    IL_000b:  ldstr      "Calling IFoo.Foo on FooBar - expecting IFooBar::IF"
    + "oo.Bar"
    IL_0010:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0015:  nop
    IL_0016:  ldloc.1
    IL_0017:  ldc.i4.s   10
    IL_0019:  callvirt   instance int32 IFoo::Foo(int32)
    IL_001e:  ldc.i4.s   40
    IL_0020:  ceq
    IL_0022:  ldstr      "Calling IFoo.Foo on FooBar"
    IL_0027:  call       void Test::Assert(bool,
                                           string)
    IL_002c:  nop
    IL_002d:  ldstr      "Calling IBar.Bar on FooBar - expecting IBar::Bar"
    IL_0032:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_0037:  nop
    IL_0038:  ldloc.2
    IL_0039:  ldc.i4.s   10
    IL_003b:  callvirt   instance int32 IBar::Bar(int32)
    IL_0040:  ldc.i4.s   30
    IL_0042:  ceq
    IL_0044:  ldstr      "Calling IBar.Bar on FooBar"
    IL_0049:  call       void Test::Assert(bool,
                                           string)
    IL_004e:  nop
    IL_004f:  call       int32 Test::Ret()
    IL_0054:  stloc.3
    IL_0055:  br.s       IL_0057

    IL_0057:  ldloc.3
    IL_0058:  ret
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
// WARNING: Created Win32 resource file methodimpl.res
