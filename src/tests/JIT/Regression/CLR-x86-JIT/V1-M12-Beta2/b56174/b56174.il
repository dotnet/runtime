// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b56174
{
}
.class public auto ansi TestClass extends ['mscorlib']System.Object
{
.field static value class [mscorlib]System.RuntimeTypeHandle args

.method public hidebysig static int32 Main(class [mscorlib]System.String[]) il managed
{
	.maxstack  8
	.entrypoint
	ldtoken int32
	stsfld value class [mscorlib]System.RuntimeTypeHandle TestClass::args
	ldc.i4 100
	ret
} // end of method

.method public hidebysig specialname rtspecialname instance void .ctor() il managed
{
  // Code size       7 (0x7)
  .maxstack  8
  IL_0000:  ldarg.0
  IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
  IL_0006:  ret
} // end of method 'Test::.ctor'

} // end of class 'Test'
