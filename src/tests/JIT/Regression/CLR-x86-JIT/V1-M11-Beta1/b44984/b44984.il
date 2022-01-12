// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xd135fd7 {}
.assembly extern xunit.core {}
.class ILGEN_0xd135fd7 {
.method static int32 Method_0x96745647(int8 Arg_0x0, unsigned int8 Arg_0x1, unsigned int32 Arg_0x2, int16 Arg_0x3, unsigned int8 Arg_0x4, unsigned int32 Arg_0x5, int8 Arg_0x6, int64 Arg_0x7, float64 Arg_0x8) {
.maxstack	7

 .locals (unsigned int32[] local_0x0,int16 local_0x1,int8[] local_0x2,int64 local_0x3)

ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x0
ldc.i4 0xbef1cd3e 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x2
ldc.i8 0xe5b3ccc5db1f2aa3 
stloc local_0x3


 ldarg Arg_0x3
 ldloc local_0x1
 ldarg Arg_0x3
 bge Branch_0x2

 ldc.i4.2
 br Branch_0x3

 Branch_0x2:

 ldc.i4.4
 Branch_0x3:

 beq Branch_0x0

 ldc.i4.s 30
 ldloc local_0x1
 bge.un Branch_0x4

 ldloc local_0x3
 br Branch_0x5

 Branch_0x4:

 ldarg Arg_0x7
 Branch_0x5:

 br Branch_0x1

 Branch_0x0:

 ldc.i4.s 108
 conv.i8
 Branch_0x1:

 ldarg Arg_0x3
 conv.ovf.i4.un
 ldc.i4 0xe4e061fd
 conv.i4
 blt.un Branch_0x8

 ldarg Arg_0x7
 ldarg Arg_0x7
 add.ovf.un
 br Branch_0x9

 Branch_0x8:

 ldloc local_0x3
 ldarg Arg_0x7
 add
 Branch_0x9:

 clt
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0xe4e591c5 
ldc.i4 0x84196de8 
ldc.i4 0x441a0d3d 
ldc.i4 0xaae59b8a 
ldc.i4 0x7d4649e9 
ldc.i4 0x4425385a 
ldc.i4 0x7f7ee258 
ldc.i8 0xa607da729c29200c 
ldc.r8 float64(0x86c207634f436fee) 

call int32 ILGEN_0xd135fd7::Method_0x96745647(int8 Arg_0x0, unsigned int8 Arg_0x1, unsigned int32 Arg_0x2, int16 Arg_0x3, unsigned int8 Arg_0x4, unsigned int32 Arg_0x5, int8 Arg_0x6, int64 Arg_0x7, float64 Arg_0x8)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
