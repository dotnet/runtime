// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/134129 - a calli
// with a vararg call site signature used to crash the mono interpreter.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary { .ver 0:0:0:0 }
.assembly 'vararg_calli_interp' {}

.class public auto ansi beforefieldinit VarargCalli extends [System.Runtime]System.Object
{
    .method public static vararg void Target(int32 fixedArg) cil managed
    {
        .maxstack 1
        ret
    }

    // The calli needs a method of its own: the interpreter reports the error
    // while transforming the method that contains it, that is on entry to it,
    // so the handler cannot sit in the same method.
    .method public static void DoVarargCalli() cil managed
    {
        .maxstack 3
        ldc.i4.1
        ldc.i4.2
        ldftn      vararg void VarargCalli::Target(int32)
        calli      vararg void(int32, ..., int32)
        ret
    }

    .method public static int32 TestEntryPoint() cil managed
    {
        .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type, string[]) = {
            type([TestLibrary]TestLibrary.PlatformDetection)
            string[1] ('IsMonoInterpreter')
        }
        .entrypoint
        .maxstack 2

        .try
        {
            call       void VarargCalli::DoVarargCalli()
            leave.s    FAILED
        }
        catch [System.Runtime]System.NotSupportedException
        {
            pop
            leave.s    PASSED
        }

    FAILED:
        ldstr      "Expected NotSupportedException from a calli with a vararg signature"
        newobj     instance void [System.Runtime]System.Exception::.ctor(string)
        throw

    PASSED:
        ldc.i4     100
        ret
    }
}
