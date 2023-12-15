// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME { }
.assembly extern xunit.core {}
.namespace JitTest_compat_enum
{
  .class private auto ansi serializable sealed TestEnum extends [mscorlib]System.Enum
  {
    .field public specialname rtspecialname int32 value__
    .field public static literal value class JitTest_compat_enum.TestEnum v = int32(0x00000000)
    .field public static literal value class JitTest_compat_enum.TestEnum w = int32(0x00000001)
  }
  .class public auto ansi beforefieldinit TestClass extends [mscorlib]System.Object
  {
    .method private hidebysig static value class JitTest_compat_enum.TestEnum callee() il managed
    {
		.maxstack  8
		.locals (value class JitTest_compat_enum.TestEnum V_0)
		ldc.i4.1
		stloc.0
		ldloc.0
		ret
    }
    .method private hidebysig static int32 caller() il managed
    {
      // Code size       10 (0xa)
      .maxstack  8
      tail. call       value class JitTest_compat_enum.TestEnum JitTest_compat_enum.TestClass::callee()
      ret
    } // end of method TestClass::caller

    .method public hidebysig static 
            int32 Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       40 (0x28)
      .maxstack  2
      .locals (int32 V_0)
      IL_0000:  call       int32 JitTest_compat_enum.TestClass::caller()
      IL_0005:  ldc.i4.1
      IL_0006:  bne.un.s   IL_0017

      IL_0008:  ldstr      "passed"
      IL_000d:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0012:  ldc.i4.s   100
      IL_0014:  stloc.0
      IL_0015:  br.s       IL_0026

      IL_0017:  ldstr      "failed"
      IL_001c:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0021:  ldc.i4.s   101
      IL_0023:  stloc.0
      IL_0024:  br.s       IL_0026

      IL_0026:  ldloc.0
      IL_0027:  ret
    } // end of method TestClass::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method TestClass::.ctor

  } // end of class TestClass

} // end of namespace JitTest_compat_enum

//*********** DISASSEMBLY COMPLETE ***********************
