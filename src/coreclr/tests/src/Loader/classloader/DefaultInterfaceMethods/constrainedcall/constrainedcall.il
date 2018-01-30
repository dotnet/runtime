// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.6.1055.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly constrainedcall
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 07 01 00 00 00 00 ) 

  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module constrainedcall.exe
// MVID: {6171EA0F-1009-482D-8EF6-C944886D5D66}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01860000


// =============== CLASS MEMBERS DECLARATION ===================

.class interface private abstract auto ansi IFoo
{
  .method public hidebysig newslot virtual 
          instance int32  Foo(int32 c) cil managed
  {
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (class IAdd V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  castclass  IAdd
    IL_0007:  stloc.0
    IL_0008:  ldloc.0
    IL_0009:  ldarg.1
    IL_000a:  callvirt   instance int32 IAdd::Add(int32)
    IL_000f:  stloc.1
    IL_0010:  br.s       IL_0012

    IL_0012:  ldloc.1
    IL_0013:  ret
  } // end of method IFoo::Foo

} // end of class IFoo

.class interface private abstract auto ansi IAdd
{
  .method public hidebysig newslot abstract virtual 
          instance int32  Add(int32 c) cil managed
  {
  } // end of method IAdd::Add

} // end of class IAdd

.class private sequential ansi sealed beforefieldinit FooValue
       extends [mscorlib]System.ValueType
       implements IFoo,
                  IAdd
{
  .field public int32 val

  .method public hidebysig newslot virtual final 
          instance int32  Add(int32 c) cil managed
  {
    // Code size       26 (0x1a)
    .maxstack  3
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.0
    IL_0003:  ldfld      int32 FooValue::val
    IL_0008:  ldarg.1
    IL_0009:  add
    IL_000a:  stfld      int32 FooValue::val
    IL_000f:  ldarg.0
    IL_0010:  ldfld      int32 FooValue::val
    IL_0015:  stloc.0
    IL_0016:  br.s       IL_0018

    IL_0018:  ldloc.0
    IL_0019:  ret
  } // end of method FooValue::Add

} // end of class FooValue

.class interface private abstract auto ansi IHorrible`1<T>
{
  .method public hidebysig newslot abstract virtual 
          instance int32  GetLocalVal() cil managed
  {
  } // end of method IHorrible`1::GetLocalVal

  .method public hidebysig newslot abstract virtual 
          instance void  SetLocalVal(int32 val) cil managed
  {
  } // end of method IHorrible`1::SetLocalVal

  .method public hidebysig newslot virtual 
          instance int32  Horrible() cil managed
  {
    // Code size       26 (0x1a)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  callvirt       instance int32 class IHorrible`1<!T>::GetLocalVal()
    IL_0007:  stloc.0
    IL_0008:  ldloc.0
    IL_0009:  ldc.i4.1
    IL_000a:  add
    IL_000b:  stloc.0
    IL_000c:  ldarg.0
    IL_000d:  ldloc.0
    IL_000e:  callvirt       instance void class IHorrible`1<!T>::SetLocalVal(int32)
    IL_0013:  nop
    IL_0014:  ldloc.0
    IL_0015:  stloc.1
    IL_0016:  br.s       IL_0018

    IL_0018:  ldloc.1
    IL_0019:  ret
  } // end of method IHorrible`1::Horrible

} // end of class IHorrible`1

.class private sequential ansi sealed beforefieldinit HorribleCase`1<Z>
       extends [mscorlib]System.ValueType
       implements class IHorrible`1<class [mscorlib]System.Collections.Generic.IList`1<!Z>>,
                  class IHorrible`1<class [mscorlib]System.Collections.Generic.IEnumerable`1<!Z>>
{
  .field private int32 localVal
  .method public hidebysig newslot virtual final 
          instance int32  GetLocalVal() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldfld      int32 valuetype HorribleCase`1<!Z>::localVal
    IL_0007:  stloc.0
    IL_0008:  br.s       IL_000a

    IL_000a:  ldloc.0
    IL_000b:  ret
  } // end of method HorribleCase`1::GetLocalVal

  .method public hidebysig newslot virtual final 
          instance void  SetLocalVal(int32 val) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  stfld      int32 valuetype HorribleCase`1<!Z>::localVal
    IL_0008:  ret
  } // end of method HorribleCase`1::SetLocalVal

  .method private hidebysig newslot virtual final 
          instance int32  'IHorrible<System.Collections.Generic.IList<Z>>.Horrible'() cil managed
  {
    .override  method instance int32 class IHorrible`1<class [mscorlib]System.Collections.Generic.IList`1<!Z>>::Horrible()
    // Code size       23 (0x17)
    .maxstack  3
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.0
    IL_0003:  ldfld      int32 valuetype HorribleCase`1<!Z>::localVal
    IL_0008:  ldc.i4.1
    IL_0009:  add
    IL_000a:  stloc.0
    IL_000b:  ldloc.0
    IL_000c:  stfld      int32 valuetype HorribleCase`1<!Z>::localVal
    IL_0011:  ldloc.0
    IL_0012:  stloc.1
    IL_0013:  br.s       IL_0015

    IL_0015:  ldloc.1
    IL_0016:  ret
  } // end of method HorribleCase`1::'IHorrible<System.Collections.Generic.IList<Z>>.Horrible'
} // end of class HorribleCase`1

