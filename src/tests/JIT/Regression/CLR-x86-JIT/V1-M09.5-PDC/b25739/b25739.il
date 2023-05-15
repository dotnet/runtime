// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b25739' {}
.class public ILGEN_0xee612700 {

.field static	unsigned int16 field_0x1
.field static	float32 field_0x8
.field static	unsigned int64[] field_0xd
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	14

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,int16[] local_0x5,int32 ecode)
 
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
 newarr [mscorlib]System.UInt32 
 stloc local_0x2
 ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x3
 ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x5
 
 ldc.i4 0xf6c5a68 
 stsfld unsigned int16 ILGEN_0xee612700::field_0x1
 ldc.r4 float32(0x64e35fd3) 
 stsfld float32 ILGEN_0xee612700::field_0x8
 ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0xee612700::field_0xd

 ldc.i4.6
 conv.i8
 ldsfld float32 ILGEN_0xee612700::field_0x8
 conv.i8

 ldsfld unsigned int64[] ILGEN_0xee612700::field_0xd
 ldsfld unsigned int16 ILGEN_0xee612700::field_0x1
 ldelem.u8
 conv.ovf.i4
 shl
 sub
 conv.ovf.i4
 
 pop
 leave xx
 } catch [mscorlib]System.IndexOutOfRangeException {
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
