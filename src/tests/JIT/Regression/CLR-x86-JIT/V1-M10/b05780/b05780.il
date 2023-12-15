// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b05780
{
}
.class public _simple {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	100

.locals (int32, int32, int32, int32, int64, int64, int64, int64, float32, float32, float32, float32, float64, float64, float64, float64)
 ldc.i4 1 ldc.i4 1 ldc.i4 1 ldc.i4 1 stloc.0 stloc.1 stloc.2 stloc.3 ldc.i8 1 ldc.i8 1 ldc.i8 1 ldc.i8 1
 stloc 4 stloc 5 stloc 6 stloc 7 ldc.r4 1 ldc.r4 1 ldc.r4 1 ldc.r4 1 stloc 8 stloc 9 stloc 10 stloc 11
 ldc.r8 1 ldc.r8 1 ldc.r8 1 ldc.r8 1 stloc 12 stloc 13 stloc 14 stloc 15

        ldc.r8 4
        ldloc 0
     conv.r8
       ldloc 4
        ldloc 12
      stloc 13
        ldc.i8 3
     stloc 4
    stloc 4
   stloc 12
        ldloc 5
        ldloc 12
        ldloc 13
       add
      stloc 14
  conv.r8
 cgt
        ldc.i4.s -116
    conv.r4
        ldc.i4.M1
        ldloc 14
        ldloc 13
       sub
      stloc 14
     conv.r8
   stloc 12
  conv.u4
        ldc.i4.5
      conv.r4
     conv.r8
        ldc.i4.8
      conv.u8
    stloc 5
        ldloc 13
        ldloc 11
        ldloc 10
      stloc 11
     stloc 11
   add
        ldloc 14
   conv.i8
  stloc 4
 pop
stloc.1
ldc.i4 1
sub

ldc.i4 100
add
	ret
}
}
