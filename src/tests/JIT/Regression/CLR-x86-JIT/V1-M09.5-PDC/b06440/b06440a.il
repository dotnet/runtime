// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b06440a
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

        ldc.r4 -126
        ldloc 15
       stloc 13
        ldloc 12
       conv.r8
        ldloc 3
     stloc.1
   stloc 12
 conv.i1
        ldloc 6
        ldloc 4
        ldloc 6
      and
     add
        ldloc 4
        ldloc 11
       conv.r8
      stloc 15
        ldloc.0
      conv.u8
     stloc 4
        ldloc 5
        ldloc 12
       stloc 14
    stloc 5
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
        ldloc 14
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
