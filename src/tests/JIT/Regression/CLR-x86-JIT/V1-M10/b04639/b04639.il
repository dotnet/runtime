// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b04639
{
}
.class public _simple {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	100

.locals (int32, int32, int32, int32)
ldc.i4 1
ldc.i4 1
ldc.i4 1
ldc.i4 1
stloc.0
stloc.1
stloc.2
stloc.3

 ldc.i4 45
 ldc.i4 34001
 ldloc.3
 conv.u8
 ldloc.2
 conv.i8
 add
 ldloc.0
 ldc.i4.s 9
 stloc.0
 conv.u1
 ldloc.3
 stloc.2
 conv.u8
 add
 ldc.i8 0xb8231398c4fc06c
 ldc.i8 0x46dda8c4674e147f
 ldc.i8 14002
 conv.i8
 add
 conv.i8
 add
 ldc.r8 3.400000e+001
 conv.r4
 ldc.r4 1.650000e+002
 add
 conv.i8
 add
 add
 conv.u1
 add
 conv.u2
 stloc.0
 ldc.i4 45
sub

ldc.i4 100
add
 ret
}
}
