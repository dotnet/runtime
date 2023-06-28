// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b20079' {}
.class public ILGEN_0x38ea91b6 {

.field static	unsigned int32 field_0x2

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	14

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,
int64[] local_0x7,unsigned int16 local_0xb,int32 local_0xe, int32 ecode)

 ldc.i4.1
 stloc ecode
 .try {

ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x7
ldc.i4 0x3fd52d02 
stloc local_0xb
ldc.i4 0x62a1355e 
stloc local_0xe

ldc.i4 0x46ec664a 
stsfld unsigned int32 ILGEN_0x38ea91b6::field_0x2

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
 ldloc local_0xb
 conv.u1
 newarr [mscorlib]System.UInt16
 ldloc local_0x7
 ldlen
 conv.ovf.i1
 ldloc local_0xb
 conv.u8
 conv.ovf.i2
 stelem.i2
End_Orphan_1:
 ldsflda unsigned int32 ILGEN_0x38ea91b6::field_0x2
 ldind.u4
 stloc local_0xe
End_Orphan_0:
 ldc.r8 float64(0x5ebe44322f902706)
 conv.ovf.u
 
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
