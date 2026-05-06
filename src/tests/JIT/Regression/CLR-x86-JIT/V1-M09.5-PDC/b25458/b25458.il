// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b25458' {}
.class public ILGEN_0x24781474 {

.field static	float32 field_0x8
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	16
.locals (int32 ecode)
 ldc.i4.1
 stloc ecode
 .try {
 
 ldc.r4 float32(0x63c43222) 
 stsfld float32 ILGEN_0x24781474::field_0x8

 ldc.i4 44
 ldc.i4.3
 conv.ovf.i8.un
 ldc.i8 0x4eb13053384164
 sub.ovf.un
 ldsfld float32 ILGEN_0x24781474::field_0x8
 ldc.r4 44.5
 clt
 shl
 conv.i4
 ceq
 pop

 leave xx
 } catch [mscorlib]System.OverflowException {
 pop
 ldc.i4.0
 stloc ecode
 leave xx
 }
 
xx:
 ldloc ecode
ldc.i4 100
add
 ret
}
}
