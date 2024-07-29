// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b06440b
{
}
.class public _simple {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	100

.locals (int32, int32, int32, float64, int64, int64,
		int64, int64, float32, float32, float32,
		float32, float64, float64, float64, float64)

	ldc.i4.0  stloc.0
	ldc.i4.0  stloc.1
	ldc.i4.0  stloc.2
	ldc.r8 0  stloc.3
	ldc.i8 0  stloc 4
	ldc.i8 0  stloc 5
	ldc.i8 0  stloc 6
	ldc.i8 0  stloc 7
	ldc.r4 0  stloc 8
	ldc.r4 0  stloc 9
	ldc.r4 0  stloc 10
	ldc.r4 0  stloc 11
	ldc.r8 0  stloc 12
	ldc.r8 0  stloc 13
	ldc.r8 0  stloc 14
	ldc.r8 0  stloc 15

	ldc.r8 5
	stloc 3
 
	ldc.r4 -126.0	
	ldloc 3
	conv.r8
	stloc 3 
	
	conv.i1	
	ldloc 4	
	ldloc 4
	ldloc 8
	conv.r8
	stloc 15
	ldloc 12
	stloc 14
	or
	conv.u2
	ldloc 9
	ldloc 14
	stloc 13
	ldloc 13
	conv.r4
	stloc 10
	ldc.i8 -89
	ldloc 15
	ldloc 12
	sub
	conv.u8
	sub
	stloc 6
	pop
	cgt
ldc.i4 100
add
	ret
}
}
