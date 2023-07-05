// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b18852' {}
.class public ILGEN_0x38e9694e {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	18

 .locals (unsigned int8 local_0xa,float32 local_0x10, int32 ecode)
 ldc.i4.1
 stloc ecode
 .try {

ldc.i4 0x49881c6f 
stloc local_0xa
ldc.r4 float32(0x26b1a70) 
stloc local_0x10

 ldc.i4.3
 ldc.i4.8
 neg
 mul.ovf
 conv.ovf.u2.un
 ldc.i8 44
 conv.i
 ldloc local_0xa
 not
 ldc.i4 3
 div
 add
 div
 conv.r4
 stloc local_0x10
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


