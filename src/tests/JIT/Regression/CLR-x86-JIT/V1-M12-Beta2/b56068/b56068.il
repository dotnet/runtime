// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b56068' {}
.assembly extern xunit.core {}
.class ILGEN_0x14dd78f4 {
.field static	unsigned int8 field_0x0
.field static	unsigned int64 field_0x1
.method static int32 Method_0xb96ba46d(int64 Arg_0x0, int16 Arg_0x1) {
.maxstack	19

 .locals (unsigned int32 local_0x0,native int local_0x1)

ldc.i4 0xea93af2c 
stloc local_0x0
ldc.i4 0x3140352a 
stloc local_0x1

ldc.i4 0x853ae3e7 
stsfld unsigned int8 ILGEN_0x14dd78f4::field_0x0
ldc.i8 0x5aee996118761357 
stsfld unsigned int64 ILGEN_0x14dd78f4::field_0x1

Start_Orphan_0:
Start_Orphan_1:
 ldsflda unsigned int64 ILGEN_0x14dd78f4::field_0x1
 ldind.u8
 conv.ovf.i
Start_Orphan_6:
 ldarga Arg_0x1
 ldc.i4.m1
 stind.i2
End_Orphan_6:
 ldloc local_0x1
 ldloc local_0x1
 add.ovf
 ldc.i4.s 52
 ldc.i4.7
 mul
 cgt
 conv.ovf.u.un
 ble.un Branch_0x2

 ldc.i8 0x2b3ca14e25508278
 ldc.i8 0x51ae28c924842e1c
 sub.ovf
 conv.ovf.i4
 pop
 br Branch_0x3

 Branch_0x2:

 ldloca local_0x0
 ldloc local_0x1
 conv.i2
 ldloc local_0x0
 ldc.i4.3
 clt.un
 mul.ovf
 stind.i4
 Branch_0x3:

End_Orphan_1:
Start_Orphan_d:
Start_Orphan_e:
 nop
End_Orphan_e:
 ldarga Arg_0x1
 ldc.i4.2
 not
 stind.i2
End_Orphan_d:
Start_Orphan_f:
 ldc.i4.5
 stsfld unsigned int8 ILGEN_0x14dd78f4::field_0x0
End_Orphan_f:
Start_Orphan_10:
 nop
End_Orphan_10:
 ldc.i4.5
 ldc.i4.1
 add
Start_Orphan_11:
 nop
End_Orphan_11:
 ldc.i4.6
 ldc.i4.3
 mul.ovf
 or
 conv.ovf.u8.un
 conv.u8
Start_Orphan_12:
Start_Orphan_13:
 nop
End_Orphan_13:
 ldsflda unsigned int64 ILGEN_0x14dd78f4::field_0x1
 pop
End_Orphan_12:
Start_Orphan_14:
 nop
End_Orphan_14:
 ldc.r4 float32(0xa72a0d70)
 ldc.r8 float64(0xb00dd26c32913d01)
 ceq
 conv.ovf.i8.un
 conv.ovf.i2
 neg
 shl
Start_Orphan_15:
 nop
End_Orphan_15:
 ldarg Arg_0x0
 conv.ovf.i8.un
 sub.ovf
 conv.ovf.i1.un
Start_Orphan_16:
 ldsflda unsigned int64 ILGEN_0x14dd78f4::field_0x1
Start_Orphan_17:
 nop
End_Orphan_17:
 ldc.i4.5
 conv.r.un
Start_Orphan_18:
 nop
End_Orphan_18:
 ldarg Arg_0x0
 conv.r8
 add 
 conv.ovf.u2
 conv.ovf.i8
 stind.i8
End_Orphan_16:
 ldsfld unsigned int8 ILGEN_0x14dd78f4::field_0x0
 conv.ovf.u2.un
Start_Orphan_19:
Start_Orphan_1a:
 ldc.i4.7
 starg Arg_0x1
End_Orphan_1a:
End_Orphan_19:
Start_Orphan_1f:
 ldsflda unsigned int64 ILGEN_0x14dd78f4::field_0x1
 ldarg Arg_0x0
 stind.i8
End_Orphan_1f:
 ldloc local_0x1
 conv.ovf.i4.un
 ldloc local_0x1
 conv.ovf.i.un
 sub.ovf
 conv.ovf.u1
Start_Orphan_21:
 ldloca local_0x1
 ldc.i8 0x96f569805aff11a4
 conv.ovf.i1.un
 stind.i
End_Orphan_21:
 ldloca local_0x0
 ldind.u4
 ldc.i4.2
 conv.ovf.u8.un
 conv.ovf.i.un
 div
 sub.ovf.un
 conv.ovf.i1
 sub.ovf.un
 conv.u
 blt.un Branch_0x0

 ldloca local_0x1
 ldarg Arg_0x0
 conv.u8
 conv.i
 conv.ovf.i1
 ldloc local_0x0
 conv.i8
 ldc.i8 0x6bd239099b86985c
 conv.u8
 and
 conv.ovf.i2.un
 clt
 ldloc local_0x1
 conv.ovf.u1
 conv.u2
 ldc.i4.s -111
 rem.un
 ldloc local_0x1
 ldloc local_0x1
 cgt
 ldloc local_0x0
 clt.un
 ldsfld unsigned int64 ILGEN_0x14dd78f4::field_0x1
 conv.i1
 mul.ovf
 add
 clt.un
 conv.u
 stind.i
 br Branch_0x1

 Branch_0x0:

 ldloca local_0x0
 ldloc local_0x1
 ldloc local_0x1
 add
 conv.ovf.u2
 conv.i8
 ldc.i4.m1
 conv.u4
 conv.r.un
 conv.ovf.u8
 sub
 ldc.i4.0
 conv.ovf.u8.un
 ldc.r8 float64(0xc67e2194c08a6439)
 conv.ovf.i8
 clt.un
 ldarg Arg_0x0
 ldarg Arg_0x0
 rem.un
 ldarg Arg_0x0
 conv.ovf.i8.un
 clt
 add.ovf
 conv.i8
 and
 ldloc local_0x1
 conv.u8
 ldc.r4 float32(0x4c530a9b)
 ldc.r4 float32(0x9bd0416d)
 ceq
 shr
 ldc.i8 0x1941d7fc45ef0ab4
 ldarg Arg_0x0
 or
 ldloc local_0x1
 conv.ovf.i8
 or
 add.ovf.un
 conv.ovf.u8
 ldarg Arg_0x0
 ldarg Arg_0x0
 mul.ovf.un
 ldarg Arg_0x0
 conv.ovf.i8.un
 clt.un
 ldc.r4 float32(0x942abff4)
 ldc.r4 float32(0xa81063bb)
 cgt.un
 ldloc local_0x1
 ldloc local_0x1
 add.ovf
 sub
 add.ovf
 conv.ovf.i8
 or
 ceq
 stind.i4
 Branch_0x1:

End_Orphan_0:
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
ldc.i8 0x2d5368601d6596ce 
ldc.i4 0xb622dcf1 

call int32 ILGEN_0x14dd78f4::Method_0xb96ba46d(int64 Arg_0x0, int16 Arg_0x1)
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
