// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xb1b96d2b {}
.class ILGEN_0xb1b96d2b {
.method static int32 main() {
.entrypoint
.maxstack	10

 .locals (int64 local_0xf)

 ldc.i8 0x0862c9e60000000 
 stloc local_0xf

 ldloc local_0xf
 conv.r4
 conv.i4
 dup
 sub
ldc.i4 100
add
 ret
}
}
