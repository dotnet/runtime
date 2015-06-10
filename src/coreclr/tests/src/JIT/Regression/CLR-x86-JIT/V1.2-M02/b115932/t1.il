// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly t1 {}
.module t1.exe
.method public static int32 foo() cil managed
{
  .entrypoint
  .maxstack  2
  .locals init (string V_0,
           int32& V_1)
  IL_0002:  ldstr      "aaa"
  IL_0007:  stloc.0
  IL_0008:  ldloc.0
  IL_0009:  stloc.1
  IL_000a:  ldloc.1
  ldind.i4
  IL_0012:  call       void [mscorlib]System.Console::WriteLine(int32)
  ldc.i4 100
  IL_0017:  ret
}
