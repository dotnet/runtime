// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}
.assembly ILGEN_0x9461697d {}
.class ILGEN_0x9461697d {
.method static int32 Main() {
.entrypoint
.maxstack	2
 ldc.r8 1.2e+309
 dup
 call void [System.Console]System.Console::WriteLine(float64)
 call float64 [mscorlib]System.Math::Sin(float64)
 dup
 call void [System.Console]System.Console::WriteLine(float64)
 ldc.r8 1.0
 clt
 ldc.i4 100
 mul
 ldc.i4 100
 add
 ret
}
}
