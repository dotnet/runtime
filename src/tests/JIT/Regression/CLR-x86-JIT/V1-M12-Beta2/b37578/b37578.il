// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x68ef9cc3 {}
.class ILGEN_0x68ef9cc3 {

.field static	unsigned int64[] field_0x7
.method static float32 Method_0x21f5(unsigned int64 Arg_0x1) {
.maxstack	17

 .locals (int32[] local_0x2,int32 local_0x5)
 ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x2
 ldc.i4 0x4 
 stloc local_0x5

 ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x68ef9cc3::field_0x7


Start_Orphan_23:
 ldloc local_0x2
 ldloc local_0x5
 ldarg Arg_0x1
 conv.i
 conv.i8
 ldc.r4 float32(0x574d3de6)
 conv.u
 conv.ovf.u8
 mul
 conv.u1
 stelem.i4
End_Orphan_23:

Start_Orphan_56:
 ldsfld unsigned int64[] ILGEN_0x68ef9cc3::field_0x7
 ldc.i4.8
 ldc.i4 55
 conv.i8
 stelem.i8
End_Orphan_56:
 ldc.r4 55.5
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0x4e2a22be136a5d29 

call float32 ILGEN_0x68ef9cc3::Method_0x21f5(unsigned int64 Arg_0x1)
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
