// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x58f32e86 {}
.assembly extern xunit.core {}
.class ILGEN_0x58f32e86 {

.method static int32 Method_0x5d38(unsigned int32 Arg_0x0) {
.maxstack	17

 .locals (int64[] local_0x0,int32[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,float32 local_0x4,int16[] local_0x7,float64[] local_0x8)

ldnull stloc local_0x0

ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x3
ldc.r4 float32(0x2f48983) 
stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x7
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x8


Start_Orphan_0:
 ldc.i4.8
 conv.u1
 newarr [mscorlib]System.Single
 ldc.r8 float64(0x1a3667b23c6c720e)
 conv.ovf.i4
 ldelem.r4
 ldc.i8 0x757f3ceb6cd32b58
 conv.ovf.u8.un
 conv.r4
 sub
 conv.ovf.u1
 conv.u1
 newarr [mscorlib]System.UInt64

Start_Orphan_5:
 ldloc local_0x8
 ldloc local_0x2
 ldarg Arg_0x0
 ldelema [mscorlib]System.UInt32
 ldind.u4
 ldelema [mscorlib]System.Double
 ldc.r8 float64(0x40df34e62db42f16)
 conv.u8
 ldc.i8 0x2f745bb040967838
 ldc.i8 0x5bad18cf3a9e7fe0
 rem.un
 div.un
 conv.r8
 stind.r8
End_Orphan_5:

Start_Orphan_d:
 ldloca local_0x4
 ldc.r4 float32(0x598076fe)
 stind.r4
End_Orphan_d:

Start_Orphan_e:
 ldloc local_0x7
 ldc.i4.4
 ldelema [mscorlib]System.Int16
 ldc.i4.5
 stind.i2
End_Orphan_e:

Start_Orphan_c:
 ldloc local_0x8
 ldc.r4 float32(0x509c5c60)
 conv.ovf.i2
 ldelema [mscorlib]System.Double
 pop
End_Orphan_c:

 ldloc local_0x4
 conv.u
 conv.u
 ldelema [mscorlib]System.UInt64
 ldc.i8 0x7e54324e29bb31a6
 conv.u8
 stind.i8
End_Orphan_0:


Start_Orphan_2a:
 ldloc local_0x1
 ldarga Arg_0x0
 ldind.u4
 ldelema [mscorlib]System.Int32
 ldloc local_0x3
 ldlen
 ldloc local_0x1
 ldc.i4.5
 ldelem.i4
 mul.ovf.un
 stind.i4
End_Orphan_2a:

 ldloc local_0x2
 ldc.r4 float32(0x399d1191)
 conv.ovf.i4
 conv.ovf.u8
 conv.u1
 //cgt
 pop
 pop
 ldc.i4.0
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x648073fe 
call int32 ILGEN_0x58f32e86::Method_0x5d38(unsigned int32 Arg_0x0)
 pop
  leave stop
 } catch [mscorlib]System.OverflowException {
 pop
 leave stop
 }
 stop:
ldc.i4 100
ret
}

}
