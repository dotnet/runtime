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
.method public static int32 Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.locals init (typedref V_0)

ldloc.0
refanytype
call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
brfalse.s NON_NULL_CASE
ldc.i4.m1
ret

NON_NULL_CASE:
call int32 f()
mkrefany int32
refanytype
call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
ldtoken int32
call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
beq.s SUCCESS
ldc.i4.m1
ret

SUCCESS:
ldc.i4 100
ret
}
}
