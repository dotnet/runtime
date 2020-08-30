// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1ad812c2 {}
.class ILGEN_0x1ad812c2 {
.method static int32 Method_0xf621eda6(unsigned int16 Arg_0x0, int64 Arg_0x1) {
.maxstack	9

 .locals (int16[] local_0x0,float64 local_0x1,unsigned int16[] local_0x2,unsigned int64 local_0x3,int8 local_0x4,int64 local_0x5,float64 local_0x6,float64[] local_0x7)

ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x0
ldc.r8 float64(0xf14851fbd596b02b) 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x2
ldc.i8 0x684ef17cb5bde61b 
stloc local_0x3
ldc.i4 0x51bfc9e 
stloc local_0x4
ldc.i8 0xb1ad95d4dd776744 
stloc local_0x5
ldc.r8 float64(0x7e8acc27903ed93d) 
stloc local_0x6


Start_Orphan_0:
 ldc.i4.s -96
 brtrue Branch_0x2

 ldloc local_0x4
 br Branch_0x3

 Branch_0x2:

 ldloc local_0x4
 Branch_0x3:

 conv.ovf.i8
Start_Orphan_2:
 ldc.i4.6
 ldc.i4 0xc45a60cf
 beq Branch_0x4

 nop
 br Branch_0x5

 Branch_0x4:

 nop
 Branch_0x5:

End_Orphan_2:
 ldc.i4.8
 ldloc local_0x4
 bge Branch_0x8

 ldc.r4 float32(0xd2f53782)
 br Branch_0x9

 Branch_0x8:

 ldc.r4 float32(0xeaf0714b)
 Branch_0x9:

 ldloca local_0x6
 ldind.r8
 blt.un Branch_0x6

 ldloca local_0x3
 ldind.u8
 br Branch_0x7

 Branch_0x6:

 ldloca local_0x5
 ldind.i8
 Branch_0x7:

 beq Branch_0x0

 ldarg Arg_0x0
 conv.ovf.i8.un
 pop
 br Branch_0x1

 Branch_0x0:

 ldc.i8 0x58af443ce6f2601a
 conv.u8
 starg Arg_0x1
 Branch_0x1:

End_Orphan_0:
Start_Orphan_7:
 ldloc local_0x0
 ldc.i4.s 90
 ldelema [mscorlib]System.Int16
 pop
End_Orphan_7:
 ldc.i4.1
 conv.i4
 conv.ovf.i1
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i4 0x28e5ef35 
ldc.i8 0x2d53e8fa41dcf19e 

call int32 ILGEN_0x1ad812c2::Method_0xf621eda6(unsigned int16 Arg_0x0, int64 Arg_0x1)
	ldc.i4.1
	xor
ldc.i4 100
add
 ret
}

}
