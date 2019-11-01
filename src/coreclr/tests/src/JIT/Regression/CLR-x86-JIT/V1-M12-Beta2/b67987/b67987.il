// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x48eb9718 {}
.class ILGEN_0x48eb9718 {
.field static	unsigned int8 field_0x1
.method static int32 Method_0xb80cd6ff() {
.maxstack	7

 .locals (float32[] LOCAL_0x0,native int[] LOCAL_0x1,unsigned int64[] LOCAL_0x2,unsigned int8[] LOCAL_0x3)
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0x3
ldc.i4 0xfdfd6f90 
stsfld unsigned int8 ILGEN_0x48eb9718::field_0x1
 ldc.i8 0xf73abe2a54ffaaca
 neg
 Branch_0x5:
Start_Orphan_3:
 ldloc LOCAL_0x0
 ldc.i4.s -96
 ldelema [mscorlib]System.Single
 ldc.r8 float64(0x23df72bddbc89785)
 stind.r4
End_Orphan_3:
 ldloc LOCAL_0x2
 ldc.i4.s 28
 ldc.i4.1
 ble Branch_0xa
 ldc.i4.1
 br Branch_0xb
Branch_0xa:
 ldc.i4.7
Branch_0xb:
 ldelem.u8
 blt.un Branch_0x0
 ldc.i4.0
Branch_0xf:
 ldsfld unsigned int8 ILGEN_0x48eb9718::field_0x1
 bge Branch_0xc
 ldc.r4 float32(0xc8f4f8dc)
 conv.i4
 br Branch_0xd
Branch_0xc:
 ldc.i4.4
Branch_0x11:
Branch_0xd:
 br Branch_0x1
Branch_0x0:
 ldc.i4.4
Branch_0x1:
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
call int32 ILGEN_0x48eb9718::Method_0xb80cd6ff()
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