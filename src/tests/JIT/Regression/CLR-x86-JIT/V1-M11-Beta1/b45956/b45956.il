// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x11c02e62 {}
.assembly extern xunit.core {}
.class ILGEN_0x11c02e62 {
.method static int32 Method_0x7f2e741b(unsigned int8 Arg_0x0, unsigned int8 Arg_0x1, float32 Arg_0x2, float32 Arg_0x3, unsigned int32 Arg_0x4) {
.maxstack	15

 .locals (unsigned int32[] local_0x0,unsigned int32 local_0x1,unsigned int32 local_0x2)
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x0
ldc.i4 0xdec7765f 
stloc local_0x1
ldc.i4 0xc593a447 
stloc local_0x2

Start_Orphan_0:
 ldc.i8 0xf794666482ed95de
 ldc.i8 0xe9d64060e8fe75f5
 bgt.un Branch_0x4

 ldc.i8 0x3c930d9c65c3b662
 br Branch_0x5

 Branch_0x4:

 ldc.i8 0x8ffb984142121a28
 Branch_0x5:

 conv.ovf.u
 conv.ovf.i8.un
Start_Orphan_3:
 ldc.i4.6
 neg
 ldc.i8 0x3e152769aa9b736d
 ldc.i8 0x137b3c6e3c35fd6f
 blt.un Branch_0x8

 ldc.i4.s 7
 br Branch_0x9

 Branch_0x8:

 ldc.i4.4
 Branch_0x9:

 blt.un Branch_0x6

 ldc.i8 0x8bdf448882a4d8f3
 pop
 br Branch_0x7

 Branch_0x6:

 ldloc local_0x0
 pop
 Branch_0x7:

End_Orphan_3:
 ldc.r8 float64(0x7e06c07c45024997)
 conv.i8
 conv.ovf.i8
 ldc.i4.2
 conv.ovf.i1.un
 conv.ovf.u8
 div.un
 bgt.un Branch_0x0

 br Branch_0x1

 Branch_0x0:

 ldc.i4.2
 starg Arg_0x4
 Branch_0x1:

End_Orphan_0:
Start_Orphan_8:
Start_Orphan_9:
 ldarga Arg_0x4
 ldc.i4.5
 stind.i4
End_Orphan_9:
 ldc.i8 0x73858d08f017d0f8
 conv.ovf.i8
 ldc.i4.7
 conv.u8
 clt.un
 starg Arg_0x4
End_Orphan_8:
 ldc.i8 0x1202182b96b6ccf0
 conv.r4
 conv.i2
 conv.ovf.u4
 conv.ovf.u1.un
 ldc.i4.3
 conv.ovf.i.un
 ldloc local_0x2
 add.ovf.un
 conv.r4
 conv.i
Start_Orphan_12:
 ldloc local_0x0
 ldc.i4.8
 conv.ovf.u8.un
 ldc.i8 0xbe6bfb9276131be5
 cgt
 ldc.i4.7
 stelem.i4
End_Orphan_12:
 ldc.i8 0x8f54b2e9570b1265
 ldc.i8 0x992ef9a9a53d55f1
 cgt
 ldarg Arg_0x1
 xor
 conv.ovf.i1
 ldc.i8 0x50ab6227f9e8b917
 conv.ovf.u4
 conv.ovf.i.un
 cgt.un
 sub.ovf
 ceq
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x2ca39039 
ldc.i4 0x6c5c6664 
ldc.r4 float32(0x8240fcd4) 
ldc.r4 float32(0x6bda69bd) 
ldc.i4 0x60009466 

call int32 ILGEN_0x11c02e62::Method_0x7f2e741b(unsigned int8 Arg_0x0, unsigned int8 Arg_0x1, float32 Arg_0x2, float32 Arg_0x3, unsigned int32 Arg_0x4)

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
