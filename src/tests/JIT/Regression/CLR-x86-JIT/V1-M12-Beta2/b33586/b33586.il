// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x3b5fce00 {}
.assembly extern xunit.core {}
.class ILGEN_0x3b5fce00 {

.method static int32 Method_0x21de(unsigned int32 Arg_0x0, int16 Arg_0x1, float32 Arg_0x2, unsigned int64 Arg_0x3, int32 Arg_0x4) {
.maxstack	7

 .locals (unsigned int64[] local_0x0)

 ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x0

 ldc.i4.6
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.i4 0x14612d4a 
ldc.i4 0x3be37fb5 
ldc.r4 float32(0x47f22d0a) 
ldc.i8 0x15d76c9b13534dd3 
ldc.i4 0x23d4239b 

call int32 ILGEN_0x3b5fce00::Method_0x21de(unsigned int32 Arg_0x0, int16 Arg_0x1, float32 Arg_0x2, unsigned int64 Arg_0x3, int32 Arg_0x4)
ldc.i4 6
sub
ldc.i4 100
add
ret
}

}
