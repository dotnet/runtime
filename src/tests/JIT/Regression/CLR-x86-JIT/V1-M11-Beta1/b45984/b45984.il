// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b45984' {}
.assembly extern xunit.core {}
.class ILGEN_0x496bb0b {

.field static	int32[] field_0x1
.method static int32 Method_0x110798c(int64 Arg_0x0) {
.maxstack	9

 .locals (int32 local_0x0,int16 local_0x1,unsigned int8 local_0x2,unsigned int64[] local_0x3,unsigned int8 local_0x4,float32 local_0x5,unsigned int64 local_0x6,int8[] local_0x7)

ldc.i4 0xb748a1a5 
stloc local_0x0
ldc.i4 0xc20e1d87 
stloc local_0x1
ldc.i4 0x4dfab155 
stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x3
ldc.i4 0x36f36fbe 
stloc local_0x4
ldc.r4 float32(0xeb7bd814) 
stloc local_0x5
ldc.i8 0x738c8258272dff7c 
stloc local_0x6
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x7

ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x496bb0b::field_0x1

Start_Orphan_0:
 ldc.i4.3
 ldc.i4.1
 blt Branch_0x2

 ldloc local_0x6
 br Branch_0x3

 Branch_0x2:

 ldc.i8 0x2dc865e4f62bb370
 Branch_0x3:

 ldc.i4.2
 ldloc local_0x0
 blt.un Branch_0x4

 ldarg Arg_0x0
 br Branch_0x5

 Branch_0x4:

 ldloc local_0x6
 Branch_0x5:

 sub
 ldc.i4.8
 conv.i4
 ldc.i4.2
 bgt Branch_0x8

 ldc.i8 0x368a158b446f2bd1
 br Branch_0x9

 Branch_0x8:

 ldc.r4 float32(0x43f92e68)
 conv.i8
 Branch_0x9:

 blt.un Branch_0x0

 ldsfld int32[] ILGEN_0x496bb0b::field_0x1
 ldloc local_0x0
 ldelema [mscorlib]System.Int32
 pop
 br Branch_0x1

 Branch_0x0:

 ldloca local_0x4
 ldc.i4 0x4411a026
 stind.i1
 Branch_0x1:

End_Orphan_0:
 ldc.i4.0
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.i8 0xe3b349bbc6880fff 

call int32 ILGEN_0x496bb0b::Method_0x110798c(int64 Arg_0x0)
ldc.i4 100
add
 ret
}

}
