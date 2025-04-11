// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly ASSEMBLY_NAME { }
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary { .ver 0:0:0:0 }
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
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = {}
.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {
    string('https://github.com/dotnet/runtime/issues/91923')
    type([TestLibrary]TestLibrary.PlatformDetection)
    string[1] ('IsAppleMobile')
}
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
