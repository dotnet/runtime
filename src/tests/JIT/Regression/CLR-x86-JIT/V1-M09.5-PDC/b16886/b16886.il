// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b16886' {}
.class public ILGEN_622380794 {


.field static	int16 field_0x7
.field static	unsigned int16 field_0x9
.field static	int8 field_0xe
.field static	int16 field_0x13
.field static	int16 field_0x1f
.field static	int8 field_0x22

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	18


 ldc.i4 0x6735443f
 neg
 conv.r.un
 conv.i
 ldc.i8 0x52b5523660ac7d8a
 conv.r4
 ldsfld int16 ILGEN_622380794::field_0x13
 conv.r4
 add
 conv.u
 xor
 neg
 ldc.i4.3
 conv.r8
 neg
 ldc.i4.1
 conv.r4
 conv.r4
 neg
 ldc.i8 0x1e07355f322e39f8
 conv.r4
 neg
 ceq
 conv.r8
 clt.un
 ceq
 ldsfld int8 ILGEN_622380794::field_0xe
 conv.u8
 not
 ldc.r4 float32(0x70ca3706)
 conv.i
 conv.r4
 ldc.i8 0x597b47e97b4d747
 conv.r4
 cgt
 conv.i8
 or
 ldc.i4 0x23f151d
 conv.u8
 not
 conv.u8
 not
 ldsfld int8 ILGEN_622380794::field_0x22
 conv.r.un
 neg
 conv.r4
Start_Orphan_62:
End_Orphan_62:
Start_Orphan_64:
 ldsflda int8 ILGEN_622380794::field_0x22
 ldsfld int16 ILGEN_622380794::field_0x1f
 stind.i1
End_Orphan_64:
 ldc.r4 float32(0xdee40f1)
 conv.u1
 conv.r4
 cgt
 shl
 sub
 ldsfld unsigned int16 ILGEN_622380794::field_0x9
 conv.u8
 ldsfld int16 ILGEN_622380794::field_0x7
 conv.r8
 conv.i
 conv.u8
 or
 xor
 conv.i1
 shr.un
ldc.i4 100
add
ret
}
}
