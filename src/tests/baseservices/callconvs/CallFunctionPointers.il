// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 5:0:0:0
}
.assembly extern System.Runtime.InteropServices
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 5:0:0:0
}
.assembly CallFunctionPointers
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module CallFunctionPointers.dll

.class public auto ansi abstract sealed beforefieldinit CallFunctionPointers
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 CallUnmanagedIntInt(void* p, int32 b) cil managed
  {
    .maxstack 2
    ldarg.1
    ldarg.0
    calli unmanaged int32(int32)
    ret
  }

  .method public hidebysig static int32 CallUnmanagedCdeclIntInt(void* p, int32 b) cil managed
  {
    .maxstack 2
    ldarg.1
    ldarg.0
    calli unmanaged cdecl int32(int32)
    ret
  }

  .method public hidebysig static int32 CallUnmanagedStdcallIntInt(void* p, int32 b) cil managed
  {
    .maxstack 2
    ldarg.1
    ldarg.0
    calli unmanaged stdcall int32(int32)
    ret
  }

  .method public hidebysig static char CallUnmanagedCharChar(void* p, char b) cil managed
  {
    .maxstack 2
    ldarg.1
    ldarg.0
    calli unmanaged char(char)
    ret
  }

  .method public hidebysig static char CallUnmanagedCdeclCharChar(void* p, char b) cil managed
  {
    .maxstack 2
    ldarg.1
    ldarg.0
    calli unmanaged cdecl char(char)
    ret
  }

  .method public hidebysig static char CallUnmanagedStdcallCharChar(void* p, char b) cil managed
  {
    .maxstack 2
    ldarg.1
    ldarg.0
    calli unmanaged stdcall char(char)
    ret
  }
}
