// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:2:1:0
}
.assembly extern System.Runtime.Extensions
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:2:1:0
}
.assembly 'typeintrinsicstests'
{
}
.namespace Tests
{
  .class auto ansi Program
  {
    .method public static int32  Main(class [mscorlib]System.String[] args) il managed
    {
      .entrypoint
      // Code size       68 (0x44)
      .maxstack  2
      .locals init (int32 V_0)
      IL_0000:  ldc.i4.s   100
      IL_0002:  stloc.0
      IL_0003:  ldtoken    [System.Runtime]System.Byte&
      IL_0008:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
      IL_000d:  call       instance bool [System.Runtime]System.Type::get_IsPrimitive()
      IL_0012:  brfalse.s  IL_0018

      IL_0014:  ldloc.0
      IL_0015:  ldc.i4.1
      IL_0016:  add
      IL_0017:  stloc.0
      IL_0018:  ldtoken    [System.Runtime]System.Int16&
      IL_001d:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
      IL_0022:  call       instance bool [System.Runtime]System.Type::get_IsClass()
      IL_0027:  brfalse.s  IL_002d

      IL_0029:  ldloc.0
      IL_002a:  ldc.i4.1
      IL_002b:  add
      IL_002c:  stloc.0
      IL_002d:  ldtoken    [System.Runtime]System.UInt32&
      IL_0032:  call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
      IL_0037:  call       instance bool [System.Runtime]System.Type::get_IsValueType()
      IL_003c:  brfalse.s  IL_0042

      IL_003e:  ldloc.0
      IL_003f:  ldc.i4.1
      IL_0040:  add
      IL_0041:  stloc.0
      IL_0042:  ldloc.0
      IL_0043:  ret
    }
  }
}