// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }

.assembly 'library' { }

.class public auto ansi abstract sealed beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Nested Types
    .class interface nested public auto ansi abstract beforefieldinit I0
    {
        // Methods
        .method public hidebysig newslot abstract virtual
            instance void Method () cil managed
        {
        } // end of method I0::Method

    } // end of class I0

    .class interface nested public auto ansi abstract beforefieldinit I00
        implements Program/I0
    {
        // Methods
        .method public final hidebysig virtual
            instance void Program.I0.Method () cil managed
        {
            .override method instance void Program/I0::Method()
            // Method begins at RVA 0x2068
            // Code size 2 (0x2)
            .maxstack 8

            IL_0000: nop
            IL_0001: ret
        } // end of method I00::Program.I0.Method
    } // end of class I00

    .class interface nested public auto ansi abstract beforefieldinit I01
        implements Program/I0
    {
        // Methods
        .method public final hidebysig virtual
            instance void Program.I0.Method () cil managed
        {
            .override method instance void Program/I0::Method()
            // Method begins at RVA 0x2068
            // Code size 2 (0x2)
            .maxstack 8

            IL_0000: nop
            IL_0001: ret
        } // end of method I00::Program.I0.Method
    } // end of class I00

    .class interface nested public auto ansi abstract beforefieldinit I000
        implements Program/I00
    {
    } // end of class I000

    .class interface nested public auto ansi abstract beforefieldinit I010
        implements Program/I01
    {
    } // end of class I000

    .class nested public auto ansi beforefieldinit MyFoo
        extends [mscorlib]System.Object
        implements Program/I000, Program/I010
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

} // end of class Program
