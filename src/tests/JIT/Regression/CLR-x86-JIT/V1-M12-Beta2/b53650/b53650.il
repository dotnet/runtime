// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b53650' {}
.assembly extern xunit.core {}
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
