
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }

.assembly 'library' { }

.class public auto ansi abstract sealed beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Nested Types
    .class interface nested public auto ansi abstract beforefieldinit IFoo
    {
        // Methods
        .method public hidebysig newslot abstract virtual
            instance void Method () cil managed
        {
        } // end of method IFoo::Method

    } // end of class IFoo

    .class interface nested public auto ansi abstract beforefieldinit IBar
        implements Program/IFoo
    {
        // Methods
        .method public final hidebysig virtual
            instance void Program.IFoo.Method () cil managed
        {
            .override method instance void Program/IFoo::Method()
            // Method begins at RVA 0x2068
            // Code size 2 (0x2)
            .maxstack 8

            IL_0000: nop
            IL_0001: ret
        } // end of method IBar::Program.IFoo.Method

    } // end of class IBar

    .class interface nested public auto ansi abstract beforefieldinit IBaz
        implements Program/IBar
    {
    } // end of class IBaz

    .class nested public auto ansi beforefieldinit MyFoo
        extends [mscorlib]System.Object
        implements Program/IBaz
    {
        // Methods
        .method public hidebysig specialname rtspecialname
            instance void .ctor () cil managed
        {
            // Method begins at RVA 0x2076
            // Code size 8 (0x8)
            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: nop
            IL_0007: ret
        } // end of method MyFoo::.ctor

    } // end of class MyFoo


    // Methods
    .method public hidebysig static
        void CallMethod (
            class Program/IFoo foo
        ) cil managed
    {
        .custom instance void [mscorlib]mscorlib.CompilerServices.NullableContextAttribute::.ctor(uint8) = (
            01 00 01 00 00
        )
        // Method begins at RVA 0x2050
        // Code size 9 (0x9)
        .maxstack 8

        IL_0000: nop
        IL_0001: ldarg.0
        IL_0002: callvirt instance void Program/IFoo::Method()
        IL_0007: nop
        IL_0008: ret
    } // end of method Program::CallMethod

} // end of class Program
