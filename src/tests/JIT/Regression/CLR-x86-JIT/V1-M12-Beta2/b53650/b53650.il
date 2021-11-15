// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x4f1d560c {}
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}
.class ILGEN_0x4f1d560c {
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.locals (float64 local_0x0)

 ldc.r8 float64(0x5a75ab3594b996a7) 
 stloc local_0x0

 ldloca local_0x0
 ldind.r8
 ldc.r4 float32(0x7fa03458)
 ldloc local_0x0
 clt
 conv.r8
 div 
 pop

 ldc.i4 100
 ret
}
}
