// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b28596' {}
.class public ILGEN_0x393f432b {

.field static	float32 field_0x3

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11

 .locals (unsigned int8[] local_0x2,int64[] local_0x7,unsigned int8 local_0x8)
.try {
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x7
ldc.i4 0x2f2917c 
stloc local_0x8

ldc.r4 float32(0x3b902473) 
stsfld float32 ILGEN_0x393f432b::field_0x3

Start_Orphan_0:
 ldloc local_0x7
 ldc.i4 55
Start_Orphan_1:
 ldloca local_0x8
 ldc.i4 0x73ef294b
 stind.i1
End_Orphan_1:
 ldloc local_0x7
 ldc.i4 0x5909163b
 ldelema [mscorlib]System.Int64
 ldind.i8
 ldc.i8 1
 mul
 stelem.i8
End_Orphan_0:
 ldsflda float32 ILGEN_0x393f432b::field_0x3
 ldind.r4
Start_Orphan_a:
 ldloc local_0x2
 ldc.i4 1
 ldc.i4 59
 stelem.i1
End_Orphan_a:

 conv.u2
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
