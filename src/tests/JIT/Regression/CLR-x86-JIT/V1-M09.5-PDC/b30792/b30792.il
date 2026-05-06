// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b30792' {}
.class public ILGEN_0x497ea5a8 {


.field static	int8 field_0x1
.field static	unsigned int64[] field_0x2

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	19

 .locals (unsigned int16 local_0x2,unsigned int8[] local_0x3,int16 local_0x5,float64[] local_0x7)
 .try {
ldc.i4 0x5ebe294d 
stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x3
ldc.i4 0x685710a5 
stloc local_0x5
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x7


ldc.i4 0x2f1d7445 
stsfld int8 ILGEN_0x497ea5a8::field_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x497ea5a8::field_0x2


Start_Orphan_3:
 ldloc local_0x7
 ldc.i4.7
 ldelema [mscorlib]System.Double
 ldc.r8 float64(0x1b504797420a7b0)
 neg
 stind.r8
End_Orphan_3:
 ldc.r4 float32(0x67be2c77)
 neg
 conv.r4
Start_Orphan_7:
 ldc.i4.1
 conv.u1
 newarr [mscorlib]System.UInt32
 ldc.r8 float64(0x65ba66a058bc50ee)
 conv.ovf.i4
 ldelema [mscorlib]System.UInt32
 pop
End_Orphan_7:
 neg
 conv.ovf.i
 ldloc local_0x5
 conv.ovf.u2
 conv.ovf.i4.un
 ldc.r4 float32(0x5e221358)
 neg
 conv.i2
 ldloc local_0x2
 ldloc local_0x2
 ldc.i4.m1
 add
 add
 add.ovf
 or
 ldsfld int8 ILGEN_0x497ea5a8::field_0x1
 ldc.i4.s 64
 ldc.i4.4
 add.ovf
 rem
 conv.ovf.i1.un
Start_Orphan_20:
 nop
End_Orphan_20:
 rem
 conv.ovf.i.un
Start_Orphan_21:
 ldloc local_0x3
 ldc.i4.6
 ldc.i4.2
 stelem.i1
End_Orphan_21:
 cgt.un
 neg
  pop
  leave exit
} catch [mscorlib]System.OverflowException
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
