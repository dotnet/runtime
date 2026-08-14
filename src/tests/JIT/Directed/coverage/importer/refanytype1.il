// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly refanytype1 {}
.method public static int32 f()
{
ldc.i4 100
ret
}
.class public auto ansi refanytype1 {
.method private static bool IsInt32(valuetype [mscorlib]System.RuntimeTypeHandle handle) noinlining
{
ldarga.s handle
ldtoken int32
call instance bool valuetype [mscorlib]System.RuntimeTypeHandle::Equals(valuetype [mscorlib]System.RuntimeTypeHandle)
ret
}
.method private static bool IsDefault(valuetype [mscorlib]System.RuntimeTypeHandle handle) noinlining
{
.locals init (valuetype [mscorlib]System.RuntimeTypeHandle defaultHandle)
ldarga.s handle
ldloc defaultHandle
call instance bool valuetype [mscorlib]System.RuntimeTypeHandle::Equals(valuetype [mscorlib]System.RuntimeTypeHandle)
ret
}
.method public static int32 Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.locals init (typedref empty)
.entrypoint
call int32 f()
mkrefany int32
refanytype
call bool refanytype1::IsInt32(valuetype [mscorlib]System.RuntimeTypeHandle)
brtrue.s NON_NULL_PASS
ldc.i4.m1
ret
NON_NULL_PASS:
ldloc empty
refanytype
call bool refanytype1::IsDefault(valuetype [mscorlib]System.RuntimeTypeHandle)
brtrue.s RAW_NULL_PASS
ldc.i4.s -2
ret
RAW_NULL_PASS:
ldloc empty
// GetTargetType contains refanytype followed by Type.GetTypeFromHandle, which the importer
// fuses into CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL.
call class [mscorlib]System.Type [mscorlib]System.TypedReference::GetTargetType(typedref)
brfalse.s PASS
ldc.i4.s -3
ret
PASS:
ldc.i4 100
ret
}
}
