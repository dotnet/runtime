// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14067b
{
}
.class ILGEN_622380794 {

.method static int32 main() {
.entrypoint
.maxstack	10

 .locals (int64 local_0x1)
 ldc.i8 0
 stloc.0
 
 ldloca local_0x1
 ldind.i8
 conv.u
 conv.r4
 conv.i4
 pop
 ldc.i4 100
 ret
}
}
