// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}





// Metadata version: v4.0.50741
.assembly extern mscorlib
{
  .ver 0:0:0:0
}
.assembly Test_HndIndex_10_Reordered
{
  .custom instance void [mscorlib]System.Security.SecurityTransparentAttribute::.ctor() = ( 01 00 00 00 ) 
  .ver 0:0:0:0
}
.assembly extern xunit.core {}
.module Test_HndIndex_10_Reordered.exe
// MVID: {68AEE7ED-2AC8-4330-9EC7-4EBFD6BD74E0}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x00370000


// =============== CLASS MEMBERS DECLARATION ===================

.class private abstract auto ansi sealed beforefieldinit Test_Test_HndIndex_10_Reordered.App
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       106 (0x6a)
    .maxstack  8
    .locals init (class Test_Test_HndIndex_10_Reordered.DerivedClass V_0,
             int32 V_1)
    IL_0000:  ldstr      "Hello World"
    IL_0005:  ldstr      "Hello World 2"
    IL_000a:  newobj     instance void Test_Test_HndIndex_10_Reordered.DerivedClass::.ctor(string,
                                                                string)
    IL_000f:  stloc      V_0
    IL_0013:  ldstr      "Value of Test: "
    IL_0018:  ldloc      V_0
    IL_001c:  callvirt   instance string Test_Test_HndIndex_10_Reordered.BaseClass::get_Test()
    IL_0021:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0026:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002b:  ldstr      "FAIL: The double ctor call worked without generati"
    + "ng an exception"
    IL_0030:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0035:  ldc.i4     0x65
    IL_003a:  stloc      V_1
    IL_003e:  leave      IL_0065

    IL_0043:  callvirt   instance string [mscorlib]System.Exception::get_Message()
    IL_0048:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004d:  ldstr      "PASS: The double ctor call generated an exception"
    IL_0052:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0057:  ldc.i4     0x64
    IL_005c:  stloc      V_1
    IL_0060:  leave      IL_0065

    IL_0065:  ldloc      V_1
    IL_0069:  ret
    IL_006a:  
    // Exception count 1
    .try IL_0000 to IL_0043 catch [mscorlib]System.Exception handler IL_0043 to IL_0065
  } // end of method App::Main

  .method public hidebysig static void  WriteLine(string format) cil managed noinlining
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  ldarg      format
    IL_0004:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0009:  ret
  } // end of method App::WriteLine

} // end of class Test.App

.class private auto ansi beforefieldinit Test_Test_HndIndex_10_Reordered.DerivedClass
       extends Test_Test_HndIndex_10_Reordered.BaseClass
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string input,
                               string input2) cil managed noinlining
  {
    // Code size       25 (0x19)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg      input
    IL_0005:  call       instance void Test_Test_HndIndex_10_Reordered.BaseClass::.ctor(string)
    IL_000a:  call       void Test_Test_HndIndex_10_Reordered.DerivedClass::SetExceptionSP()
    IL_000f:  ldarg      input
    IL_0013:  call       void Test_Test_HndIndex_10_Reordered.DerivedClass::FillStackAndCallout(string)
    IL_0018:  ret
  } // end of method DerivedClass::.ctor

  .method public hidebysig static void  SetExceptionSP() cil managed noinlining
  {
    // Code size       36 (0x24)
    .maxstack  8
    IL_0000:  ldstr      "In the setup try block."
    IL_0005:  call       void Test_Test_HndIndex_10_Reordered.App::WriteLine(string)
    IL_000a:  call       void Test_Test_HndIndex_10_Reordered.DerivedClass::ThrowInvalidOperationException()
    IL_000f:  leave      IL_0023

    IL_0014:  ldstr      "In the setup catch block."
    IL_0019:  call       void Test_Test_HndIndex_10_Reordered.App::WriteLine(string)
    IL_001e:  leave      IL_0023

    IL_0023:  ret
    IL_0024:  
    // Exception count 1
    .try IL_0000 to IL_0014 catch [mscorlib]System.InvalidOperationException handler IL_0014 to IL_0023
  } // end of method DerivedClass::SetExceptionSP

  .method public hidebysig static void  FillStackAndCallout(string input) cil managed noinlining
  {
    // Code size       23 (0x17)
    .maxstack  8
    .locals init (valuetype Test_Test_HndIndex_10_Reordered.StackFiller16 V_0)
    IL_0000:  ldloca     V_0
    IL_0004:  call       void Test_Test_HndIndex_10_Reordered.StackFiller16::Fill(valuetype Test_Test_HndIndex_10_Reordered.StackFiller16&)
    IL_0009:  ldarg      input
    IL_000d:  ldloca     V_0
    IL_0011:  call       void Test_Test_HndIndex_10_Reordered.DerivedClass::AccessExceptionSP(string,
                                                                   valuetype Test_Test_HndIndex_10_Reordered.StackFiller16&)
    IL_0016:  ret
  } // end of method DerivedClass::FillStackAndCallout

  .method public hidebysig static void  AccessExceptionSP(string input,
                                                          valuetype Test_Test_HndIndex_10_Reordered.StackFiller16& filler1) cil managed noinlining
  {
    // Code size       92 (0x5c)
    .maxstack  8
    IL_0000:  ldstr      "In the outer try block."
    IL_0005:  call       void Test_Test_HndIndex_10_Reordered.App::WriteLine(string)
    IL_000a:  leave      IL_0042

    IL_000f:  ldstr      "In the outer finally block."
    IL_0014:  call       void Test_Test_HndIndex_10_Reordered.App::WriteLine(string)
    IL_0019:  ldarg      input
    IL_001d:  brtrue     IL_0032

    IL_0022:  ldstr      "In the inner try block."
    IL_0027:  call       void Test_Test_HndIndex_10_Reordered.App::WriteLine(string)
    IL_002c:  leave      IL_0041

    IL_0031:  pop
    IL_0032:  ldstr      "In the inner catch block."
    IL_0037:  call       void Test_Test_HndIndex_10_Reordered.App::WriteLine(string)
    IL_003c:  leave      IL_0041

    IL_0041:  endfinally
    IL_0042:  ldstr      "In the return block."
    IL_0047:  call       void Test_Test_HndIndex_10_Reordered.App::WriteLine(string)
    IL_004c:  br         IL_0051

    IL_0051:  ldstr      "In the real return block."
    IL_0056:  call       void Test_Test_HndIndex_10_Reordered.App::WriteLine(string)
    IL_005b:  ret
    IL_005c:  
    // Exception count 2
    .try IL_0000 to IL_000f finally handler IL_000f to IL_0042
    .try IL_0022 to IL_0031 catch [mscorlib]System.InvalidOperationException handler IL_0031 to IL_0041
  } // end of method DerivedClass::AccessExceptionSP

  .method public hidebysig static void  ThrowInvalidOperationException() cil managed noinlining
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_0005:  throw
  } // end of method DerivedClass::ThrowInvalidOperationException

} // end of class Test.DerivedClass

