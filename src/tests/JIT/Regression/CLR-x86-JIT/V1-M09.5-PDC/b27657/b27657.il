// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b27657' {}
.class public ILGEN_0x884e5960 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	15
.locals (int32 ecode)

 ldc.i4.1
 stloc ecode
 .try {

 ldc.i4.1

Start_Orphan_9:
 ldc.i4 255
 newarr [mscorlib]System.Int16
 ldc.i4.m1
 conv.ovf.u4
 ldelema [mscorlib]System.Int16
 ldc.i4.m1
 conv.ovf.u2
 stind.i2
End_Orphan_9:

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
