// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly t {}
.method public static int32 Main() cil managed
{
  .entrypoint
  .maxstack  2
  .locals init (string V_0,
           uint8& V_1)
  ldstr      "aaa"
  stloc.0
  ldloc.0
  stloc.1
  ldloc.1
  conv.i4
  call       string [mscorlib]System.Convert::ToString(int32)
  call       void [mscorlib]System.Console::WriteLine(string)
  ldc.i4 100
  ret
}
