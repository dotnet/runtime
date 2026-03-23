// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { auto }
.assembly extern xunit.core {}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }

.assembly 'TypeIntrinsics_il' { }

.class public auto ansi beforefieldinit Test_TypeIntrinsics_il
       extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, valuetype [Microsoft.DotNet.XUnitExtensions]Xunit.TestRuntimes) = {string('https://github.com/dotnet/runtime/issues/54867') int32(2)}
    .entrypoint
    .maxstack  1
    // it's not currently possible to produce `ldtoken [type]&` in C#
    ldtoken    string&
    call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    call       instance bool [System.Runtime]System.Type::get_IsValueType()
    brtrue.s   FAILED

    ldtoken    valuetype [System.Runtime]System.Int16&
    call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    call       instance bool [System.Runtime]System.Type::get_IsValueType()
    brtrue.s   FAILED

    ldtoken    object&
    call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    call       instance bool [System.Runtime]System.Type::get_IsValueType()
    brtrue.s   FAILED

    ldtoken    valuetype [System.Runtime]System.Decimal&
    call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    call       instance bool [System.Runtime]System.Type::get_IsValueType()
    brtrue.s   FAILED

    ldc.i4.s   100
    ret
FAILED:
    ldc.i4.s   42
    ret
  }
}
