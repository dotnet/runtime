// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1c24c9c8 {}
.class ILGEN_0x1c24c9c8 {
.field static	int64 field_0x0
.field static	int8[] field_0x1
.field static	unsigned int64 field_0x2
.field static	int16[] field_0x3
.field static	unsigned int8 field_0x4

.method static int32 Method_0x70ae6ea7(int32 Arg_0x0, native int Arg_0x1, int8 Arg_0x2, int32 Arg_0x3, unsigned int32 Arg_0x4) {
.maxstack	9

 .locals (native int local_0x0,unsigned int64 local_0x1,int8[] local_0x2,int8 local_0x3,unsigned int32 local_0x4)

ldc.i4 0x2e1a518 
stloc local_0x0
ldc.i8 0xfc2dd05216826bf8 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x2
ldc.i4 0x658a47a2 
stloc local_0x3
ldc.i4 0x2339f343 
stloc local_0x4

ldc.i8 0x4e5faa58b5e8255f 
stsfld int64 ILGEN_0x1c24c9c8::field_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x1c24c9c8::field_0x1
ldc.i8 0x54f8e63d31c6c6d4 
stsfld unsigned int64 ILGEN_0x1c24c9c8::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x1c24c9c8::field_0x3
ldc.i4 0x97d21ded 
stsfld unsigned int8 ILGEN_0x1c24c9c8::field_0x4

//END INIT OF STATIC FIELDS

 BLOCK_1:
 ldc.i4.s -11
 ldarg Arg_0x1
 bge Branch_0x2

 ldsfld unsigned int64 ILGEN_0x1c24c9c8::field_0x2
 br Branch_0x3

 Branch_0x2:

 ldsfld unsigned int64 ILGEN_0x1c24c9c8::field_0x2
 Branch_0x3:

 conv.r4
 call float64 [mscorlib]System.Math::Round(float64)
Start_Orphan_0:
 ldc.i4.3
 ldloc local_0x0
 mul.ovf
 ldloc local_0x0
 brfalse Branch_0x6

 ldarg Arg_0x1
 br Branch_0x7

 Branch_0x6:

 ldarg Arg_0x1
 Branch_0x7:

 bgt.un Branch_0x4

 ldc.i4.1
 ldarg Arg_0x2
 ble.un Branch_0x8

 nop
 br Branch_0x9

 Branch_0x8:

 nop
 Branch_0x9:

 br Branch_0x5

 Branch_0x4:

 ldloc local_0x2
 pop
 Branch_0x5:

End_Orphan_0:
Start_Orphan_1:
 ldsfld int16[] ILGEN_0x1c24c9c8::field_0x3
 ldloc local_0x3
 ldc.i4.m1
 stelem.i2
End_Orphan_1:
 ldc.i8 0x29be683685f2648c
 not
 ldloca local_0x1
 ldind.u8
 div.un
 conv.r.un
 blt Branch_0x0

Start_Orphan_2:
 ldsflda int64 ILGEN_0x1c24c9c8::field_0x0
 ldsfld int64 ILGEN_0x1c24c9c8::field_0x0
 stind.i8
End_Orphan_2:
 ldloca local_0x4
 ldind.u4
 br Branch_0x1

 Branch_0x0:

Start_Orphan_3:
 ldsfld unsigned int64 ILGEN_0x1c24c9c8::field_0x2
 stsfld int64 ILGEN_0x1c24c9c8::field_0x0
End_Orphan_3:
 ldsfld unsigned int64 ILGEN_0x1c24c9c8::field_0x2
 ldsfld unsigned int64 ILGEN_0x1c24c9c8::field_0x2
 add.ovf.un
 conv.i1
Start_Orphan_4:
 ldc.i8 0xdf6376e9695ff2cf
 stloc local_0x1
End_Orphan_4:
 ldsfld int64 ILGEN_0x1c24c9c8::field_0x0
 ldc.i8 0x2aeccd1fa1249d6c
 xor
 conv.ovf.u2.un
 cgt.un
 Branch_0x1:

EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x856031c5 
ldc.i4 0xbf82b355 conv.i
ldc.i4 0xf888b7f0 
ldc.i4 0x5d49c722 
ldc.i4 0x72ae7adf 

call int32 ILGEN_0x1c24c9c8::Method_0x70ae6ea7(int32 Arg_0x0, native int Arg_0x1, int8 Arg_0x2, int32 Arg_0x3, unsigned int32 Arg_0x4)
 pop
leave END
} catch [mscorlib]System.IndexOutOfRangeException {
pop
leave END
}
END:
ldc.i4 100
ret

}

}
