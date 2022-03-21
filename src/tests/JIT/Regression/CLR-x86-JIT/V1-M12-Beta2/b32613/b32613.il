// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b32613 { }
.assembly extern xunit.core {}
.class ILGEN_0x59749830 {

.field static	int8[] field_0x0
.field static	float32 field_0x1
.field static	float32[] field_0x2
.field static	int64[] field_0x3
.field static	float32 field_0x4
.field static	int32[] field_0x5

.method static int32 Method_0x386e(float64 Arg_0x0, unsigned int16 Arg_0x1) {
.maxstack	19

 .locals (int16[] local_0x0,int32[] local_0x1,int16 local_0x2,unsigned int8 local_0x3,int32[] local_0x4,int16[] local_0x5)

ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x1
ldc.i4 0x7af812ab 
stloc local_0x2
ldc.i4 0x4e9f67f0 
stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x5

ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x59749830::field_0x0
ldc.r4 float32(0xf07743e) 
stsfld float32 ILGEN_0x59749830::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x59749830::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x59749830::field_0x3
ldc.r4 float32(0x6ee011af) 
stsfld float32 ILGEN_0x59749830::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x59749830::field_0x5

 ldsfld int32[] ILGEN_0x59749830::field_0x5
 ldsfld float32 ILGEN_0x59749830::field_0x4
 ldsfld float32[] ILGEN_0x59749830::field_0x2
 ldloc local_0x2
 ldelem.r4
 neg
 cgt
 ldc.i8 0x16544e1553697b73
 conv.u4
 conv.u1
 newarr [mscorlib]System.Int16
Start_Orphan_1f:
 ldsfld int8[] ILGEN_0x59749830::field_0x0
 ldc.i4.s 50
 ldelema [mscorlib]System.SByte
 pop
End_Orphan_1f:
 ldc.i4.1
 ldelema [mscorlib]System.Int16
 ldind.i2
 ldc.i4.6
 ldc.i8 0x487676101c0e4beb
 conv.ovf.u.un
 sub
 conv.ovf.u4.un
 mul.ovf.un
 add.ovf
 ldelem.i4
 ldloc local_0x2
 conv.ovf.i.un
 ldc.i4 0x57ad27d6
 conv.u
Start_Orphan_3b:
 ldloc local_0x5
 ldc.i4.8
 ldelema [mscorlib]System.Int16
 pop
End_Orphan_3b:
 ceq
 conv.ovf.u4.un
 rem.un
Start_Orphan_55:
 ldsfld int8[] ILGEN_0x59749830::field_0x0
 ldarg Arg_0x1
 ldelema [mscorlib]System.SByte
 ldc.i4.m1
 stind.i1
End_Orphan_55:
 ldarg Arg_0x0
 conv.i8
 ldc.i8 0x345ec7b5a351085
 add.ovf.un
 ldc.i4 0x3c9017d1
 conv.i8
 ldc.i4.3
 conv.ovf.i4.un
 ldloc local_0x0
 ldlen
 rem
 conv.i4
 shr.un
Start_Orphan_64:
 ldsfld int8[] ILGEN_0x59749830::field_0x0
 ldarg Arg_0x1
 ldelema [mscorlib]System.SByte
 pop
End_Orphan_64:
 ldc.r4 float32(0xab77b33)
 ckfinite
 conv.ovf.u8
 div
 add
 conv.ovf.i.un
 clt.un
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.r8 float64(0x316749285f8c7a8) 
ldc.i4 0x65865ea0 

call int32 ILGEN_0x59749830::Method_0x386e(float64 Arg_0x0, unsigned int16 Arg_0x1)
leave.s IL_FAIL
} catch [mscorlib]System.IndexOutOfRangeException {
leave.s IL_END
}
IL_END:
ldc.i4.s 100
ret
IL_FAIL:
ldc.i4.s 101
ret


}

}
