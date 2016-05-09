// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xa83217d {}
.class ILGEN_0xa83217d {

.field static	int8 field_0x0
.field static	float32[] field_0x1
.field static	int16 field_0x2

.method static int32 Method_0xf3555db(int64 Arg_0x0, unsigned int64 Arg_0x1, int32 Arg_0x2, unsigned int32 Arg_0x3, int16 Arg_0x4, int8 Arg_0x5) {
.maxstack	9

 .locals (float32[] local_0x0,unsigned int16[] local_0x1,int16[] local_0x2)

ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x2

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x237e1607 
stsfld int8 ILGEN_0xa83217d::field_0x0
ldc.i4 0x143de3ff 
stsfld int16 ILGEN_0xa83217d::field_0x2

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldc.i4.s 50
 ldc.i4.2
 bgt Branch_0x2

 ldc.i4 0x168b565b
 br Branch_0x3

 Branch_0x2:

 ldc.i4.6
 Branch_0x3:

 conv.r4
 ldc.i4.7
 ldarg Arg_0x4
 bge.un Branch_0x6

 ldarg Arg_0x0
 br Branch_0x7

 Branch_0x6:

 ldc.i8 0x103eac7c377a62bb
 Branch_0x7:

 ldarg Arg_0x1
 conv.u8
 blt.un Branch_0x4

 ldarg Arg_0x1
 ldarg Arg_0x1
 bne.un Branch_0x8

 ldc.r8 float64(0xdf67fdd7b2623205)
 br Branch_0x9

 Branch_0x8:

 ldc.r8 float64(0xdd6f7c7b34492d6)
 Branch_0x9:

 br Branch_0x5

 Branch_0x4:

 ldc.r4 float32(0xbaa07460)
 Branch_0x5:

 ble.un Branch_0x0

 ldloc local_0x1
 ldc.i4.s -37
 ldelema [mscorlib]System.UInt16
 pop
 br Branch_0x1

 Branch_0x0:
Branch_0x1:

End_Orphan_0:
 ldarg Arg_0x1
 conv.ovf.i4
 ldc.i4.5
 rem.un
 ldc.i4.s 118
 conv.u1
 newarr [mscorlib]System.UInt16
 ldc.i4.8
 ldsfld int16 ILGEN_0xa83217d::field_0x2
 rem.un
 ldelem.u2
 div.un
Start_Orphan_7:
 ldloc local_0x2
 stloc local_0x2
End_Orphan_7:
Start_Orphan_8:
 ldc.i4.7
 stsfld int8 ILGEN_0xa83217d::field_0x0
End_Orphan_8:
 ldc.i4.m1
 ldc.i4.8
 ceq
 ldc.i4.1
 neg
 xor
 ldc.r8 float64(0x46e949b327cfb2bd)
 ldc.r8 float64(0x7019c1998b85fa9e)
 mul 
 conv.ovf.u4
 shr.un
 ceq
EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0xf43932d957d41f7d 
ldc.i8 0x8d40345a5b19eb4e 
ldc.i4 0xdb586905 
ldc.i4 0x43ee1538 
ldc.i4 0xe5a5ad0d 
ldc.i4 0x6246a8a5 

call int32 ILGEN_0xa83217d::Method_0xf3555db(int64 Arg_0x0, unsigned int64 Arg_0x1, int32 Arg_0x2, unsigned int32 Arg_0x3, int16 Arg_0x4, int8 Arg_0x5)
pop
leave EEE
} catch [mscorlib]System.IndexOutOfRangeException {
pop
leave EEE
}
EEE:
ldc.i4 100
 ret
}

}
