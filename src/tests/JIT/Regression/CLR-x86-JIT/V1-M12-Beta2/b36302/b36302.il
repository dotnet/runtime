// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x7ff55ade {}
.assembly extern xunit.core {}
.class ILGEN_0x7ff55ade {

.method static float32 Method_0x39f2(int64 Arg_0x0, float64 Arg_0x1, int8 Arg_0x2)
{
.maxstack	19

 ldc.i4.0
 conv.i8
 neg
 conv.i
 conv.ovf.i8
 ldarg Arg_0x1
 conv.ovf.u
 conv.ovf.u8.un
 conv.ovf.u
 conv.ovf.u8
 mul.ovf.un
 conv.i
 ldarga Arg_0x1
 ldind.r8
 conv.i
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
ldc.i8 0x79862fe355a859ab
ldc.r8 float64(0x3668541f66c4ad2)
ldc.i4 0xdc346b5

call float32 ILGEN_0x7ff55ade::Method_0x39f2(int64 Arg_0x0, float64 Arg_0x1, int8 Arg_0x2)
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