.class private auto ansi beforefieldinit HorribleTest
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Horror<(class IHorrible`1<!!U>) T,U>(!!T t) cil managed
  {
    // Code size       33 (0x21)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarga.s   t
    IL_0003:  constrained. !!T
    IL_0009:  callvirt   instance int32 class IHorrible`1<!!U>::Horrible()
    IL_000e:  ldarga.s   t
    IL_0010:  constrained. !!T
    IL_0016:  callvirt   instance int32 class IHorrible`1<!!U>::Horrible()
    IL_001b:  add
    IL_001c:  stloc.0
    IL_001d:  br.s       IL_001f

    IL_001f:  ldloc.0
    IL_0020:  ret
  } // end of method HorribleTest::Horror

  .method public hidebysig static void  RunTest() cil managed
  {
    // Code size       58 (0x3a)
    .maxstack  2
    .locals init (valuetype HorribleCase`1<object> V_0)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    valuetype HorribleCase`1<object>
    IL_0009:  ldloc.0
    IL_000a:  call       int32 HorribleTest::Horror<valuetype HorribleCase`1<object>,class [mscorlib]System.Collections.Generic.IEnumerable`1<object>>(!!0)
    IL_000f:  ldc.i4.3
    IL_0010:  ceq
    IL_0012:  ldstr      "Fail"
    IL_0017:  call       void Test::Assert(bool,
                                           string)
    IL_001c:  nop
    IL_001d:  ldloca.s   V_0
    IL_001f:  initobj    valuetype HorribleCase`1<object>
    IL_0025:  ldloc.0
    IL_0026:  call       int32 HorribleTest::Horror<valuetype HorribleCase`1<object>,class [mscorlib]System.Collections.Generic.IList`1<object>>(!!0)
    IL_002b:  ldc.i4.0
    IL_002c:  ceq
    IL_002e:  ldstr      "Fail"
    IL_0033:  call       void Test::Assert(bool,
                                           string)
    IL_0038:  nop
    IL_0039:  ret
  } // end of method HorribleTest::RunTest

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method HorribleTest::.ctor

} // end of class HorribleTest

.class private auto ansi beforefieldinit SimpleConstraintTest
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  CallFoo_WithConstraints<(IFoo) T>(!!T& foo,
                                                                           int32 val) cil managed
  {
    // Code size       19 (0x13)
    .maxstack  2
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  ldarg.0
    IL_0002:  ldarg.1
    IL_0003:  constrained. !!T
    IL_0009:  callvirt   instance int32 IFoo::Foo(int32)
    IL_000e:  stloc.0
    IL_000f:  br.s       IL_0011

    IL_0011:  ldloc.0
    IL_0012:  ret
  } // end of method SimpleConstraintTest::CallFoo_WithConstraints

  .method public hidebysig static void  RunTest() cil managed
  {
    // Code size       75 (0x4b)
    .maxstack  2
    .locals init (valuetype FooValue V_0)
    IL_0000:  nop
    IL_0001:  ldloca.s   V_0
    IL_0003:  initobj    FooValue
    IL_0009:  ldloca.s   V_0
    IL_000b:  ldc.i4.s   10
    IL_000d:  stfld      int32 FooValue::val
    IL_0012:  ldstr      "Calling CallFoo_WithConstraints on FooValue - expe"
    + "cting IFoo::Foo"
    IL_0017:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_001c:  nop
    IL_001d:  ldloca.s   V_0
    IL_001f:  ldc.i4.s   10
    IL_0021:  call       int32 SimpleConstraintTest::CallFoo_WithConstraints<valuetype FooValue>(!!0&,
                                                                                                 int32)
    IL_0026:  ldc.i4.s   20
    IL_0028:  ceq
    IL_002a:  ldstr      "Calling CallFoo_WithConstraints on FooValue"
    IL_002f:  call       void Test::Assert(bool,
                                           string)
    IL_0034:  nop
    IL_0035:  ldloc.0
    IL_0036:  ldfld      int32 FooValue::val
    IL_003b:  ldc.i4.s   10
    IL_003d:  ceq
    IL_003f:  ldstr      "Expecting boxing on CallFoo_WithConstraints"
    IL_0044:  call       void Test::Assert(bool,
                                           string)
    IL_0049:  nop
    IL_004a:  ret
  } // end of method SimpleConstraintTest::RunTest

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  nop
    IL_0007:  ret
  } // end of method SimpleConstraintTest::.ctor

} // end of class SimpleConstraintTest

.class private auto ansi beforefieldinit Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       23 (0x17)
    .maxstack  1
    .locals init (int32 V_0)
    IL_0000:  nop
    IL_0001:  call       void HorribleTest::RunTest()
    IL_0006:  nop
    IL_0007:  call       void SimpleConstraintTest::RunTest()
    IL_000c:  nop
    IL_000d:  call       int32 Test::Ret()
    IL_0012:  stloc.0
    IL_0013:  br.s       IL_0015

    IL_0015:  ldloc.0
    IL_0016:  ret
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
// WARNING: Created Win32 resource file constrainedcall.res
