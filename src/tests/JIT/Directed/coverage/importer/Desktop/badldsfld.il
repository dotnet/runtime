// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly ASSEMBLY_NAME { }
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
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
// [ActiveIssue("https://github.com/dotnet/runtimelab/issues/", typeof(PlatformDetection), nameof(PlatformDetection.IsAppleMobile))]
.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string,
                                                                                        class [System.Runtime]System.Type,
                                                                                        string[]) = (01 00 2F 68 74 74 70 73 3A 2F 2F 67 69 74 68 75
                                                                                                    62 2E 63 6F 6D 2F 64 6F 74 6E 65 74 2F 72 75 6E
                                                                                                    74 69 6D 65 6C 61 62 2F 69 73 73 75 65 73 2F 61
                                                                                                    54 65 73 74 4C 69 62 72 61 72 79 2E 50 6C 61 74
                                                                                                    66 6F 72 6D 44 65 74 65 63 74 69 6F 6E 2C 20 54
                                                                                                    65 73 74 4C 69 62 72 61 72 79 2C 20 56 65 72 73
                                                                                                    69 6F 6E 3D 30 2E 30 2E 30 2E 30 2C 20 43 75 6C
                                                                                                    74 75 72 65 3D 6E 65 75 74 72 61 6C 2C 20 50 75
                                                                                                    62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D 6E 75 6C
                                                                                                    6C 01 00 00 00 24 49 73 41 70 70 6C 65 4D 6F 62
                                                                                                    69 6C 65 00 00 00 )
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
