// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern legacy library mscorlib {}
.assembly 'ldobjfloat'
{
  
}
.class M
{
  .method public static int32 Main()
  {
    .entrypoint
    .maxstack 10
    .locals (float32 f)
    ldc.r4 3.14
    stloc f
    ldloca f
    ldobj float32
    call void [System.Console]System.Console::WriteLine(float32)
    ldc.i4 100
    ret
  }
}
