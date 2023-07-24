// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly legacy library ldfldunboxedvt {}
.class public sequential ansi sealed beforefieldinit VT
       extends [mscorlib]System.ValueType
{
  .field public int32 a
} // end of class VT

.class public auto ansi beforefieldinit ldfldunboxedvt
       extends [mscorlib]System.Object
{
  .method public hidebysig static void f() cil managed
  {
    .maxstack  5
    .locals init (valuetype VT V_0)
    IL_0000:  ldloca.s   V_0
    IL_0002:  initobj    VT
    IL_0008:  //ldloca.s   V_0
		ldloc.s   V_0
    IL_000a:  ldfld      int32 VT::a
    IL_000f:  call       void [System.Console]System.Console::WriteLine(int32)
	ret
  }

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack 5
    .try
    {
	call void ldfldunboxedvt::f()
	leave.s PASS
    }
    catch [mscorlib]System.NullReferenceException
    {
	ldstr "FAIL - Caught an unexpected NullReferenceException"
	call void [System.Console]System.Console::WriteLine(string)
	leave.s FAIL

    }
    FAIL:
	ldc.i4.s   1
	ret
    PASS:
	ldstr "PASS"
	call void [System.Console]System.Console::WriteLine(string)
	ldc.i4.s   100
	ret
  } // end of method ldfldunboxedvt::Main

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  }

}
