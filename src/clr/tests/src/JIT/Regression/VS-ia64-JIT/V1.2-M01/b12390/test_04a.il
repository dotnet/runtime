// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib{}
.assembly test_08{}
.module test_08.exe

.class public auto ansi beforefieldinit repro
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32  Main(string[] argv) cil managed
  {
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