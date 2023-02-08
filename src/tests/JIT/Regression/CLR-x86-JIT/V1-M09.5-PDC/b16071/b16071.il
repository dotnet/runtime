// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b16071' {}
.class public ILGEN_622380794 {

.field static	unsigned int8 field_0x0
.field static	int16 field_0x3
.field static	unsigned int8 field_0x4
.field static	unsigned int16 field_0x5
.field static	unsigned int8 field_0x20
.field static	unsigned int16 field_0x21
.field static	int8 field_0x22
.field static	unsigned int8 field_0x24

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	22

 .locals (unsigned int8 local_0x10,int8 local_0x12,int8 local_0x22,unsigned int16 local_0x29)

	ldc.i4.0 stloc.0
	ldc.i4.0 stloc.1
	ldc.i4.0 stloc.2
	ldc.i4.0 stloc.3

ldc.i4 0x6237d47 
stloc local_0x29

ldc.i4 0x77a71a3a 
stsfld unsigned int8 ILGEN_622380794::field_0x0
ldc.i4 0x6cec65ab 
stsfld int16 ILGEN_622380794::field_0x3
ldc.i4 0x72264de8 
stsfld unsigned int8 ILGEN_622380794::field_0x20
ldc.i4 0x5a3924bb 
stsfld unsigned int16 ILGEN_622380794::field_0x21
ldc.i4 0x6e3c107a 
stsfld int8 ILGEN_622380794::field_0x22
ldc.i4 0x7a8d5474 
stsfld unsigned int8 ILGEN_622380794::field_0x24

 ldloc local_0x29
 ldsfld unsigned int16 ILGEN_622380794::field_0x21
 sub
 ldc.r8 float64(0x28fb2093)
 ldsflda int8 ILGEN_622380794::field_0x22
 ldc.i4 8
 stind.i1
 conv.i4
 sub
 stsfld unsigned int8 ILGEN_622380794::field_0x0
 ldsflda unsigned int8 ILGEN_622380794::field_0x24
 ldind.u1
 conv.u8
 ldc.i8 0x4ae6314551af63a7
 ldloca local_0x12
 ldind.i1
 conv.u8
 add
 ldc.i4.8
 ldsfld unsigned int8 ILGEN_622380794::field_0x20
 conv.i
 ldloca local_0x22
 ldind.i1
 ldsfld unsigned int8 ILGEN_622380794::field_0x4
 mul
 xor
 ceq
 shl
 ldsfld unsigned int16 ILGEN_622380794::field_0x5
 stloc local_0x10
 conv.i4
 shr.un
 not
 conv.i4
 
 ldc.i4 4294967295
 sub
ldc.i4 100
add
ret
}
}
