// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x6322da7 {}
.assembly extern xunit.core {}
.class ILGEN_0x6322da7 {

.field static	unsigned int16 field_0x2
.method static int32 Method_0xc5c40454(int16 Arg_0x0, int8 Arg_0x1, int16 Arg_0x2, unsigned int16 Arg_0x3, float64 Arg_0x4, float32 Arg_0x5, int32 Arg_0x6, float64 Arg_0x7) {
.maxstack	11

 .locals (unsigned int8[] local_0x0,float32 local_0x1,float32 local_0x2,unsigned int32 local_0x3,float32 local_0x4,int32[] local_0x5)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x0
ldc.r4 float32(0x2e25b87e) 
stloc local_0x1
ldc.r4 float32(0xe931e8f8) 
stloc local_0x2
ldc.i4 0x3b28d0cb 
stloc local_0x3
ldc.r4 float32(0xc9265777) 
stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x5

Start_Orphan_0:
 ldarg Arg_0x5
 ldc.r8 float64(0x237c37471cf59627)
 clt.un
ldc.i4 0x5bf37c98 
 ldc.i4.4
 bge.un Branch_0x8

 ldloc local_0x3
 br Branch_0x9

 Branch_0x8:

 ldc.i4.2
 Branch_0x9:

 blt.un Branch_0x0
 br Branch_0x1

 Branch_0x0:
 Branch_0x1:

End_Orphan_0:
ldc.i4 0x5bf37c98 
 conv.ovf.i8
 conv.ovf.i1.un
 conv.i8
Start_Orphan_6:
 ldloc local_0x1
 starg Arg_0x7
End_Orphan_6:
 ldarg Arg_0x6
 conv.ovf.u8
 conv.ovf.i8
 xor
Start_Orphan_9:
 ldc.r8 float64(0xb31b3b90a1fb9814)
 conv.i2
 stsfld unsigned int16 ILGEN_0x6322da7::field_0x2
End_Orphan_9:
 ldarga Arg_0x7
 ldind.r8
 conv.ovf.i8
 cgt
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x8a7f5a94 
ldc.i4 0x6ff484b5 
ldc.i4 0x51a3a0ef 
ldc.i4 0x2103b0d9 
ldc.r8 float64(0x3247778e7d8903e7) 
ldc.r4 float32(0xe61b71f0) 
ldc.i4 0x18389b17 
ldc.r8 float64(0x176ef8354a3e13d2) 

call int32 ILGEN_0x6322da7::Method_0xc5c40454(int16 Arg_0x0, int8 Arg_0x1, int16 Arg_0x2, unsigned int16 Arg_0x3, float64 Arg_0x4, float32 Arg_0x5, int32 Arg_0x6, float64 Arg_0x7)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100
 ret
}

}
