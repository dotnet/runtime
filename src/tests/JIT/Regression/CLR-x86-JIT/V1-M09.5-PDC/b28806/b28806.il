// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b28806' {}
.class public ILGEN_0x2e194c20 {
.field static	int32 field_0x3
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	7
 .try {
 ldc.i4.m1
stsfld int32 ILGEN_0x2e194c20::field_0x3

 ldsfld int32 ILGEN_0x2e194c20::field_0x3
 ldc.i4.m1
 mul.ovf.un
   pop
  leave exit
} catch [mscorlib]System.OverflowException
{
  pop
  leave exit
}
exit:
  ldc.i4.0
ldc.i4 100
add
ret
}
}
