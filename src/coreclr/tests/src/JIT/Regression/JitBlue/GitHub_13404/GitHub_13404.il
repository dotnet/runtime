// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib {auto}
.assembly extern System.Console {auto}
.assembly GitHub_13404 {}

.class Program
{
    .field static int8 s8;

    .method static void Test() noinlining
    {
        ldsfld int8 Program::s8
        conv.ovf.i2.un
        pop
        ret
    }

    .method static int32 Main() 
    {
        .entrypoint
        .maxstack 1

        ldc.i4 -1
        stsfld int8 Program::s8

        .try
        {
            call void Program::Test()
            leave FAIL
        }
        catch [mscorlib]System.OverflowException
        {
            pop
            leave PASS
        }
    FAIL:
        ldstr "FAIL"
        call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 1
        ret
    PASS: 
        ldstr "PASS"
        call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 100
        ret
    }
}
