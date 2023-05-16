// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b28597' {}
.class public ILGEN_0x443f4d38 {

.field static	int16 field_0x2
.field static	int32[] field_0x4
.field static	int32 field_0x6

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	19

 .locals (int32 local_0x0)

 ldc.i4.0  stloc.0
 .try {
ldc.i4 0x58954674 
stloc local_0x0

ldc.i4 0x30bf5855 
stsfld int16 ILGEN_0x443f4d38::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x443f4d38::field_0x4
ldc.i4 0x19943fd1 
stsfld int32 ILGEN_0x443f4d38::field_0x6

Start_Orphan_0:
 ldsfld int16 ILGEN_0x443f4d38::field_0x2
 conv.u1
 newarr [mscorlib]System.Int32
 stsfld int32[] ILGEN_0x443f4d38::field_0x4
End_Orphan_0:
 ldc.r4 float32(0x2cd21ed9)
 conv.ovf.u8.un
 ldc.i8 0x634c6b44489d328f
 conv.ovf.u8
Start_Orphan_7:
 ldsflda int32 ILGEN_0x443f4d38::field_0x6
 ldc.i4.2
 stind.i4
End_Orphan_7:
 ldc.r4 float32(0x42ef6dd5)
 conv.u8
 div.un
 ldloc local_0x0
 conv.i
 ldc.i4.6
 ldc.i4.3
 rem
 cgt
 conv.ovf.u8
Start_Orphan_c:
 ldc.r4 float32(0x4da1214e)
 conv.i1
 stsfld int32 ILGEN_0x443f4d38::field_0x6
End_Orphan_c:
 ldloc local_0x0
 conv.u8
 ldc.r8 float64(0x689d5db34e120c9)
 conv.ovf.i8
 rem
 sub.ovf
 sub.ovf.un
 or
Start_Orphan_16:
 ldsflda int16 ILGEN_0x443f4d38::field_0x2
 ldc.i4.0
 stind.i2
End_Orphan_16:
 ldc.i8 0x54b76ff3125a2c8a
 conv.ovf.i
 conv.ovf.u8.un
 rem.un
 conv.u4
   pop
  leave exit
} catch [mscorlib]System.DivideByZeroException
{
  pop
  leave exit
}
exit:
  ldc.i4.0
ldc.i4 100
add
ret
}
}
