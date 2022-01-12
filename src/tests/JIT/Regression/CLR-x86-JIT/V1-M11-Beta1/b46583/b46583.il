// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x305f0007 {}
.assembly extern xunit.core {}
.class ILGEN_0x305f0007 {

.field static	unsigned int32 field_0x0
.field static	int8 field_0x1

.method static int32 Method_0x6c0fc706(float32 Arg_0x0) {
.maxstack	13

 .locals (float32 local_0x0,int8[] local_0x1,unsigned int32 local_0x2,int8 local_0x3,int8[] local_0x4,int8[] local_0x5,int16[] local_0x6,unsigned int8[] local_0x7)

ldc.r4 float32(0x68be1061) 
stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x1
ldc.i4 0x8ff6f80a 
stloc local_0x2
ldc.i4 0xb7c5d914 
stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x5
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x6
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x7

ldc.i4 0xc3e4478e 
stsfld unsigned int32 ILGEN_0x305f0007::field_0x0
ldc.i4 0x8a8466c8 
stsfld int8 ILGEN_0x305f0007::field_0x1

Start_Orphan_0:
 ldsfld int8 ILGEN_0x305f0007::field_0x1
 conv.i8
 ldloc local_0x2
 ldc.i4.5
 ble.un Branch_0x8

 ldc.i8 0x14f0640376490155
 br Branch_0x9

 Branch_0x8:

 ldc.i8 0x63c9092e505e74b
 Branch_0x9:

 add
Start_Orphan_4:
 ldloc local_0x5
 stloc local_0x4
End_Orphan_4:
 ldc.i8 0x6912ac3b62762133
 bge Branch_0x2

 ldc.i4.3
 conv.u1
 newarr [mscorlib]System.SByte
 pop
 br Branch_0x3

 Branch_0x2:

 ldloca local_0x0
 ldloc local_0x0
 stind.r4
 Branch_0x3:

End_Orphan_0:
 ldc.i4.0
Start_Orphan_7:
Start_Orphan_8:
 ldloca local_0x3
 pop
End_Orphan_8:
End_Orphan_7:
 ldc.r4 float32(0x1c6a2700)
 conv.u8
 conv.ovf.u4.un
 bgt Branch_0x0

Start_Orphan_12:
 ldc.i4.8
 ldloc local_0x2
 cgt
 stloc local_0x3
End_Orphan_12:
ldc.i4 55
br Branch_0x1

 Branch_0x0:

Start_Orphan_18:
 ldloc local_0x4
 ldc.i4.0
 ldc.i4.2
 stelem.i1
End_Orphan_18:
 ldc.i4.3
 Branch_0x1:

 ldc.i4.5
 conv.ovf.i4
 conv.u1
 newarr [mscorlib]System.Byte
 ldc.i4.5
Start_Orphan_24:
 ldc.i4 0xd889d3fd
 stsfld unsigned int32 ILGEN_0x305f0007::field_0x0
End_Orphan_24:
 ldsfld unsigned int32 ILGEN_0x305f0007::field_0x0
 ldc.i8 0x8466df45d25cc3ce
 conv.u4
 div
 add
 ldelema [mscorlib]System.Byte
 ldind.u1
 rem
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.r4 float32(0xba7917fa) 

call int32 ILGEN_0x305f0007::Method_0x6c0fc706(float32 Arg_0x0)

  pop
  leave out_of_here
} catch [mscorlib]System.IndexOutOfRangeException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100
 ret
}

}
