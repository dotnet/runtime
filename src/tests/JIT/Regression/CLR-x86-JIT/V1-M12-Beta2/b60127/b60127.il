// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x68501543 {}
.assembly extern xunit.core {}
.class ILGEN_0x68501543 {
.method static int32 Method_0xfcb53e4a(float32 Arg_0x4) {
.maxstack	9
 ldc.i4.3
 newarr [mscorlib]System.Int32
 ldc.i4.2
 ldelem.i4
 brtrue Branch_0x0
 ldc.i4 0xdbe32521
 conv.r8
 br Branch_0x1
Branch_0x0:
 ldarg Arg_0x4
Branch_0x1:
 conv.i4
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
 ldc.r4 3.3
 call int32 ILGEN_0x68501543::Method_0xfcb53e4a(float32 Arg_0x4)
 ldc.i4 3689096381
 sub
ret
}
}
