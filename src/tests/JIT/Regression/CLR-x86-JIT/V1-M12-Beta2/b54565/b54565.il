// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x127c25 {}
.class ILGEN_0x127c25 {

.field static	int16 field_0x0
.field static	unsigned int8 field_0x1
.field static	unsigned int32 field_0x2
.field static	unsigned int8 field_0x3
.field static native int[] field_0x4
.field static	unsigned int16[] field_0x5

.method static int32 Method_0xc6986748() {
.maxstack	13

 .locals (float32 local_0x0,int16[] local_0x1)

ldc.r4 float32(0x35f74880) 
stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x1


ldc.i4 0x32485934 
stsfld int16 ILGEN_0x127c25::field_0x0
ldc.i4 0xe2384b0a 
stsfld unsigned int8 ILGEN_0x127c25::field_0x1
ldc.i4 0x6ec69009 
stsfld unsigned int32 ILGEN_0x127c25::field_0x2
ldc.i4 0x2505da94 
stsfld unsigned int8 ILGEN_0x127c25::field_0x3
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x127c25::field_0x4
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x127c25::field_0x5


Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 nop
End_Orphan_2:
 ldc.i4.8
 ldsfld int16 ILGEN_0x127c25::field_0x0
 clt
 conv.u2
Start_Orphan_3:
 nop
End_Orphan_3:
 ldc.i4.1
 brtrue Branch_0x6

 ldc.i8 0xc3dd6af8b8fedfeb
 br Branch_0x7

 Branch_0x6:

 ldc.i8 0xb167b47a1780742f
 Branch_0x7:

 brtrue Branch_0x4

 ldc.i8 0xd691b08a499f0c44
 ldc.i8 0x77b9e53a116dfd36
 clt.un
 br Branch_0x5

 Branch_0x4:

 ldloc local_0x0
 ldloc local_0x0
 beq Branch_0x8

 ldc.i4.5
 br Branch_0x9

 Branch_0x8:

 ldsfld unsigned int32 ILGEN_0x127c25::field_0x2
 Branch_0x9:

 Branch_0x5:

 bgt.un Branch_0x2

 nop
 br Branch_0x3

 Branch_0x2:

 ldc.i4.0
 conv.u1
 newarr [mscorlib]System.IntPtr
Start_Orphan_4:
 nop
End_Orphan_4:
 ldc.i4 0xe4d33ebc
Start_Orphan_5:
 nop
End_Orphan_5:
 ldc.i8 0x1367aa69e4ba1066
 conv.ovf.i2.un
 stelem.i
 Branch_0x3:

End_Orphan_1:
Start_Orphan_6:
 ldsfld native int[] ILGEN_0x127c25::field_0x4
Start_Orphan_7:
 nop
End_Orphan_7:
 ldc.i4.7
 ldc.i4.4
 ldc.i4.5
 add.ovf
 stelem.i
End_Orphan_6:
 ldsfld unsigned int16[] ILGEN_0x127c25::field_0x5
 ldc.i4.3
Start_Orphan_8:
 nop
End_Orphan_8:
 ldc.r4 float32(0x31639d35)
 conv.ovf.i1
 shl
 ldelema [mscorlib]System.UInt16
 ldind.u2
 brtrue Branch_0x0

 ldloca local_0x0
 pop
 br Branch_0x1

 Branch_0x0:

 nop
 Branch_0x1:

End_Orphan_0:
 ldsfld unsigned int8 ILGEN_0x127c25::field_0x1
EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20

call int32 ILGEN_0x127c25::Method_0xc6986748()
pop
ldc.i4 100
 ret
}

}
