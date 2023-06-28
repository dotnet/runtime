// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14326
{
}
.class public ILGEN_622380794 {

//BEGIN STATIC FIELDS
.field static	int32 field_0x0
.field static	int64 field_0x1
.field static	float32 field_0x2
.field static	float64 field_0x3
.field static	int32 field_0x4
.field static	int64 field_0x5
.field static	float32 field_0x6
.field static	float64 field_0x7
//END STATIC FIELDS
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

 .locals (int32 local_0x0,int64 local_0x1,float32 local_0x2,float64 local_0x3,int32 local_0x4,int64 local_0x5,float32 local_0x6,float64 local_0x7)
 ldc.i4.0  stloc.0
 ldc.i8 0  stloc.1
 ldc.r4 0  stloc.2
 ldc.r8 0  stloc.3
 ldc.i4.0  stloc 4
 ldc.i8 0  stloc 5
 ldc.r4 0  stloc 6
 ldc.r8 0  stloc 7
 
 ldc.i4.1
 ldsflda int64 ILGEN_622380794::field_0x5
 ldind.i8
 conv.r4
 ldc.i4.1
 conv.r4
 clt.un
 conv.u2
 ldsfld int64 ILGEN_622380794::field_0x1
 conv.u2
 not
 ldsfld int64 ILGEN_622380794::field_0x5
 neg
 conv.u2
 ldc.i4.6
 conv.i8
 conv.u4
 clt
 ldc.i4.6
 ldc.i4.6
 shl
 conv.i
 ldc.i4 0x3be921e9
 ldc.i4.2
 and
 conv.i
 xor
 ldloca local_0x7
 ldind.r8
 conv.r4
 conv.u
 xor
 sub
 neg
 ceq
 ldsfld int64 ILGEN_622380794::field_0x5
 conv.r4
 conv.u
 ldc.r4 float32(0x79756370)
 conv.u
 ldsfld int32 ILGEN_622380794::field_0x0
 conv.u
 clt.un
 ldloc local_0x6
 conv.i1
 ldc.i4 0x468a96c
 mul
 cgt.un
 cgt
 conv.i
 ldsfld int64 ILGEN_622380794::field_0x5
 ldc.i4.6
 shr
 ldsfld int64 ILGEN_622380794::field_0x5
 ldc.i4.0
 shr
 xor
 ldloc local_0x2
 ldloc local_0x6
 ldsfld float32 ILGEN_622380794::field_0x6
 sub
 cgt.un
 shr.un
 conv.i2
 ldc.i4.4
 ldc.i4.s 39
 sub
 ldc.i4.s 3
 ldloc local_0x0
 add
 or
 conv.u
 conv.u
 ceq
 add
 conv.u2
 clt
 and
 shr.un
 ldc.i4.1
 sub.ovf.un
ldc.i4 100
add
 ret
}
}
