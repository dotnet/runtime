// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x24fde2d6 {}
.class ILGEN_0x24fde2d6 {
.method static int32 Main()
{
.maxstack	13
.entrypoint
 ldc.r8 float64(0x1b376e25972ab43)
 call float64 [mscorlib]System.Math::Abs(float64)
 conv.i
 ldc.i4 100
 add
 ret
}
}
