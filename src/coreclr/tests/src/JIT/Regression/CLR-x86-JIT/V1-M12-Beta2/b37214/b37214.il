// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x68eb9462 {}
.class ILGEN_0x68eb9462 {

.method static float32 Method_0x42ed(unsigned int8 Arg_0x0, unsigned int64 Arg_0x1, unsigned int32 Arg_0x2, float32 Arg_0x3, int8 Arg_0x4, unsigned int8 Arg_0x5, int32 Arg_0x6) {
.maxstack	19

 .locals (int32 local_0x1)

ldc.i4 0x73514940 
stloc local_0x1

 BLOCK_1:
Start_Orphan_0:
 ldarg Arg_0x1
 conv.i1
 ldarg Arg_0x6
 conv.ovf.u8.un
 ldc.i4 0x41f48fd
 conv.u8
 ldc.i4.5
 conv.ovf.u8.un
 mul.ovf
 ceq
 conv.u2
 rem.un
 stloc local_0x1
End_Orphan_0:
 ldc.r8 float64(0x1967157856fa6604)
 conv.r4
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x35bbfb3 
ldc.i8 0x3a092fd2181e3149 
ldc.i4 0x99c459f 
ldc.r4 float32(0x694b1915) 
ldc.i4 0xc859f5 
ldc.i4 0xab44ddc 
ldc.i4 0x633b7dc4 

call float32 ILGEN_0x68eb9462::Method_0x42ed(unsigned int8 Arg_0x0, unsigned int64 Arg_0x1, unsigned int32 Arg_0x2, float32 Arg_0x3, int8 Arg_0x4, unsigned int8 Arg_0x5, int32 Arg_0x6)
conv.i4
pop
leave sss
} catch [mscorlib]System.DivideByZeroException {
pop
leave sss
}
sss:
ldc.i4 100
 ret
}

}
