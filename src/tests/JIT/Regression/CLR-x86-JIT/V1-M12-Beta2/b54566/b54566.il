// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1ad7535f {}
.assembly extern xunit.core {}
.class ILGEN_0x1ad7535f {

.field static	float64[] field_0x0
.field static	int8 field_0x1
.field static	int8 field_0x2
.field static	unsigned int32[] field_0x3

.method static int32 Method_0x2ddbcfb4(int16 Arg_0x0) {
.maxstack	13

 .locals (unsigned int64 local_0x0,float32[] local_0x1)

ldc.i8 0xdbdd1225b95c5f0 
stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x1

ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x1ad7535f::field_0x0
ldc.i4 0xa52edc2e 
stsfld int8 ILGEN_0x1ad7535f::field_0x1
ldc.i4 0x112ba400 
stsfld int8 ILGEN_0x1ad7535f::field_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x1ad7535f::field_0x3

Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 nop
End_Orphan_2:
 ldsflda int8 ILGEN_0x1ad7535f::field_0x1
Start_Orphan_3:
 nop
End_Orphan_3:
 ldc.r4 float32(0xf8fbf043)
 conv.u4
 stind.i1
End_Orphan_1:
Start_Orphan_4:
 nop
End_Orphan_4:
 ldc.i4.8
 conv.ovf.i.un
 conv.i2
Start_Orphan_5:
 ldsfld unsigned int32[] ILGEN_0x1ad7535f::field_0x3
 ldc.i4.8
 ldelema [mscorlib]System.UInt32
 pop
End_Orphan_5:
Start_Orphan_6:
 nop
End_Orphan_6:
 ldc.i4.0
 ldc.i4.1
 bgt Branch_0x6

 ldc.i8 0xf08627d3e1e09bb9
 br Branch_0x7

 Branch_0x6:

 ldloc local_0x0
 Branch_0x7:

 ldc.r8 float64(0x71f53a85edca14d8)
 conv.i8
 blt.un Branch_0x4

 ldloc local_0x0
 ldloc local_0x0
 bgt Branch_0x8

 ldc.i4 0x2b3e277a
 br Branch_0x9

 Branch_0x8:

 ldc.i4.0
 Branch_0x9:

 br Branch_0x5

 Branch_0x4:

 ldsfld int8 ILGEN_0x1ad7535f::field_0x1
 ldc.i4.5
 rem.un
 Branch_0x5:

 blt.un Branch_0x2

Start_Orphan_7:
 ldloc local_0x1
 pop
End_Orphan_7:
Start_Orphan_8:
 nop
End_Orphan_8:
 ldc.i4.1
 ldc.i4.0
 sub.ovf
 conv.u8
 br Branch_0x3

 Branch_0x2:

 ldc.i8 0xa1309b26b576da9f
 conv.ovf.u8.un
Start_Orphan_9:
 nop
End_Orphan_9:
 ldc.i8 0x656f90278c587a20
 ldloc local_0x0
 mul.ovf.un
 mul.ovf.un
 Branch_0x3:

Start_Orphan_a:
 nop
End_Orphan_a:
 ldc.i4.m1
 conv.ovf.u1.un
 conv.ovf.i.un
 conv.i8
 div.un
Start_Orphan_b:
 ldc.i8 0x398f4149acafeef2
 conv.u8
 stloc local_0x0
End_Orphan_b:
Start_Orphan_c:
 ldsfld int8 ILGEN_0x1ad7535f::field_0x2
 stsfld int8 ILGEN_0x1ad7535f::field_0x2
End_Orphan_c:
 ldc.i4.8
 ldc.i4 0x4a7c518b
 ldc.i4.1
 mul.ovf
 conv.i2
 cgt
 conv.ovf.u8
 bge.un Branch_0x0

Start_Orphan_d:
 ldloca local_0x0
 pop
End_Orphan_d:
Start_Orphan_e:
 ldc.i4.7
 starg Arg_0x0
End_Orphan_e:
 ldc.r4 float32(0xa66a363e)
 conv.ovf.u1
Start_Orphan_f:
 nop
End_Orphan_f:
 ldc.r4 float32(0x24ed6a3c)
 ldc.r4 float32(0x3f7bc282)
 cgt
 and
 conv.u1
 newarr [mscorlib]System.UInt32
Start_Orphan_10:
 nop
End_Orphan_10:
 ldc.i8 0x6267ec3ffe692076
 ldloc local_0x0
 or
 conv.ovf.i1
 ldc.r8 float64(0x545e93fd7b6f14e5)
 conv.ovf.u
Start_Orphan_11:
 nop
End_Orphan_11:
 ldarg Arg_0x0
 conv.ovf.i.un
 and
 cgt
Start_Orphan_12:
 ldc.i4.s -56
 pop
End_Orphan_12:
 ldc.r8 float64(0xb58dd9557166c54b)
 conv.u8
 neg
 conv.ovf.u2
 stelem.i4
 br Branch_0x1

 Branch_0x0:

 ldc.r4 float32(0x7c37f318)
 conv.ovf.u8
 conv.ovf.i4
 conv.u1
 newarr [mscorlib]System.UInt32
 stsfld unsigned int32[] ILGEN_0x1ad7535f::field_0x3
 Branch_0x1:

End_Orphan_0:
 ldc.i4.4
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x9ba12972 

call int32 ILGEN_0x1ad7535f::Method_0x2ddbcfb4(int16 Arg_0x0)
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
