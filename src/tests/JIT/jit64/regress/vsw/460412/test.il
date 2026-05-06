// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}





// Metadata version: v2.0.50131
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}
.assembly 'test_460412'
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
// MVID: {1BC10DDA-FF11-4E02-AEEF-C2F0AAC78A25}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x02F10000

// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi beforefieldinit Test_test.Widget`1<T>
       extends [mscorlib]System.Object
{
  .method public hidebysig instance void 
          SyncIt() cil managed synchronized noinlining
  {
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  nop
    IL_0001:  ldstr      "SyncIt"
    IL_0006:  call       void [System.Console]System.Console::WriteLine(string)
    IL_000b:  nop
///////////////////////////////////////////////////////////
ldnull
starg 0
///////////////////////////////////////////////////////////
    IL_0011:  nop
    IL_0012:  ret
  } // end of method Widget`1::SyncIt

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
    // Code size       87 (0x57)
    .maxstack  2
    .locals init (int32 V_0,
             int32 V_1,
             bool V_2)
    IL_0000:  nop
    IL_0001:  ldc.i4.0
    IL_0002:  stloc.0
    IL_0003:  ldloc.0
    IL_0014:  call       int32 Test_test.Program::RunTests<object>()
    IL_0019:  add
    IL_001a:  stloc.0
    IL_001b:  ldloc.0
    IL_002c:  ldc.i4.0
    IL_002d:  ceq
    IL_002f:  stloc.2
    IL_0030:  ldloc.2
    IL_0031:  brtrue.s   IL_0044

    IL_0033:  nop
    IL_0034:  ldstr      "Test FAILED"
    IL_0039:  call       void [System.Console]System.Console::WriteLine(string)
    IL_003e:  nop
    IL_003f:  ldc.i4.s   101
    IL_0041:  stloc.1
    IL_0042:  br.s       IL_0055

    IL_0044:  nop
    IL_0045:  ldstr      "Test SUCCESS"
    IL_004a:  call       void [System.Console]System.Console::WriteLine(string)
    IL_004f:  nop
    IL_0050:  ldc.i4.s   100
    IL_0052:  stloc.1
    IL_0053:  br.s       IL_0055

    IL_0055:  ldloc.1
    IL_0056:  ret
  } // end of method Program::Main

  .method private hidebysig static int32 
          RunTests<T>() cil managed noinlining
  {
    // Code size       189 (0xbd)
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

    .try
    {
      IL_0082:  nop
      IL_0083:  newobj     instance void class Test_test.Widget`1<!!T>::.ctor()
      IL_0088:  call       instance void class Test_test.Widget`1<!!T>::SyncIt()
      IL_008d:  nop
      IL_008e:  nop
      IL_008f:  leave.s    IL_00b6

    }  // end .try
    catch [mscorlib]System.NullReferenceException 
    {
      IL_0091:  stloc.1
      IL_0092:  nop
      IL_0093:  ldstr      "Widget<{0}>.SyncIt() failed"
      IL_0098:  ldtoken    !!T
      IL_009d:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
      IL_00a2:  call       void [System.Console]System.Console::WriteLine(string,
                                                                    object)
      IL_00a7:  nop
      IL_00a8:  ldloc.1
      IL_00a9:  call       void [System.Console]System.Console::WriteLine(object)
      IL_00ae:  nop
      IL_00af:  ldloc.0
      IL_00b0:  ldc.i4.1
      IL_00b1:  add
      IL_00b2:  stloc.0
      IL_00b3:  nop
      IL_00b4:  leave.s    IL_00b6

    }  // end handler
    IL_00b6:  nop
    IL_00b7:  ldloc.0
    IL_00b8:  stloc.2
    IL_00b9:  br.s       IL_00bb

    IL_00bb:  ldloc.2
    IL_00bc:  ret
  } // end of method Program::RunTests

} // end of class Test.Program


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file b441487.res
