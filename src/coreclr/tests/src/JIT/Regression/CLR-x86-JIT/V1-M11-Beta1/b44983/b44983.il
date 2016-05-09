// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xeb5027d {}
.class ILGEN_0xeb5027d {
.field static	int64 field_0x0
.field static	int32[] field_0x1
.field static	unsigned int8[] field_0x2

.method static int32 Method_0xe23b6e0(int32 Arg_0x0) {
.maxstack	9

 .locals (int32 local_0x0,unsigned int16 local_0x1,int64 local_0x2,int32[] local_0x3,unsigned int16 local_0x4)
ldc.i4 0xf6a9373a 
stloc local_0x0
ldc.i4 0x3cadc61c 
stloc local_0x1
ldc.i8 0x801bd714fe4f90a2 
stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x3
ldc.i4 0x98d70cc6 
stloc local_0x4
ldc.i8 0x85c225bfab463889 
stsfld int64 ILGEN_0xeb5027d::field_0x0
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0xeb5027d::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0xeb5027d::field_0x2

Start_Orphan_0:
Start_Orphan_1:
 nop
End_Orphan_1:
 ldarg Arg_0x0
 ldc.i4.1
 mul
 conv.ovf.i8.un
Start_Orphan_2:
 ldloc local_0x1
 ldc.i4.1
 blt.un Branch_0x2

 nop
 br Branch_0x3

 Branch_0x2:

 nop
 Branch_0x3:

End_Orphan_2:
Start_Orphan_3:
 nop
End_Orphan_3:
 ldloc local_0x1
 ldc.i4.1
 add
 ldc.i4.4
 conv.ovf.u
 bgt.un Branch_0x4

 ldc.r4 float32(0x106bba2d)
 ldc.r8 float64(0x4c5e23e13dad44a7)
 bgt.un Branch_0x6

 ldsfld int64 ILGEN_0xeb5027d::field_0x0
 br Branch_0x7

 Branch_0x6:

 ldloc local_0x2
 Branch_0x7:

 br Branch_0x5

 Branch_0x4:

 ldc.i8 0xa49bc3766384da55
 conv.u8
 Branch_0x5:

 ble Branch_0x0

Start_Orphan_5:
 nop
End_Orphan_5:
 ldc.i4.3
 ldloc local_0x1
 rem
 brfalse Branch_0x8

 nop
 br Branch_0x9

 Branch_0x8:

 ldarg Arg_0x0
 pop
 Branch_0x9:

 br Branch_0x1

 Branch_0x0:

 ldsfld int32[] ILGEN_0xeb5027d::field_0x1
 stloc local_0x3
 Branch_0x1:

End_Orphan_0:
Start_Orphan_7:
 ldloc local_0x3
 stsfld int32[] ILGEN_0xeb5027d::field_0x1
End_Orphan_7:
 ldc.i4.8
 conv.ovf.u2
 conv.r8
 conv.ovf.u8
 conv.ovf.i2
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i4 0x2f4b1550 

call int32 ILGEN_0xeb5027d::Method_0xe23b6e0(int32 Arg_0x0)
ldc.i4 92
add
 ret
}

}
