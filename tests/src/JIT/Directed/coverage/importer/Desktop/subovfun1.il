// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


.assembly extern legacy library mscorlib {}
.assembly subovfun1 {.custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 01 00 00 ) }
.method public static uint32 f(uint32 arg)
{
ldarg.0
ldc.i4 1
sub.ovf.un
ret
}
.method public static int32 Main()
{
.entrypoint
ldc.i4 101
call uint32 f(uint32)
conv.i4
ret
}