// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b06440c
{
}
.class public _simple {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	100

.locals (int64, float32, float64, float64, float64, float64)

	ldc.i8 0  stloc.0
	ldc.r4 0  stloc.1
	ldc.r8 0  stloc.2
	ldc.r8 0  stloc.3
	ldc.r8 0  stloc 4
	ldc.r8 0  stloc 5

  ldloc 4
  stloc 5

  ldloc 2
  stloc 4

  ldloc 4
  stloc 3

  ldloc 3
  conv.r4
  stloc 1

    ldloc 5
    ldloc 2
   sub
   conv.u8
  
   ldc.i8 -89
  sub
  stloc 0

 ldc.i4 0  //-126
ldc.i4 100
add	
 ret
}
}
