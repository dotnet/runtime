// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2693d3a2 {}
.assembly extern xunit.core {}
.class ILGEN_0x2693d3a2 {
.method static int32 Method_1(int32 Arg_0x0) {
.maxstack	7
 ldarga Arg_0x0
 pop
 ldc.i4.m1
 conv.r.un
 conv.ovf.u4
 ret
}
.method static int32 Method_2(int32 Arg_0x0) {
.maxstack	7
 ldarg Arg_0x0
 pop
 ldc.i4.m1
 conv.r.un
 conv.ovf.u4
 ldc.i4 4294967195
 sub
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
 ldc.i4 0x6b0f7b31 
 call int32 ILGEN_0x2693d3a2::Method_2(int32 Arg_0x0)
 pop
 ldc.i4 0x6b0f7b31 
 call int32 ILGEN_0x2693d3a2::Method_1(int32 Arg_0x0)
 ldc.i4 4294967195
 sub
 ret
}
}
