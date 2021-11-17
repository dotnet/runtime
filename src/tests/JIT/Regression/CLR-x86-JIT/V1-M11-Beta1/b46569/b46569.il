// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x5db0aa0 {}
.assembly extern xunit.core {}
.class ILGEN_0x5db0aa0 {

.method static int32 Method_0x14ca58b7(int64 Arg_0x0, float32 Arg_0x1, float32 Arg_0x2, int16 Arg_0x3, unsigned int16 Arg_0x4, float32 Arg_0x5, int32 Arg_0x6, int32 Arg_0x7, float64 Arg_0x8) {
.maxstack	19

 .locals (unsigned int16 local_0x0,unsigned int16[] local_0x1,unsigned int32 local_0x2,float32 local_0x3)

ldc.i4 0xff96c4b6 
stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x1
ldc.i4 0x49856ea 
stloc local_0x2
ldc.r4 float32(0xecab3c57) 
stloc local_0x3

Start_Orphan_0:
Start_Orphan_1:
 ldc.i8 0x3cfd13f01085ce81
 conv.r8
 neg
 conv.ovf.i8
Start_Orphan_8:
Start_Orphan_9:
 nop
End_Orphan_9:
 ldc.i4.6
 stloc local_0x2
End_Orphan_8:
Start_Orphan_a:
 nop
End_Orphan_a:
 ldc.r8 float64(0x735539f730c9b3d1)
 conv.ovf.i2
Start_Orphan_b:
 nop
End_Orphan_b:
 ldc.i4.2
 ldc.i4 0x6cd4310f
 and
 mul.ovf
 conv.ovf.u8.un
 mul.ovf.un
 conv.ovf.i2.un
Start_Orphan_c:
Start_Orphan_d:
 ldc.r4 float32(0x528314cd)
 ldarg Arg_0x5
 mul conv.r4
 starg Arg_0x8
End_Orphan_d:
Start_Orphan_e:
 nop
End_Orphan_e:
 ldarg Arg_0x0
 conv.ovf.i8
 conv.ovf.u2
 conv.u2
 pop
End_Orphan_c:
Start_Orphan_f:
 nop
End_Orphan_f:
 ldarg Arg_0x0
 ldarg Arg_0x0
 add.ovf.un
 conv.ovf.u1
 ldc.i4.s 87
 ldc.i4.s 86
 add
 conv.i4
 shr.un
Start_Orphan_10:
Start_Orphan_11:
 nop
End_Orphan_11:
 ldc.i4.0
 pop
End_Orphan_10:
Start_Orphan_12:
 ldloc local_0x1
 stloc local_0x1
End_Orphan_12:
 ldloc local_0x2
 conv.ovf.i.un
 mul.ovf.un
Start_Orphan_13:
 ldloca local_0x3
 pop
End_Orphan_13:
Start_Orphan_14:
 ldc.i4.7
 starg Arg_0x7
End_Orphan_14:
 ldarg Arg_0x0
 ldc.i4.5
 shl
 conv.ovf.i1.un
 and
 bgt.un Branch_0x2

Start_Orphan_15:
Start_Orphan_16:
 nop
End_Orphan_16:
 ldc.i4.2
 ldc.i4.4
 cgt
 stloc local_0x0
End_Orphan_15:
 ldc.i4.0
 conv.ovf.i1
 conv.ovf.u1.un
 stloc local_0x2
 br Branch_0x3

 Branch_0x2:

Start_Orphan_17:
 nop
End_Orphan_17:
 ldarg Arg_0x0
 conv.ovf.u
Start_Orphan_18:
 nop
End_Orphan_18:
 ldc.i4.5
 ldarg Arg_0x3
 mul.ovf.un
 div.un
Start_Orphan_19:
 ldc.i4.1
 starg Arg_0x3
End_Orphan_19:
 ldc.i8 0x9dd147c54dba12b8
 conv.ovf.i8.un
 conv.ovf.i
 add.ovf.un
 conv.u8
 starg Arg_0x0
 Branch_0x3:

End_Orphan_1:
 ldarg Arg_0x1
 conv.i1
Start_Orphan_1a:
 ldloca local_0x3
Start_Orphan_1b:
Start_Orphan_1c:
 ldc.i4.m1
 starg Arg_0x4
End_Orphan_1c:
Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldc.i4.m1
 ldloc local_0x0
 rem.un
 conv.r8
 pop
End_Orphan_1b:
Start_Orphan_1e:
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldloc local_0x1
 ldlen
 starg Arg_0x7
End_Orphan_1e:
 ldc.r8 float64(0xf422eead895b14d7)
 conv.i2
 conv.ovf.i1
 conv.r4
 stind.r4
End_Orphan_1a:
Start_Orphan_20:
Start_Orphan_21:
 ldc.i4.4
 starg Arg_0x4
End_Orphan_21:
 ldarg Arg_0x8
 conv.r4
 neg
 ckfinite
 stloc local_0x3
End_Orphan_20:
 ldc.i4.m1
 conv.r4
 ldc.r8 float64(0x62a271d88a8dbac)
 clt.un
 conv.ovf.i4.un
Start_Orphan_22:
 ldloc local_0x1
 ldc.i4.6
 ldelema [mscorlib]System.UInt16
 ldc.i4.2
 ldc.i4 0xe0edd145
 cgt
 stind.i2
End_Orphan_22:
Start_Orphan_23:
 ldloc local_0x0
 starg Arg_0x7
End_Orphan_23:
Start_Orphan_24:
 nop
End_Orphan_24:
 ldarg Arg_0x2
 ldloc local_0x3
 div conv.r4
 ldc.r8 float64(0xadccf7e51feff98c)
 ldc.r8 float64(0xbdf33a9817ef0172)
 rem conv.r4
 sub conv.r4 
 conv.ovf.i
 mul.ovf
 conv.ovf.i8.un
 conv.u4
 xor
Start_Orphan_25:
Start_Orphan_26:
 ldloca local_0x3
 pop
End_Orphan_26:
 ldloc local_0x3
 stloc local_0x3
End_Orphan_25:
Start_Orphan_27:
Start_Orphan_28:
 nop
End_Orphan_28:
 ldc.r4 float32(0x26f1025f)
 ldloc local_0x3
 rem conv.r4
 conv.i2
 starg Arg_0x4
End_Orphan_27:
 ldc.r4 float32(0x78b0d106)
 ckfinite
 conv.u8
Start_Orphan_29:
 ldarga Arg_0x4
 ldc.i4.8
 stind.i2
End_Orphan_29:
Start_Orphan_2a:
 nop
End_Orphan_2a:
 ldc.r8 float64(0xc7bb3f9b490c8e19)
 conv.ovf.u8
 conv.i8
Start_Orphan_2b:
 nop
End_Orphan_2b:
 ldarg Arg_0x0
 conv.i8
 conv.ovf.u1
 conv.ovf.i8.un
 or
 and
Start_Orphan_2e:
 ldarga Arg_0x4
 ldc.i4.4
 stind.i2
End_Orphan_2e:
 conv.ovf.i1
 bgt.un Branch_0x0

 ldc.i4.7
 not
 conv.ovf.u8.un
 conv.i1
 conv.u1
 newarr [mscorlib]System.UInt16
 pop
 br Branch_0x1

 Branch_0x0:

 ldc.i4.7
 starg Arg_0x4
 Branch_0x1:

End_Orphan_0:
Start_Orphan_37:
 ldc.i8 0xb05204d73e690f1e
 conv.ovf.i
 ldarg Arg_0x0
 conv.ovf.i.un
 and
 starg Arg_0x7
End_Orphan_37:
ldc.i4 55
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
	ldc.i8 0x32e05eaaae466f4 
	ldc.r4 float32(0x977cc1dc) 
	ldc.r4 float32(0x1ff2dd0e) 
	ldc.i4 0x605fd326 
	ldc.i4 0xe5abd4a9 
	ldc.r4 float32(0x4fd5952d) 
	ldc.i4 0xd23a95c2 
	ldc.i4 0x1548acbf 
	ldc.r8 float64(0xe743a5a5c5c5be76) 
	
	call int32 ILGEN_0x5db0aa0::Method_0x14ca58b7(int64 Arg_0x0, float32 Arg_0x1, float32 Arg_0x2, int16 Arg_0x3, unsigned int16 Arg_0x4, float32 Arg_0x5, int32 Arg_0x6, int32 Arg_0x7, float64 Arg_0x8)
	pop
	leave X
} catch [mscorlib]System.OverflowException
{
	pop
	leave X
}
X:
ldc.i4 100
 ret
}

}
