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
.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [System.Runtime]System.Type, string[]) = (
			01 00 30 68 74 74 70 73 3a 2f 2f 67 69 74 68 75
			62 2e 63 6f 6d 2f 64 6f 74 6e 65 74 2f 72 75 6e
			74 69 6d 65 6c 61 62 2f 69 73 73 75 65 73 2f 54
			4f 44 4f 61 54 65 73 74 4c 69 62 72 61 72 79 2e
			50 6c 61 74 66 6f 72 6d 44 65 74 65 63 74 69 6f
			6e 2c 20 54 65 73 74 4c 69 62 72 61 72 79 2c 20
			56 65 72 73 69 6f 6e 3d 30 2e 30 2e 30 2e 30 2c
			20 43 75 6c 74 75 72 65 3d 6e 65 75 74 72 61 6c
			2c 20 50 75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e
			3d 6e 75 6c 6c 01 00 00 00 0d 49 73 41 70 70 6c
			65 4d 6f 62 69 6c 65 00 00
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
