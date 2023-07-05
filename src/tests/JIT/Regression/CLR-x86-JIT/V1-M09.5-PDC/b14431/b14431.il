// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14431
{
}
.class public ILGEN_622380794 {

.field static	int64 field_0x1
.field static	int64 field_0x5
.field static	float64 field_0x7
.field static	float64 field_0xb
.field static	float32 field_0xe

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

 .locals (int32 local_0x0,int64 local_0x1,float32 local_0x2,int64 local_0x5,float64 local_0x7,int64 local_0x9,float64 local_0xb)
 ldc.i4.0  stloc.0
 ldc.i8 0  stloc.1 
 ldc.r4 0  stloc.2
 ldc.i8 0  stloc.3 
 ldc.r8 0  stloc 4 
 ldc.i8 0  stloc 5 
 ldc.r8 0  stloc 6

 .try {
 ldc.r4 float32(0x3db620e4)
 ldc.i8 0x629529297ea65036
 stloc local_0x5
 ldsflda int64 ILGEN_622380794::field_0x1
 ldind.i8
 pop
 ldloca local_0x2
 ldind.r4
 ldloc local_0x1
 stsfld int64 ILGEN_622380794::field_0x5
 ldsflda float32 ILGEN_622380794::field_0xe
 ldind.r4
 mul
 mul
 pop
 ldloc local_0x0
 conv.r.un
 stsfld float64 ILGEN_622380794::field_0xb
 ldc.i4.1
 conv.r.un
 ldc.r8 float32(0x1491400f)
 ldloc local_0x9
 conv.r.un
 stsfld float64 ILGEN_622380794::field_0x7
 ldloc local_0x7
 conv.r8
 ldloc local_0xb
 add
 sub
 cgt.un
 conv.i1
 pop
 leave out_of_here
 } catch [mscorlib]System.OverflowException {
 pop
 leave out_of_here
 }
 out_of_here:
 ldc.i4 100
 ret
}
}
