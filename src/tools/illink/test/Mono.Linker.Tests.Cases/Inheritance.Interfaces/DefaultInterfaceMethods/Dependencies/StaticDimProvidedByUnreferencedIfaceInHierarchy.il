// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }

.assembly 'library' { }

.class public auto ansi abstract sealed beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Nested Types
    .class interface nested public auto ansi abstract beforefieldinit IBase
    {
        // Methods
        .method public hidebysig abstract virtual static
            void Method () cil managed
        {
        } // end of method IBase::Method

    } // end of class IBase

    .class interface nested public auto ansi abstract beforefieldinit I2
        implements Program/IBase
    {
        // Methods
        .method public hidebysig static
            void Program.IBase.Method () cil managed
        {
            .override method void Program/IBase::Method()
            // Method begins at RVA 0x205f
            // Code size 2 (0x2)
            .maxstack 8

            IL_0000: nop
            IL_0001: ret
        } // end of method I2::Program.IBase.Method

    } // end of class I2

    .class interface nested public auto ansi abstract beforefieldinit I3
        implements Program/I2
    {
    } // end of class I3

    .class interface nested public auto ansi abstract beforefieldinit I4
        implements Program/I3
    {
    } // end of class I4


    // Methods
    .method public hidebysig static
        void CallMethod<(Program/IBase) T> () cil managed
    {
        .param constraint T, Program/IBase
            .custom instance void [mscorlib]mscorlib.CompilerServices.NullableAttribute::.ctor(uint8) = (
                01 00 01 00 00
            )
        // Method begins at RVA 0x2050
        // Code size 14 (0xe)
        .maxstack 8

        IL_0000: nop
        IL_0001: constrained. !!T
        IL_0007: call void Program/IBase::Method()
        IL_000c: nop
        IL_000d: ret
    } // end of method Program::CallMethod

} // end of class Program
