// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x689aad70 {}
.class ILGEN_0x689aad70 {
.field static	int16 field_0x0
.field static	float64 field_0x1
.field static	int64[] field_0x2

.method static int32 Method_0x12a(float32 Arg_0x0, int8 Arg_0x1, unsigned int16 Arg_0x2, int64 Arg_0x3, float64 Arg_0x4, int8 Arg_0x5, int64 Arg_0x6) {
.maxstack	9

 .locals (int64 local_0x0,int8 local_0x1,float64 local_0x2,unsigned int8[] local_0x3)

ldc.i8 0x5bf058fa7c353e39 
stloc local_0x0
ldc.i4 0x6b512eec 
stloc local_0x1
ldc.r8 float64(0xb402513c517b4b) 
stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x3

ldc.i4 0x65904b8f 
stsfld int16 ILGEN_0x689aad70::field_0x0
ldc.r8 float64(0x677e57c668082c72) 
stsfld float64 ILGEN_0x689aad70::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x689aad70::field_0x2


Start_Orphan_0:
Start_Orphan_1:
 nop
End_Orphan_1:
 ldsfld float64 ILGEN_0x689aad70::field_0x1
 ldloc local_0x2
 div
 stsfld float64 ILGEN_0x689aad70::field_0x1
End_Orphan_0:
Start_Orphan_2:
 nop
End_Orphan_2:
 ldarg Arg_0x6
 not
 conv.i1
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.r4 float32(0x2543433) 
ldc.i4 0x79b336b6 
ldc.i4 0x182d1b03 
ldc.i8 0x6090539076355d90 
ldc.r8 float64(0x3ee57b7c21233988) 
ldc.i4 0x76c12b79 
ldc.i8 0x47b658707c2c18e 

call int32 ILGEN_0x689aad70::Method_0x12a(float32 Arg_0x0, int8 Arg_0x1, unsigned int16 Arg_0x2, int64 Arg_0x3, float64 Arg_0x4, int8 Arg_0x5, int64 Arg_0x6)
ldc.i4 13
sub
ret
}

}
