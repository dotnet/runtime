// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b34951' {}
.assembly extern xunit.core {}
.class ILGEN_0xe8c74ae0 {


.field static	unsigned int64 field_0x0
.field static	float64 field_0x1
.field static	unsigned int8 field_0x2
.field static	float32[] field_0x3
.field static	int64[] field_0x4
.field static	float64[] field_0x5
.field static	float32 field_0x6
.field static	unsigned int64[] field_0x7

.method static int32 Method_0x5a2b(int32 Arg_0x0, unsigned int32 Arg_0x1, float32 Arg_0x2, unsigned int32 Arg_0x3, int16 Arg_0x4, unsigned int8 Arg_0x5, int16 Arg_0x6, int16 Arg_0x7, float64 Arg_0x8) {
.maxstack	13

 .locals (unsigned int64 local_0x0,int8[] local_0x1,int32[] local_0x2,unsigned int8 local_0x3,float32[] local_0x4,unsigned int64 local_0x5)

 ldc.i8 0x5314582f74556c9e 
stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x2
ldc.i4 0x67ac68bc 
stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x4
ldc.i8 0x5e0ec126677253 
stloc local_0x5

ldc.i8 0x534231ac31aa1630 
stsfld unsigned int64 ILGEN_0xe8c74ae0::field_0x0
ldc.r8 float64(0x474436722b667edd) 
stsfld float64 ILGEN_0xe8c74ae0::field_0x1
ldc.i4 0x382a131a 
stsfld unsigned int8 ILGEN_0xe8c74ae0::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0xe8c74ae0::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0xe8c74ae0::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0xe8c74ae0::field_0x5
ldc.r4 float32(0x299664f4) 
stsfld float32 ILGEN_0xe8c74ae0::field_0x6
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0xe8c74ae0::field_0x7


Start_Orphan_0:
Start_Orphan_1:
 nop
End_Orphan_1:
 ldc.r8 float64(0x4a8e452e50d25de3)
 conv.r4
 conv.r4
 conv.ovf.u4
 conv.u1
 newarr [mscorlib]System.Int32
Start_Orphan_2:
Start_Orphan_3:
 ldsfld float64 ILGEN_0xe8c74ae0::field_0x1
 pop
End_Orphan_3:
 ldsfld float64 ILGEN_0xe8c74ae0::field_0x1
Start_Orphan_5:
 nop
End_Orphan_5:
 ldarg Arg_0x8
 ldarg Arg_0x8
 sub
 mul
 stsfld float64 ILGEN_0xe8c74ae0::field_0x1
End_Orphan_2:
Start_Orphan_6:
Start_Orphan_7:
 nop
End_Orphan_7:
 ldc.i4 0x60532d0d
 ldc.i4.6
 mul.ovf.un
 starg Arg_0x1
End_Orphan_6:
Start_Orphan_8:
 ldc.i4.3
 stsfld unsigned int8 ILGEN_0xe8c74ae0::field_0x2
End_Orphan_8:
 ldsfld float64 ILGEN_0xe8c74ae0::field_0x1
 conv.u8
 conv.ovf.u2
Start_Orphan_9:
Start_Orphan_a:
 ldsfld float32 ILGEN_0xe8c74ae0::field_0x6
 stsfld float32 ILGEN_0xe8c74ae0::field_0x6
End_Orphan_a:
 ldc.i4.m1
 conv.u1
 newarr [mscorlib]System.UInt64
 ldarg Arg_0x0
 conv.u1
 ldelema [mscorlib]System.UInt64
 pop
End_Orphan_9:
Start_Orphan_f:
 ldsflda unsigned int64 ILGEN_0xe8c74ae0::field_0x0
 ldc.i8 0x2435350358571d2e
 stind.i8
End_Orphan_f:
 ldarg Arg_0x2
 conv.ovf.i1
 conv.ovf.i8
Start_Orphan_11:
 ldsfld float32[] ILGEN_0xe8c74ae0::field_0x3
 stloc local_0x4
End_Orphan_11:
 ldarg Arg_0x4
 conv.i8
 conv.ovf.u8.un
 xor
 conv.i2
 stelem.i4
End_Orphan_0:
Start_Orphan_12:
 ldloc local_0x2
 ldarg Arg_0x0
 ldelema [mscorlib]System.Int32
 ldc.i4.5
 stind.i4
End_Orphan_12:
 ldloc local_0x5
 ldsfld unsigned int64 ILGEN_0xe8c74ae0::field_0x0
 sub.ovf
 conv.i4
 conv.r.un
 conv.u8
 conv.ovf.u8
 conv.ovf.u4
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x4f64e59 
ldc.i4 0x31912860 
ldc.r4 float32(0x48ab2024) 
ldc.i4 0xc2a7a41 
ldc.i4 0x402629de 
ldc.i4 0x752e3214 
ldc.i4 0x69a67ba3 
ldc.i4 0x6304964 
ldc.r8 float64(0x2ca66b3a25f090c) 

call int32 ILGEN_0xe8c74ae0::Method_0x5a2b(int32 Arg_0x0, unsigned int32 Arg_0x1, float32 Arg_0x2, unsigned int32 Arg_0x3, int16 Arg_0x4, unsigned int8 Arg_0x5, int16 Arg_0x6, int16 Arg_0x7, float64 Arg_0x8)
pop

  leave a1
} catch [mscorlib]System.OverflowException {
  pop
  leave a1
}
a1:
ldc.i4 100
ret
}

}
