// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test Invalid program (evaluation stack is not empty before localloc call

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'verify01_small'{}
.assembly extern xunit.core {}

.namespace Test_verify01_small_il
{
.class public auto ansi beforefieldinit LocallocTest
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       39 (0x27)
    .maxstack  1
    .locals init(int32 V_0)
    .try
    {
      IL_0000:  call       void Test_verify01_small_il.LocallocTest::func()
      IL_0005:  leave.s    IL_0027

    }  // end .try
    catch [mscorlib]System.Security.VerificationException 
    {
      IL_0007:  pop
      IL_0008:  ldstr      "Passed"
      IL_000d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0012:  ldc.i4.s   100
      IL_0014:  stloc.0
      IL_0015:  leave.s    IL_0035

    }  // end handler
    catch [mscorlib]System.InvalidProgramException 
    {
      IL_0017:  pop
      IL_0018:  ldstr      "Passed"
      IL_009d:  call       void [System.Console]System.Console::WriteLine(string)
      IL_0022:  ldc.i4.s   100
      IL_0024:  stloc.0
      IL_0025:  leave.s    IL_0035

    }  // end handler
    IL_0027:  ldstr      "Failed"
    IL_002c:  call       void [System.Console]System.Console::WriteLine(string)
    IL_0031:  ldc.i4.1
    IL_0032:  stloc.0
    IL_0033:  br.s       IL_0035

    IL_0035:  ldloc.0
    IL_0036:  ret
  } // end of method Test_verify01_small_il.LocallocTest::Main

  .method private hidebysig static void  func() cil managed
  {
    // Code size       13 (0xd)
    .maxstack  2
    .locals init(int32* V_0,
             int32* V_1)
    IL_0000:  ldc.i4.4
    IL_0001:  ldc.i4.1
    IL_0002:  mul
    IL_0006:  ldc.i4.4
    IL_0007:  ldc.i4.1
    IL_0008:  mul
    IL_0009:  localloc
    IL_000b:  stloc.1
    IL_0003:  localloc
    IL_0005:  stloc.0
    IL_000c:  ret
  } // end of method Test_verify01_small_il.LocallocTest::func

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Test_verify01_small_il.LocallocTest::.ctor

} // end of class Test_verify01_small_il.LocallocTest


// =============================================================

//*********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file verify01_dynamic.res
}
