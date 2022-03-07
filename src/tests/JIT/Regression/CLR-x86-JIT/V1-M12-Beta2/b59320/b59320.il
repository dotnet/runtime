// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b59320' {}
.assembly extern xunit.core {}
.class ILGEN_0x77856409 {
.field static	unsigned int64 field_0x3
.field static	unsigned int16 field_0x4
.method static int32 Method_0x8322412b( native int Arg_0x3) {
.maxstack	19

 .locals (unsigned int64[] local_0x0,unsigned int32[] local_0x1,int32[] local_0x2,unsigned int64 local_0x3,int64 local_0x4,int32[] local_0x5,unsigned int32[] local_0x6)

ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x2
ldc.i8 0x4d33da6d5b6e9b00 
stloc local_0x3
ldc.i8 0xf7d5138c41fa2f41 
stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x5
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x6

ldc.i8 0x4f3496751c1e6619 
stsfld unsigned int64 ILGEN_0x77856409::field_0x3
ldc.i4 0xbd32387a 
stsfld unsigned int16 ILGEN_0x77856409::field_0x4

Start_Orphan_e:
 ldloc local_0x2
 ldloc local_0x0
 ldc.i4.1
 ldelem.u8
 ldloca local_0x4
 ldind.i8
 add.ovf
 conv.ovf.i2
 ldloc local_0x4
 conv.ovf.u1.un
 ldarg Arg_0x3
 conv.ovf.i.un
 cgt
 not
 stelem.i4
End_Orphan_e:
 ldc.i4.s 121
 conv.ovf.u8
 ldsfld unsigned int64 ILGEN_0x77856409::field_0x3
 conv.u8
 mul
 conv.ovf.u.un
 ldc.i4.4
 conv.ovf.u4.un
 ldc.i4.2
 conv.ovf.i2
 ldarg Arg_0x3
 ldarg Arg_0x3
 rem.un
 sub
 cgt
 ldc.r4 float32(0xf1b7ed)
 conv.i8
 conv.ovf.u1
 ldloc local_0x4
 ldloc local_0x3
 rem
 conv.ovf.i1.un
 sub.ovf
 cgt.un
 ldc.i4.3
 conv.ovf.i1
Start_Orphan_37:
 ldsflda unsigned int16 ILGEN_0x77856409::field_0x4
 ldc.r4 float32(0xb09cdf22)
 conv.i4
 stind.i2
End_Orphan_37:
 ldloc local_0x3
 ldsfld unsigned int64 ILGEN_0x77856409::field_0x3
 clt.un
 mul
 div
 xor
 conv.ovf.i1
 ldc.i4.0
 ldc.i4 0x54679a45
 ldc.i8 0xf76f22e843d35afb
 conv.ovf.i8.un
 conv.i4
Start_Orphan_4a:
 ldloc local_0x6
 ldc.i4.1
 ldc.i4.6
 stelem.i4
End_Orphan_4a:
 add.ovf.un
 neg
 ldc.i4.4
 mul.ovf.un
 ceq
 sub.ovf.un
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x1f0e1c47 conv.i

call int32 ILGEN_0x77856409::Method_0x8322412b( native int Arg_0x3)
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
