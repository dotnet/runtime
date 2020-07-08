// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib{}
.assembly decm014{}

.class public auto ansi beforefieldinit MyClass
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .entrypoint
    .locals init (float32)
	ldc.r4     7.9999996e+028
	stloc.0
    .try
    {
	ldloc.0
	conv.r4
	dup
	call void [System.Console]System.Console::WriteLine(float32)

	call       valuetype [mscorlib]System.Decimal [mscorlib]System.Decimal::op_Explicit(float32)
	pop
	ldstr "Fail"
	call void [System.Console]System.Console::WriteLine(string)
	leave.s    IL_0015

    }
    catch [mscorlib]System.OverflowException 
    {
	pop
	ldstr "Pass"
	call void [System.Console]System.Console::WriteLine(string)
	leave.s    IL_0016

    }
    IL_0015:  ldc.i4 1 ret
    IL_0016:  ldc.i4 100 ret
  }
}
