// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b28080' {}
.class public ILGEN_0x814c1480 {

.field static	int8[] field_0x0
.field static	unsigned int64 field_0x1
.field static	int64[] field_0x2
.field static	int64 field_0x3

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	17

 .locals (int16[] local_0x0,float32 local_0x1,int64[] local_0x2,int32 local_0x3,int64[] local_0x4,int8[] local_0x5,unsigned int8 local_0x6,unsigned int8[] local_0x7)

 .try {
ldc.r4 float32(0x1ef740e9) 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x5
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x7


ldc.i8 0x1b7715a7615131f1 
stsfld unsigned int64 ILGEN_0x814c1480::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x814c1480::field_0x2
ldc.i8 0xd6a29817d1e2302 
stsfld int64 ILGEN_0x814c1480::field_0x3

Start_Orphan_2d:
 ldsfld unsigned int64 ILGEN_0x814c1480::field_0x1
 conv.i2
 stloc local_0x6
End_Orphan_2d:

Start_Orphan_26:
 ldsfld int64 ILGEN_0x814c1480::field_0x3
 conv.u1
Start_Orphan_2a:
 ldsfld int64[] ILGEN_0x814c1480::field_0x2
 ldc.i4.1
 conv.ovf.u2.un
 conv.i4
 ldloc local_0x1
 conv.ovf.u8
 stelem.i8
End_Orphan_2a:
 newarr [mscorlib]System.Byte
 ldc.i4.s 110
 ldloc local_0x5
 ldc.i4 34
 ldelema [mscorlib]System.SByte
 ldind.i1
 stelem.i1
End_Orphan_26:
 ldc.r4 float32(0x39b545f)
Start_Orphan_31:
 ldloc local_0x7
 ldc.i4 55
 ldc.i4 66
 stelem.i1
End_Orphan_31:
 conv.ovf.u2
 
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
