// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b05637
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
 ldc.r4 1
 ldc.r4 1
 stloc 9
 stloc 11

     ldc.r8 4.8788207161049695e-295
     ldloc 11
    conv.r8
   ceq
     ldloc 9
     ldc.r4 2.3338088e+033
    stloc 9
     ldc.r4 1.0232444e+031
   stloc 11
     ldc.r4 0.4
    conv.u
   conv.r4
  cgt
 add
ldc.i4 1
sub

ldc.i4 100
add
ret
}
}