.class private auto ansi beforefieldinit Test_Test_HndIndex_10_Reordered.BaseClass
       extends [mscorlib]System.Object
{
  .field private string test
  .method public hidebysig specialname instance string 
          get_Test() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      string Test_Test_HndIndex_10_Reordered.BaseClass::test
    IL_0006:  ret
  } // end of method BaseClass::get_Test

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string input) cil managed noinlining
  {
    // Code size       31 (0x1f)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldstr      "In BaseClass::BaseClass(string) with parameter \"{0}\""
    IL_000b:  ldarg      input
    IL_000f:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0014:  ldarg.0
    IL_0015:  ldarg      input
    IL_0019:  stfld      string Test_Test_HndIndex_10_Reordered.BaseClass::test
    IL_001e:  ret
  } // end of method BaseClass::.ctor

  .property instance string Test_Test_HndIndex_10_Reordered()
  {
    .get instance string Test_Test_HndIndex_10_Reordered.BaseClass::get_Test()
  } // end of property BaseClass::Test
} // end of class Test.BaseClass

.class public sequential ansi sealed beforefieldinit Test_Test_HndIndex_10_Reordered.StackFiller16
       extends [mscorlib]System.ValueType
{
  .field public uint64 f01
  .field public uint64 f02
  .field public uint64 f03
  .field public uint64 f04
  .field public uint64 f05
  .field public uint64 f06
  .field public uint64 f07
  .field public uint64 f08
  .field public uint64 f09
  .field public uint64 f10
  .field public uint64 f11
  .field public uint64 f12
  .field public uint64 f13
  .field public uint64 f14
  .field public uint64 f15
  .field public uint64 f16
  .method public hidebysig static void  Fill(valuetype Test_Test_HndIndex_10_Reordered.StackFiller16& targetRef) cil managed noinlining
  {
    // Code size       289 (0x121)
    .maxstack  8
    IL_0000:  ldarg      targetRef
    IL_0004:  ldc.i8     0xabababababababab
    IL_000d:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f01
    IL_0012:  ldarg      targetRef
    IL_0016:  ldc.i8     0xabababababababab
    IL_001f:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f02
    IL_0024:  ldarg      targetRef
    IL_0028:  ldc.i8     0xabababababababab
    IL_0031:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f03
    IL_0036:  ldarg      targetRef
    IL_003a:  ldc.i8     0xabababababababab
    IL_0043:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f04
    IL_0048:  ldarg      targetRef
    IL_004c:  ldc.i8     0xabababababababab
    IL_0055:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f05
    IL_005a:  ldarg      targetRef
    IL_005e:  ldc.i8     0xabababababababab
    IL_0067:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f06
    IL_006c:  ldarg      targetRef
    IL_0070:  ldc.i8     0xabababababababab
    IL_0079:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f07
    IL_007e:  ldarg      targetRef
    IL_0082:  ldc.i8     0xabababababababab
    IL_008b:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f08
    IL_0090:  ldarg      targetRef
    IL_0094:  ldc.i8     0xabababababababab
    IL_009d:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f09
    IL_00a2:  ldarg      targetRef
    IL_00a6:  ldc.i8     0xabababababababab
    IL_00af:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f10
    IL_00b4:  ldarg      targetRef
    IL_00b8:  ldc.i8     0xabababababababab
    IL_00c1:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f11
    IL_00c6:  ldarg      targetRef
    IL_00ca:  ldc.i8     0xabababababababab
    IL_00d3:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f12
    IL_00d8:  ldarg      targetRef
    IL_00dc:  ldc.i8     0xabababababababab
    IL_00e5:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f13
    IL_00ea:  ldarg      targetRef
    IL_00ee:  ldc.i8     0xabababababababab
    IL_00f7:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f14
    IL_00fc:  ldarg      targetRef
    IL_0100:  ldc.i8     0xabababababababab
    IL_0109:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f15
    IL_010e:  ldarg      targetRef
    IL_0112:  ldc.i8     0xabababababababab
    IL_011b:  stfld      uint64 Test_Test_HndIndex_10_Reordered.StackFiller16::f16
    IL_0120:  ret
  } // end of method StackFiller16::Fill

} // end of class Test.StackFiller16


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
