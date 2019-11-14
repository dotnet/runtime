// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x195e59cd {}
.class ILGEN_0x195e59cd {
.field static	int16[] field_0x0
.field static	int64 field_0x1
.method static int32 Method_0x24dd30a(float64 Arg_0x0, native int Arg_0x1, int64 Arg_0x2, native int Arg_0x3, native int Arg_0x4, int8 Arg_0x5, unsigned int32 Arg_0x6, int32 Arg_0x7, unsigned int16 Arg_0x8, unsigned int8 Arg_0x9, int8 Arg_0xa, unsigned int64 Arg_0xb, unsigned int32 Arg_0xc, float64 Arg_0xd, native int Arg_0xe) {
.maxstack	13

 .locals (float64[] local_0x0)

ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x0

ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x195e59cd::field_0x0
ldc.i8 0xc0ee4094a223efb7 
stsfld int64 ILGEN_0x195e59cd::field_0x1

 ldarg Arg_0x6
Start_Orphan_d:
 ldc.r8 float64(0x453953d3b5ea8b6c)
 ldarg Arg_0xd
 clt.un
 starg Arg_0x5
End_Orphan_d:
 ldarg Arg_0xb
 ldc.i8 0xe463b184d90b4bfa
 ldsfld int64 ILGEN_0x195e59cd::field_0x1
 ldsfld int64 ILGEN_0x195e59cd::field_0x1
 sub.ovf.un
 mul.ovf
 mul.ovf
 ldarg Arg_0x4
 conv.ovf.i4.un
 ldsfld int64 ILGEN_0x195e59cd::field_0x1
 conv.ovf.i.un
 ceq
 conv.u8
 rem
 conv.i1
 bne.un Branch_0x0

Start_Orphan_14:
Start_Orphan_15:
 ldarga Arg_0x2
 pop
End_Orphan_15:
 ldc.r4 float32(0xd9eb0335)
 conv.u1
 newarr [mscorlib]System.Int16
 pop
End_Orphan_14:
Start_Orphan_1c:
 ldarg Arg_0x5
 conv.ovf.u4
 starg Arg_0x8
End_Orphan_1c:
 ldarg Arg_0x2
 ldsfld int64 ILGEN_0x195e59cd::field_0x1
 ldsfld int64 ILGEN_0x195e59cd::field_0x1
 div.un
 or
 ldsflda int64 ILGEN_0x195e59cd::field_0x1
 ldind.i8
 add.ovf.un
Start_Orphan_20:
 ldsfld int16[] ILGEN_0x195e59cd::field_0x0
 ldc.i4.s 7
 ldelema [mscorlib]System.Int16
 ldarg Arg_0x5
 stind.i2
End_Orphan_20:
 ldc.i8 0xe6775763f78491ca
 ldarg Arg_0xb
 clt.un
 conv.ovf.u4.un
 shr.un
 conv.ovf.u4
 br Branch_0x1

 Branch_0x0:
 ldarg Arg_0x2
 conv.i2
 Branch_0x1:

EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try
{
ldc.r8 float64(0x5aaf746fddeba341) 
ldc.i4 0x890ae118 conv.i
ldc.i8 0x76c5bfda8a5b109b 
ldc.i4 0xdb6f6d75 conv.i
ldc.i4 0x7af1af08 conv.i
ldc.i4 0x9397f207 
ldc.i4 0x9dfbb109 
ldc.i4 0x5269672b 
ldc.i4 0xa4656c69 
ldc.i4 0x264e96af 
ldc.i4 0xc1a345dd 
ldc.i8 0xf37752b0db5a4580 
ldc.i4 0x7f6c3de7 
ldc.r8 float64(0xa3ab419cc1b4d054) 
ldc.i4 0x3daddb16 conv.i

call int32 ILGEN_0x195e59cd::Method_0x24dd30a(float64 Arg_0x0, native int Arg_0x1, int64 Arg_0x2, native int Arg_0x3, native int Arg_0x4, int8 Arg_0x5, unsigned int32 Arg_0x6, int32 Arg_0x7, unsigned int16 Arg_0x8, unsigned int8 Arg_0x9, int8 Arg_0xa, unsigned int64 Arg_0xb, unsigned int32 Arg_0xc, float64 Arg_0xd, native int Arg_0xe)
leave EXIT
}
catch [mscorlib]System.OverflowException
{
leave EXIT
}
EXIT:
ldc.i4 100
ret
}

}
