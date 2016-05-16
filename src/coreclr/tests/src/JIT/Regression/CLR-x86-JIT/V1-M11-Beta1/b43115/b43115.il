// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1be1aaf4 {}
.class ILGEN_0x1be1aaf4 {
.field static	unsigned int64 field_0x0
.field static	int8[] field_0x1
.field static	int64[] field_0x2
.field static	unsigned int64[] field_0x3
.field static	float32 field_0x4
.field static	unsigned int64[] field_0x5

.method static int32 Method_0xeae47998(int16 Arg_0x0, int16 Arg_0x1, unsigned int8 Arg_0x2, int16 Arg_0x3, int16 Arg_0x4, int64 Arg_0x5, int16 Arg_0x6) {
.maxstack	19

 .locals (unsigned int8[] local_0x0,int16[] local_0x1,float64[] local_0x2)

ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x2


ldc.i8 0x1bbc921e4836c4c3 
stsfld unsigned int64 ILGEN_0x1be1aaf4::field_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x1be1aaf4::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x1be1aaf4::field_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x1be1aaf4::field_0x3
ldc.r4 float32(0xb4de4e6f) 
stsfld float32 ILGEN_0x1be1aaf4::field_0x4
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x1be1aaf4::field_0x5



 ldarg Arg_0x5
Start_Orphan_4b:
Start_Orphan_4c:
 ldarga Arg_0x5
 ldsfld unsigned int64 ILGEN_0x1be1aaf4::field_0x0
 stind.i8
End_Orphan_4c:
 ldc.i4 0x64354a6d
 ldc.i4.7
 mul
 conv.i1
 conv.u1
 newarr [mscorlib]System.Int16
Start_Orphan_4e:
 ldsfld int64[] ILGEN_0x1be1aaf4::field_0x2
 stsfld int64[] ILGEN_0x1be1aaf4::field_0x2
End_Orphan_4e:
Start_Orphan_4f:
 ldsfld unsigned int64[] ILGEN_0x1be1aaf4::field_0x5
 pop
End_Orphan_4f:
 ldsfld unsigned int64 ILGEN_0x1be1aaf4::field_0x0
 conv.i
 neg
Start_Orphan_51:
 nop
End_Orphan_51:
 ldc.i4.7
 ldc.i4.m1
 sub.ovf.un
 conv.ovf.i.un
 cgt
Start_Orphan_52:
 ldc.i4.5
 conv.u1
 newarr [mscorlib]System.Int16
 stloc local_0x1
End_Orphan_52:
 ldc.i4 0x7b8b9cd3
 ldc.i4.0
 sub.ovf.un
Start_Orphan_53:
 nop
End_Orphan_53:
 ldc.i4 0x7d31e299
 ldc.i4.m1
 ceq
 div
 conv.i4
 stelem.i2
End_Orphan_4b:
Start_Orphan_54:
Start_Orphan_55:
 ldsfld unsigned int64[] ILGEN_0x1be1aaf4::field_0x3
 stsfld unsigned int64[] ILGEN_0x1be1aaf4::field_0x5
End_Orphan_55:
Start_Orphan_56:
 nop
End_Orphan_56:
 ldsfld int8[] ILGEN_0x1be1aaf4::field_0x1
Start_Orphan_57:
 nop
End_Orphan_57:
 ldc.r4 float32(0x2e791bd2)
 conv.i2
 ldelema [mscorlib]System.SByte
 pop
End_Orphan_54:
Start_Orphan_58:
 nop
End_Orphan_58:
Start_Orphan_59:
 nop
End_Orphan_59:
 ldc.i4.5
 ldc.i4.0
 add
 ldc.i4.8
 conv.u
 add.ovf.un
 conv.i8
Start_Orphan_5a:
 nop
End_Orphan_5a:
Start_Orphan_5b:
 nop
End_Orphan_5b:
Start_Orphan_5c:
 ldsfld unsigned int64[] ILGEN_0x1be1aaf4::field_0x5
 stsfld unsigned int64[] ILGEN_0x1be1aaf4::field_0x3
End_Orphan_5c:
 ldsfld unsigned int64 ILGEN_0x1be1aaf4::field_0x0
 ldc.i8 0x36ff78576c793f7
 sub
 ldarg Arg_0x5
 conv.ovf.i4
 shr
 ldloc local_0x2
 ldc.i4.1
 ldelema [mscorlib]System.Double
 ldind.r8
 conv.ovf.i4
 shr.un
 add.ovf.un
Start_Orphan_5d:
 nop
End_Orphan_5d:
 ldc.i4.8
 ldc.i4.s -71
 cgt
 ldc.i4 0xa40df013
 ldc.i4.3
 add.ovf
 mul
 conv.ovf.i8
 ldsfld unsigned int64[] ILGEN_0x1be1aaf4::field_0x5
 ldc.i4.1
 ldelem.u8
 conv.ovf.u8
 ldsfld unsigned int64 ILGEN_0x1be1aaf4::field_0x0
 conv.r4
 conv.ovf.i8
 sub
 sub.ovf
 sub
 mul
 conv.ovf.u
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x4ca3cb52 
ldc.i4 0xcb252cf8 
ldc.i4 0x19a5f165 
ldc.i4 0xe482d3e0 
ldc.i4 0xe922755d 
ldc.i8 0xa19bbf8f6bf7c449 
ldc.i4 0x3e863b6d 

call int32 ILGEN_0x1be1aaf4::Method_0xeae47998(int16 Arg_0x0, int16 Arg_0x1, unsigned int8 Arg_0x2, int16 Arg_0x3, int16 Arg_0x4, int64 Arg_0x5, int16 Arg_0x6)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100
 ret
}

}
