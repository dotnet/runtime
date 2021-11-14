// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib{}
.assembly test_08{}
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}
.module test_08.exe

.class public auto ansi beforefieldinit repro
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main(string[] argv) cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .try
    {
	call void repro::f()
	leave FAIL
    }
    catch [mscorlib]System.ArrayTypeMismatchException
    {
	leave PASS
    }
    PASS:
	ldc.i4 100
	ret
    FAIL:
	ldc.i4 1
	ret
  }

  .method public hidebysig static void f() cil managed
  {
	ldc.i4.1
	newarr     [mscorlib]System.String
	
	ldc.i4.0

	ldc.i4.1
	box        [mscorlib]System.Int32
	
	stelem.ref
	ret
  }

}
