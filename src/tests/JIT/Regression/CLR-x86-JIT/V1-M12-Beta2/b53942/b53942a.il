// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b53942a {}
.class ILGEN_0x7d76169 {


.field static	int16[] field_0x0
.field static	float32 field_0x1
.field static	int64[] field_0x2
.field static	int8 field_0x3
.field static	native int field_0x4
.field static native int[] field_0x5

.method static int32 Method_0x7b15b7c1(float64 Arg_0x0, int16 Arg_0x1, unsigned int64 Arg_0x2, int16 Arg_0x3, unsigned int32 Arg_0x4, int16 Arg_0x5) {
.maxstack	17

 .locals (int32[] local_0x0,int64[] local_0x1,unsigned int64 local_0x2,unsigned int8[] local_0x3,float32 local_0x4,int16 local_0x5)

ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x1
ldc.i8 0x16cf98715ba2fc21 
stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x3
ldc.r4 float32(0xdabe3c2b) 
stloc local_0x4
ldc.i4 0x60b64a94 
stloc local_0x5


ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x7d76169::field_0x0
ldc.r4 float32(0x2f93f4a) 
stsfld float32 ILGEN_0x7d76169::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x7d76169::field_0x2
ldc.i4 0x2b0d9ed 
stsfld int8 ILGEN_0x7d76169::field_0x3
ldc.i4 0xab76d2f6 
stsfld native int ILGEN_0x7d76169::field_0x4
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x7d76169::field_0x5



 ldarg Arg_0x2
 ldloc local_0x2
 cgt
 ldarg Arg_0x2
 conv.ovf.i.un
 stsfld native int ILGEN_0x7d76169::field_0x4
 ldarg Arg_0x0
 call float32 [mscorlib]System.Math::Abs(float32)
 conv.u1
 bgt Branch_0x2

 ldc.r4 float32(0xd59da911)
 conv.u8
 br Branch_0x3

 Branch_0x2:

 ldloc local_0x2
 Branch_0x3:

 ldloc local_0x2
 not
 conv.ovf.u
 conv.r8
 call float32 [mscorlib]System.Math::Abs(float32)
 conv.ovf.u8
 ldsfld int64[] ILGEN_0x7d76169::field_0x2
 ldc.i4.0
 ldelema [mscorlib]System.Int64
 ldind.i8
 conv.ovf.i1.un
 conv.i2
 ldsfld native int ILGEN_0x7d76169::field_0x4
 conv.ovf.i1.un
 ldc.i8 0xa83e4398af92dd50
 conv.ovf.i2.un
 conv.ovf.u1.un
 sub
 clt.un
 conv.ovf.i
 conv.ovf.u8
 mul
 ble.un Branch_0x0

 ldsflda float32 ILGEN_0x7d76169::field_0x1
 ldind.r4
 conv.u1
 br Branch_0x1

 Branch_0x0:

 ldc.i4.3
 Branch_0x1:

EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.r8 float64(0x6d14494dc78348fc) 
ldc.i4 0x2e98e3d3 
ldc.i8 0xecf70870db2738e8 
ldc.i4 0x7f932988 
ldc.i4 0x162f75a3 
ldc.i4 0x2145c0b7 

call int32 ILGEN_0x7d76169::Method_0x7b15b7c1(float64 Arg_0x0, int16 Arg_0x1, unsigned int64 Arg_0x2, int16 Arg_0x3, unsigned int32 Arg_0x4, int16 Arg_0x5)
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
