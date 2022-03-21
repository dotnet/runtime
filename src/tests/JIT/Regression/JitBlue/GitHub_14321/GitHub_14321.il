// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {auto}
.assembly extern System.Console {auto}
.assembly GitHub_14321 {}
.assembly extern xunit.core {}

.class Program
{
    .method static bool TestUnsigned(int32) noinlining
    {
		.maxstack 2

		ldarg.0
		ldc.i4 42
		sub
		ldc.i4 0
		clt.un
		ret
    }

    .method static bool TestSignedOverflow(int32) noinlining
    {
		.maxstack 2

        ldarg.0
        ldc.i4 42
        add
        ldc.i4 0
        cgt
		ret
    }

    .method static int32 Main() 
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .maxstack 1

        ldc.i4 42
        call bool Program::TestUnsigned(int32)
		brtrue FAIL
        ldc.i4 0x7fffffff
        call bool Program::TestSignedOverflow(int32)
		brtrue FAIL
        ldstr "PASS"
        call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 100
        ret
    FAIL:
        ldstr "FAIL"
        call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 1
        ret
    }
}
