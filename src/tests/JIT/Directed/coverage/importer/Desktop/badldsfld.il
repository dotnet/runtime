// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly ASSEMBLY_NAME { }
.class public Test_badldsfld extends [mscorlib]System.Object
{
.field int32 i
.method static int32 f()
{
ldsfld int32 Test_badldsfld::i
ret
}
.method public static int32 Main()
{
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.try
{
call int32 Test_badldsfld::f()
leave.s FAIL
}
catch [mscorlib]System.InvalidProgramException
{
leave.s PASS
}
PASS:
ldc.i4 100
ret
FAIL:
ldc.i4 1
ret
}
}
