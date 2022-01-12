// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x68eb95f0 {}
.assembly extern xunit.core {}
.class ILGEN_0x68eb95f0 {

.field static	float32[] field_0x8
.method static float32 Method_0x2dae(int8 Arg_0x0, int32 Arg_0x1, int64 Arg_0x2, int8 Arg_0x3, unsigned int16 Arg_0x4, int64 Arg_0x5) {
.maxstack	19

 .locals (float32[] local_0x2,unsigned int16 local_0x3)

ldnull stloc.0
ldc.i4.0 stloc.1

ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x68eb95f0::field_0x8

 ldc.i4.6
 conv.i1
 conv.u1
 newarr [mscorlib]System.Byte
 ldlen
 conv.r.un
 conv.ovf.i2
 ldc.i4.6
 ldloca local_0x3
 ldind.u2
 conv.ovf.i8
Start_Orphan_34:
 ldsfld float32[] ILGEN_0x68eb95f0::field_0x8
 stloc local_0x2
End_Orphan_34:
 ldc.i4.s -5
 shl
 conv.ovf.i.un
 add.ovf.un
 conv.ovf.u1.un
 rem.un
 conv.r4
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x28cb3cf 
ldc.i4 0xe5d194c 
ldc.i8 0x540518d125d86ca2 
ldc.i4 0x237f5640 
ldc.i4 0x1096319f 
ldc.i8 0x316678b31cf056dd 

call float32 ILGEN_0x68eb95f0::Method_0x2dae(int8 Arg_0x0, int32 Arg_0x1, int64 Arg_0x2, int8 Arg_0x3, unsigned int16 Arg_0x4, int64 Arg_0x5)
conv.i4
pop
leave sss
} catch [mscorlib]System.OverflowException {
pop
leave sss
}
sss:
ldc.i4 100
 ret
}

}
