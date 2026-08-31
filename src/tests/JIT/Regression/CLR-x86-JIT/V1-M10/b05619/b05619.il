// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b05619
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
 ldc.i4 1 ldc.i4 1 ldc.i4 1 ldc.i4 1 stloc.0 stloc.1 stloc.2 stloc.3
 ldc.i8 1 ldc.i8 1 ldc.i8 1 ldc.i8 1 stloc 4 stloc 5 stloc 6 stloc 7
 ldc.r4 1 ldc.r4 1 ldc.r4 1 ldc.r4 1 stloc 8 stloc 9 stloc 10 stloc 11
 ldc.r8 1 ldc.r8 1 ldc.r8 1 ldc.r8 1 stloc 12 stloc 13 stloc 14 stloc 15

    ldloc 0
    ldc.i4 0x3acb3e04
      ldloc 7
      conv.u
      add
     ldloc 11
      ldloc 8
     stloc 11
    stloc 8
    ldc.i4 -95
      ldloc.2
     add
      ldloc 7
      conv.i4
       ldloc 7
       ldloc 7
      add
     stloc 7
    stloc.2
   add
    ldloc 9
    conv.i
    conv.r8
     ldloc 12
     ldloc 9
     conv.i8
     conv.r8
    add
   clt
  stloc.1
 pop
ldc.i4 1
sub
pop
ldc.i4 100
ret
}
}
