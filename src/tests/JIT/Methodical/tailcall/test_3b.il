// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly test_3b { }
.assembly extern xunit.core {}
.namespace JitTest_test_3b
{
  .class private value sealed ansi beforefieldinit ValueClass
  		extends [mscorlib]System.ValueType
  {
  		.field public float64 m_fld
  }
  .class public auto ansi beforefieldinit TestClass
         extends [mscorlib]System.Object
  {
    .method private hidebysig  static 
            int32 TestFunc2(value class JitTest_test_3b.ValueClass) il managed
    {
      // Code size       10 (0xa)
      .maxstack  8
      .locals init (int32 V_0)
      sizeof value class JitTest_test_3b.ValueClass
      localloc
      ldarg.0
      stobj value class JitTest_test_3b.ValueClass
      ldc.i4.0
      ret
    } // end of method TestClass::TestFunc2

    .method private hidebysig  static 
            int32 TestFunc1() il managed
    {
      // Code size       22 (0x16)
      .maxstack  8
      sizeof value class JitTest_test_3b.ValueClass
      localloc
      dup
      initobj value class JitTest_test_3b.ValueClass
      ldobj value class JitTest_test_3b.ValueClass
      tail.
      call int32 JitTest_test_3b.TestClass::TestFunc2(value class JitTest_test_3b.ValueClass)
      ret
    } // end of method TestClass::TestFunc1

    .method public hidebysig  static 
            int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       38 (0x26)
      .maxstack  1
      .locals (int32 V_0)
      IL_0000:  call       int32 JitTest_test_3b.TestClass::TestFunc1()
      IL_0005:  brfalse.s  IL_0015

      IL_0007:  ldstr      "failed"
      IL_000c:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0011:  ldc.i4.1
      IL_0012:  stloc.0
      IL_0013:  br.s       IL_0024

      IL_0015:  ldstr      "passed"
      IL_001a:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_001f:  ldc.i4.s   100
      IL_0021:  stloc.0
      IL_0022:  br.s       IL_0024

      IL_0024:  ldloc.0
      IL_0025:  ret
    } // end of method TestClass::Main

    .method public hidebysig  specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method TestClass::.ctor

  } // end of class TestClass

} 

//*********** DISASSEMBLY COMPLETE ***********************
