// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}





// Metadata version: v2.0.50523
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'test_62322'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {9AF9288D-3BE9-4952-AFD0-E77098E2DD26}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x0000000003500000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Test_test.Widget`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          Foo() cil managed noinlining
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "Foo"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
    IL_000c:  ldnull
    IL_000d:  starg      0
    IL_0011:  call       void class Test_test.Widget`1<!T>::Hello()    // Here was the exception: System.NullReferenceException: Object reference not set to an instance of an object
    IL_0016:  nop
    IL_0017:  ret
  } // end of method Widget`1::Foo

  .method public hidebysig static void  Hello() cil managed noinlining
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Widget`1::Hello

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Widget`1::.ctor

} // end of class Test.Widget`1

.class public abstract auto ansi sealed beforefieldinit Test_test.Program
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       51 (0x33)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  call       int32 Test_test.Program::RunTests<string>()
    IL_0008:  ldc.i4.0
    IL_0009:  ceq
    IL_000b:  stloc.2
    IL_000c:  ldloc.2
    IL_000d:  brtrue.s   IL_0020

    IL_000f:  nop
    IL_0010:  ldstr      "Test FAILED"
    IL_0015:  call       void [System.Console]System.Console::WriteLine(string)
    IL_001a:  nop
    IL_001b:  ldc.i4.s   101
    IL_001d:  stloc.1
    IL_001e:  br.s       IL_0031

    IL_0020:  nop
    IL_0021:  ldstr      "Test SUCCESS"
    IL_0026:  call       void [System.Console]System.Console::WriteLine(string)
    IL_002b:  nop
    IL_002c:  ldc.i4.s   100
    IL_002e:  stloc.1
    IL_002f:  br.s       IL_0031

    IL_0031:  ldloc.1
    IL_0032:  ret
  } // end of method Program::Main

  .method private hidebysig static int32 
          RunTests<T>() cil managed noinlining
  {
    // Code size       83 (0x53)
    .maxstack  2
    .locals init (int32 V_0,
             class [mscorlib]System.NullReferenceException V_1,
             int32 V_2)
    IL_0000:  nop
    IL_0001:  ldstr      "Running tests for {0}"
    IL_0006:  ldtoken    !!T
    IL_000b:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0010:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_0015:  nop
    IL_0016:  ldc.i4.0
    IL_0017:  stloc.0
    IL_0018:  nop
    IL_0019:  newobj     instance void class Test_test.Widget`1<!!T>::.ctor()
    IL_001e:  call       instance void class Test_test.Widget`1<!!T>::Foo()
    IL_0023:  nop
    IL_0024:  nop
    IL_0025:  leave.s    IL_004c

    IL_0027:  stloc.1
    IL_0028:  nop
    IL_0029:  ldstr      "Widget<{0}>.Foo() failed"
    IL_002e:  ldtoken    !!T
    IL_0033:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_0038:  call       void [System.Console]System.Console::WriteLine(string,
                                                                  object)
    IL_003d:  nop
    IL_003e:  ldloc.1
    IL_003f:  call       void [System.Console]System.Console::WriteLine(object)
    IL_0044:  nop
    IL_0045:  ldloc.0
    IL_0046:  ldc.i4.1
    IL_0047:  add
    IL_0048:  stloc.0
    IL_0049:  nop
    IL_004a:  leave.s    IL_004c

    IL_004c:  nop
    IL_004d:  ldloc.0
    IL_004e:  stloc.2
    IL_004f:  br.s       IL_0051

    IL_0051:  ldloc.2
    IL_0052:  ret
    IL_0053:  
    // Exception count 1
    .try IL_0018 to IL_0027 catch [mscorlib]System.NullReferenceException handler IL_0027 to IL_004c
  } // end of method Program::RunTests

} // end of class Test.Program


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
