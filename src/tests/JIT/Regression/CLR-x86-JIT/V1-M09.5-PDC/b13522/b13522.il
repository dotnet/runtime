// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b13522
{
}
.class public I2 {
 .method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11
.try {
 ldc.i8 0x7000000000000000
 ldc.i8 2
 mul.ovf
 ldc.i8 4
 mul
 conv.i4
 pop
 leave stop
}
catch [mscorlib]System.OverflowException {
 pop
 leave stop
}
stop:
 ldc.i4 100
 ret
}
}
