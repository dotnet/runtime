// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}


// Metadata version: v4.0.21016
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly TestCase
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module TestCase.exe
// MVID: {3357017E-FF15-4114-B1F9-AB857327E8CC}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x001F0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit TestCase
       extends [mscorlib]System.Object
{
  .method private hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method TestCase::.ctor

  .method public hidebysig newslot virtual 
          instance void  Activate() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Activate()"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method TestCase::Activate

  .method public hidebysig newslot virtual 
          instance void  Deactivate() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "Deactivate()"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method TestCase::Deactivate

  .method public hidebysig newslot virtual 
          instance void  LightUp() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "LightUp()"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method TestCase::LightUp

  .method public hidebysig newslot virtual 
          instance void  DimOut() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldstr      "DimOut()"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ret
  } // end of method TestCase::DimOut

  .method public hidebysig newslot virtual 
          instance void  Test(class TestCase obj,
                              bool b1,
                              bool b2) cil managed
  {
    // Code size       54 (0x36)
    .maxstack  1
    IL_0000:  br.s       IL_0029
    IL_0002:  brtrue.s   IL_0005
    IL_0004:  ret
    IL_0005:  br.s       IL_002c
    IL_0007:  brfalse.s  IL_0012
    IL_0009:  br.s       IL_002f
    IL_000b:  callvirt   instance void TestCase::Activate()
    IL_0010:  br.s       IL_0018
    IL_0012:  ldarg.1
    IL_0013:  callvirt   instance void TestCase::Deactivate()
    IL_0018:  ldarg.3
    IL_0019:  brfalse.s  IL_0022
    IL_001b:  ldarg.1
    IL_001c:  callvirt   instance void TestCase::LightUp()
    IL_0021:  ret
    IL_0022:  ldarg.1
    IL_0023:  callvirt   instance void TestCase::DimOut()
    IL_0028:  ret
    IL_0029:  ldarg.1
    IL_002a:  br.s       IL_0002
    IL_002c:  ldarg.2
    IL_002d:  br.s       IL_0007
    IL_002f:  ldarg.1
    IL_0030:  br.s       IL_000b
  } // end of method TestCase::Test

  .method public hidebysig newslot virtual 
          instance void  TestWrap(class TestCase obj,
                                  bool b1,
                                  bool b2) cil managed
  {
    // Code size       95 (0x5f)
    .maxstack  4
    .locals init (object[] V_0)
    IL_0000:  ldstr      "============================================"
    IL_0005:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000a:  ldc.i4.5
    IL_000b:  newarr     [mscorlib]System.Object
    IL_0010:  stloc.0
    IL_0011:  ldloc.0
    IL_0012:  ldc.i4.0
    IL_0013:  ldstr      "calling Test(obj, "
    IL_0018:  stelem.ref
    IL_0019:  ldloc.0
    IL_001a:  ldc.i4.1
    IL_001b:  ldarg.2
    IL_001c:  box        [mscorlib]System.Boolean
    IL_0021:  stelem.ref
    IL_0022:  ldloc.0
    IL_0023:  ldc.i4.2
    IL_0024:  ldstr      ", "
    IL_0029:  stelem.ref
    IL_002a:  ldloc.0
    IL_002b:  ldc.i4.3
    IL_002c:  ldarg.3
    IL_002d:  box        [mscorlib]System.Boolean
    IL_0032:  stelem.ref
    IL_0033:  ldloc.0
    IL_0034:  ldc.i4.4
    IL_0035:  ldstr      ")"
    IL_003a:  stelem.ref
    IL_003b:  ldloc.0
    IL_003c:  call       string [mscorlib]System.String::Concat(object[])
    IL_0041:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0046:  ldarg.1
    IL_0047:  ldarg.1
    IL_0048:  ldarg.2
    IL_0049:  ldarg.3
    IL_004a:  callvirt   instance void TestCase::Test(class TestCase,
                                                      bool,
                                                      bool)
    IL_004f:  ldstr      "============================================"
    IL_0054:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0059:  call       void [System.Console]System.Console::WriteLine()
    IL_005e:  ret
  } // end of method TestCase::TestWrap

  .method public hidebysig static int32  Main() cil managed
  {
    .entrypoint
    // Code size       44 (0x2c)
    .maxstack  4
    .locals init (class TestCase V_0)
    IL_0000:  newobj     instance void TestCase::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldc.i4.0
    IL_0009:  ldc.i4.0
    IL_000a:  callvirt   instance void TestCase::TestWrap(class TestCase,
                                                          bool,
                                                          bool)
    IL_000f:  ldloc.0
    IL_0010:  ldloc.0
    IL_0011:  ldc.i4.0
    IL_0012:  ldc.i4.1
    IL_0013:  callvirt   instance void TestCase::TestWrap(class TestCase,
                                                          bool,
                                                          bool)
    IL_0018:  ldloc.0
    IL_0019:  ldloc.0
    IL_001a:  ldc.i4.1
    IL_001b:  ldc.i4.0
    IL_001c:  callvirt   instance void TestCase::TestWrap(class TestCase,
                                                          bool,
                                                          bool)
    IL_0021:  ldloc.0
    IL_0022:  ldloc.0
    IL_0023:  ldc.i4.1
    IL_0024:  ldc.i4.1
    IL_0025:  callvirt   instance void TestCase::TestWrap(class TestCase,
                                                          bool,
                                                          bool)
    IL_002a:  ldc.i4 100
    IL_002b:  ret
  } // end of method TestCase::Main

} // end of class TestCase


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file TestCase.res
