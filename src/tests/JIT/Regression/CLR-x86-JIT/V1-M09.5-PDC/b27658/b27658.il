// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b27658' {}
.class public ILGEN_0xbbd892b0 {

.field static	unsigned int32 field_0x1
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	19

 .locals (int16[] local_0x0)

 ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x0

 ldc.i4 0x154d776 
 stsfld unsigned int32 ILGEN_0xbbd892b0::field_0x1

 BLOCK_1:
 ldc.r8 float64(0x5f0bfa630236e79)
 ldc.r8 float64(0x4113c4d79947ef5)
 ldc.i4.5
 conv.r8
 add
 add
 conv.i
 conv.i8
 ldloc local_0x0
 ldc.i4.8
 ldc.r4 float32(0x58753d6)
 conv.r8
 conv.ovf.i
 clt

Start_Orphan_1e:
 ldc.i4.6
 stsfld unsigned int32 ILGEN_0xbbd892b0::field_0x1
End_Orphan_1e:

 ldc.i4.5
 rem.un
 ldelema [mscorlib]System.Int16
 ldind.i2
 shr
 conv.ovf.u1.un
 dup
 sub
ldc.i4 100
add
ret
}
}
