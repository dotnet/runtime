// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b25468' {}
 .class public ILGEN_0x145288f8 {

 .method public static int32 main() {
 .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
     01 00 00 00
 )
 .entrypoint
 .maxstack	14

 .locals (int32[] local_0x6,float64[] local_0x9,unsigned int16 local_0xb,int64 local_0xf,int32 ecode)
 ldc.i4.1
 stloc ecode
 .try {

 ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x6
 ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x9
 ldc.i4 0x56734eb0 
 stloc local_0xb
 ldc.i8 0x11e969bd7a1e719d 
 stloc local_0xf

 ldloca local_0xb
 ldloc local_0x6
 ldlen
 stind.i2

 ldloc local_0xf
 conv.ovf.i4
 pop

 ldc.i4 444
 ldloc local_0x6
 ldlen
 conv.ovf.i8
 conv.u4

 clt
 
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
