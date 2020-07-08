// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2d277f5e{}
.class ILGEN_0x2d277f5e {
.field static	unsigned int32[] field_0x0
.field static	unsigned int8 field_0x5
.field static	native int field_0x9
.method static int32 Method_0x70755e5e(unsigned int16 Arg_0x1, int64 Arg_0x5, native int Arg_0x9) {
.maxstack	17

ldc.i4 255
newarr [mscorlib]System.UInt32 
stsfld unsigned int32[] ILGEN_0x2d277f5e::field_0x0
ldc.i4 0xd60572d5 
stsfld unsigned int8 ILGEN_0x2d277f5e::field_0x5
ldc.i4 0x33a52774 
stsfld native int ILGEN_0x2d277f5e::field_0x9
 ldc.i4.m1
 conv.u8
 ldc.i8 55
 ldarg Arg_0x5
 sub.ovf.un
 div
 ldsfld native int ILGEN_0x2d277f5e::field_0x9
 conv.ovf.i8.un
 sub
 conv.u2
Start_Orphan_4d:
ldc.i8 55
 conv.i1
 ldarg Arg_0x9
 conv.ovf.i4.un
 ceq
 starg Arg_0x1
End_Orphan_4d:
 ldc.i8 55
 conv.i
 ldsfld native int ILGEN_0x2d277f5e::field_0x9
 conv.ovf.i1
 ldarg Arg_0x9
 ldsfld native int ILGEN_0x2d277f5e::field_0x9
 mul
 conv.u
 div.un
 sub
 ldsfld unsigned int32[] ILGEN_0x2d277f5e::field_0x0
Start_Orphan_65:
 ldsfld unsigned int8 ILGEN_0x2d277f5e::field_0x5
 ldarg Arg_0x1
 shr
 starg Arg_0x1
End_Orphan_65:
 ldc.i4 44
 newarr [mscorlib]System.SByte
 ldarg Arg_0x5
 conv.ovf.i2
 ldelem.i1
 ldelem.u4
 sub
 ldc.i4 55
 ldsfld native int ILGEN_0x2d277f5e::field_0x9
 rem
 ldarg Arg_0x1
 ldsfld native int ILGEN_0x2d277f5e::field_0x9
 cgt
 rem
 div
 pop
 pop
 ldc.i8 55
 conv.i4
 ldsfld native int ILGEN_0x2d277f5e::field_0x9
 ldsfld native int ILGEN_0x2d277f5e::field_0x9
 add
 conv.i4
 cgt
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try
{
ldc.i4 0xd5e40332 
ldc.i8 0x9134640919a41d36 
ldc.i4 0x960c233 conv.i
call int32 ILGEN_0x2d277f5e::Method_0x70755e5e(unsigned int16 Arg_0x1, int64 Arg_0x5, native int Arg_0x9)
leave FAIL
}
catch [mscorlib]System.OverflowException
{
leave PASS
}
PASS:
	ldc.i4 100
	ret

FAIL:
	ldc.i4 1
	ret
}

}
