// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b37238' {}
.assembly extern xunit.core {}
.class ILGEN_0x7fee1ca2 {
.method static float32 Method_0x54eb(int64 Arg_0x0, int16 Arg_0x1, unsigned int32 Arg_0x2, unsigned int16 Arg_0x3, int8 Arg_0x4, int16 Arg_0x5, int8 Arg_0x6) {
.maxstack	15

 .locals (int64[] local_0x0,int32 local_0x1,unsigned int64[] local_0x2,unsigned int8 local_0x3,unsigned int64[] local_0x4,int64 local_0x5,unsigned int8[] local_0x6,unsigned int32 local_0x7,int32[] local_0x8)

ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x0
ldc.i4 0xb44784d 
stloc local_0x1
ldc.i8 0x54ac6d6f10d26fa 
stloc local_0x5
ldc.i4 0x3e7b537 
stloc local_0x7

Start_Orphan_5:
 ldarg Arg_0x0
 conv.i4
 stloc local_0x7
End_Orphan_5:

 ldloc local_0x1
 conv.ovf.i1.un
 ldarg Arg_0x0
 ldloc local_0x5
 clt.un
 cgt
 ldloc local_0x0
 ldloc local_0x7
 ldelem.i8
 conv.i1
 mul
 conv.r8
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i8 0x20c16ffc86d5015 
ldc.i4 0x71924c8b 
ldc.i4 0x2e2c213c 
ldc.i4 0x234ab01 
ldc.i4 0x1b3d6105 
ldc.i4 0x28233ccd 
ldc.i4 0x2d0873ff 

call float32 ILGEN_0x7fee1ca2::Method_0x54eb(int64 Arg_0x0, int16 Arg_0x1, unsigned int32 Arg_0x2, unsigned int16 Arg_0x3, int8 Arg_0x4, int16 Arg_0x5, int8 Arg_0x6)
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
