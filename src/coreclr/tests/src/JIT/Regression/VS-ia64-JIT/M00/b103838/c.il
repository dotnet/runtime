// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly c
{
}
.method static int32 Main()
{
    .entrypoint
    .maxstack 2
    .locals init (int32 i,
             char c,
             unsigned int16 u)

    ldc.i4     0xFFFFF
    stloc.0
    ldloc.0
    conv.u2
    dup
    stloc.1
    stloc.2
    ldloc.0
    call       void [System.Console]System.Console::WriteLine(int32)
    ldloc.1
    call       void [System.Console]System.Console::WriteLine(int32)
    ldloc.2
    call       void [System.Console]System.Console::WriteLine(int32)
    ldc.i4 100
    ret
}

