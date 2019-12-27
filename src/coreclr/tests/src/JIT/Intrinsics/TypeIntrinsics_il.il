// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }

.assembly TypeIntrinsicsTests
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 
}

.class private auto ansi beforefieldinit Test
       extends [mscorlib]System.Object
{
  .method private hidebysig static int32 Main() cil managed
  {
    .entrypoint
    .maxstack  1
    // it's not currently possible to produce `ldtoken string&` in C#
    ldtoken    [System.Runtime]System.String&
    call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    call       instance bool [System.Runtime]System.Type::get_IsValueType()
    brtrue     FAILED

    ldtoken    [System.Runtime]System.Int16&
    call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    call       instance bool [System.Runtime]System.Type::get_IsValueType()
    brfalse.s  FAILED

    ldtoken    [System.Runtime]System.Object&
    call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    call       instance bool [System.Runtime]System.Type::get_IsValueType()
    brtrue.s   FAILED

    ldtoken    [System.Runtime]System.Decimal&
    call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    call       instance bool [System.Runtime]System.Type::get_IsValueType()
    brfalse.s  FAILED

    ldc.i4.s   100
    ret

FAILED:
    ldc.i4.s   42
    ret
  }
}