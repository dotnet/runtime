// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly bug { }

.method public static int32 main() il managed {
  .entrypoint
  .maxstack 8
  .try {
  ldc.i4	2147000
  ldc.i4	   1001
  mul.ovf
  call          void [System.Console]System.Console::WriteLine(int32)
  leave s
  } catch [mscorlib]System.OverflowException {
  pop
  leave s
  }
  s:
ldc.i4 100
  ret
}
