// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b53942b {}
.assembly extern xunit.core {}
.class ILGEN_0x198bde61 {

//BEGIN STATIC FIELDS
.field static	float32[] field_0x0
.field static native int[] field_0x1
.field static	unsigned int32[] field_0x2
.field static	unsigned int8[] field_0x3
.field static	int32[] field_0x4
.field static	float32 field_0x5
//END STATIC FIELDS
.method static int32 Method_0xfd4da845(int32 Arg_0x0, float32 Arg_0x1, native int Arg_0x2, float32 Arg_0x3, native int Arg_0x4, float32 Arg_0x5, native int Arg_0x6, unsigned int8 Arg_0x7, float32 Arg_0x8) {
.maxstack	19

 .locals (float32 local_0x0)
//BEGIN INIT LOCALS
ldc.r4 float32(0xcc8f5961) 
stloc local_0x0

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x198bde61::field_0x0
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x198bde61::field_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x198bde61::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x198bde61::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x198bde61::field_0x4
ldc.r4 float32(0x5834ac0) 
stsfld float32 ILGEN_0x198bde61::field_0x5

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
 ldarg Arg_0x2
 brfalse Branch_0x8

 ldc.i8 0x80ec7e714e1681c7
 br Branch_0x9

 Branch_0x8:

 ldc.i8 0x1a63f3c2fff0bad8
 Branch_0x9:

 conv.ovf.i4
 ldc.r8 float64(0x8fbd605f86f4a7ac)
 ldc.i4.3
 conv.r.un
 cgt
 shr.un
 ldc.i4.5
 ble Branch_0x4

 ldc.i4.4
 ldsfld float32 ILGEN_0x198bde61::field_0x5
 call float64 [mscorlib]System.Math::Abs(float64)
 ldarg Arg_0x5
 cgt
 sub
 br Branch_0x5

 Branch_0x4:

Start_Orphan_b:
 ldc.i4.0
 conv.u1
 starg Arg_0x0
End_Orphan_b:
Start_Orphan_c:
 ldarga Arg_0x2
 pop
End_Orphan_c:
 ldc.i4.7
 ldarg Arg_0x6
 div
 ldloc local_0x0
 conv.ovf.u1
 sub.ovf.un
Start_Orphan_d:
 ldsfld native int[] ILGEN_0x198bde61::field_0x1
 stsfld native int[] ILGEN_0x198bde61::field_0x1
End_Orphan_d:
 ldarg Arg_0x6
 ldc.i4.m1
 rem.un
 ldarg Arg_0x4
 ldarg Arg_0x2
 rem.un
 mul
 clt
 Branch_0x5:

 ldarg Arg_0x4
 ldc.i4.0
 cgt
 ldc.i8 0xfb1e0c87d1ea0849
 conv.u2
 mul.ovf.un
 conv.u1
 newarr [mscorlib]System.UInt32
Start_Orphan_11:
 ldc.i8 0x2d9656bd7cdc8ef
 conv.ovf.i4
 pop
End_Orphan_11:
 ldc.i8 0xb9958ebff997f386
 conv.i8
 conv.ovf.i2
 ldelem.u4
 mul
 ldc.i4.3
 beq Branch_0x0

 br Branch_0x1

 Branch_0x0:
 Branch_0x1:

End_Orphan_1:
ldc.i4 55
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0xec7d92b2 
ldc.r4 float32(0x297f9dfd) 
ldc.i4 0xcbe0be20 conv.i
ldc.r4 float32(0x88d5ad85) 
ldc.i4 0x5601ce9c conv.i
ldc.r4 float32(0x2687a40c) 
ldc.i4 0x24466df7 conv.i
ldc.i4 0xb7dee1dc 
ldc.r4 float32(0x773704e8) 

call int32 ILGEN_0x198bde61::Method_0xfd4da845(int32 Arg_0x0, float32 Arg_0x1, native int Arg_0x2, float32 Arg_0x3, native int Arg_0x4, float32 Arg_0x5, native int Arg_0x6, unsigned int8 Arg_0x7, float32 Arg_0x8)
 pop
leave END
} catch [mscorlib]System.OverflowException {
pop
leave END
}
END:
ldc.i4 100
ret

}

}
