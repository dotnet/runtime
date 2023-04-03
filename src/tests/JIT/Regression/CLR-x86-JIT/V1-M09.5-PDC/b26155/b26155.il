// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b26155' {}
.class public ILGEN_0xf7ec06dc {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10

 .locals (int16 local_0xd,int64 local_0xf,int32 ecode)

 ldc.i4.1
 stloc ecode
 .try {

 ldc.i4 0x7
 stloc local_0xd
 ldc.i8 0x5 
 stloc local_0xf

 ldc.i8 0xffffffffffff0
 neg
 conv.ovf.u8
 ldloc local_0xd
 conv.ovf.u8
 ldc.i8 0x5
 ldloc local_0xf
 mul
 ldc.r8 4.4
 conv.ovf.u8
 add.ovf.un
 add
 add.ovf
 conv.i2
 
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
