// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b28595' {}
.class public ILGEN_0xd015788 {

.field static	float32 field_0x1
.field static	unsigned int32 field_0x6
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	17

 .locals (unsigned int16[] local_0x5)
 .try {
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x5
ldc.r4 float32(0x3f63cc3) 
stsfld float32 ILGEN_0xd015788::field_0x1
ldc.i4 0x683c7165 
stsfld unsigned int32 ILGEN_0xd015788::field_0x6

 ldc.i4 55
Start_Orphan_b:
 ldloc local_0x5
 ldc.i4.8
 ldelem.u2
 newarr [mscorlib]System.UInt16
 ldc.i4.m1
 ldsflda float32 ILGEN_0xd015788::field_0x1
 ldind.r4
 conv.i4
 stelem.i2
End_Orphan_b:
 ldc.i4.5
 ldc.i4 55
 ldloc local_0x5
 ldlen
 sub.ovf
 ldsfld float32 ILGEN_0xd015788::field_0x1
 conv.i1
 clt.un
 shl
 clt
Start_Orphan_3b:
 ldloc local_0x5
 ldlen
 stsfld unsigned int32 ILGEN_0xd015788::field_0x6
End_Orphan_3b:
  pop
  leave exit
} catch [mscorlib]System.IndexOutOfRangeException
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
