// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b04726
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

ldc.r8 1.197221e+011
ldc.r8 1.694962e+011
cgt.un
conv.r4
ldloc.0
conv.r4
sub
ldc.r4 4.294685e+008
ldloc.2
neg
stloc.2
ldc.i4.7
ldloc.3
or
stloc.3
mul
conv.i4
ldc.r8 2.491291e+008
ldc.r8 0x60a44b334f393dd9
ldc.i4.5
not
stloc.1
clt.un
stloc.0
ldc.i4 3865498784
sub

ldc.i4 100
add
ret
}
}
