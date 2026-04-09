// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }

.assembly 'library' { }

.class public auto ansi abstract sealed beforefieldinit Program
    extends [System.Runtime]System.Object
{
    // Nested Types
    .class interface nested public auto ansi abstract beforefieldinit IBase
    {
    } // end of class IBase

    .class interface nested public auto ansi abstract beforefieldinit IMiddle
        implements Program/IBase
    {
    } // end of class IMiddle

    .class interface nested public auto ansi abstract beforefieldinit IDerived
        implements Program/IMiddle
    {
    } // end of class IDerived

    .class nested public auto ansi beforefieldinit C
        extends [System.Runtime]System.Object
        implements Program/IDerived
    {
        // Methods
        .method public hidebysig specialname rtspecialname
            instance void .ctor () cil managed
        {
            // Method begins at RVA 0x2066
            // Code size 8 (0x8)
            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void [System.Runtime]System.Object::.ctor()
            IL_0006: nop
            IL_0007: ret
        } // end of method C::.ctor

    } // end of class C


    // Methods
    .method public hidebysig static
        void Main () cil managed
    {
        // Method begins at RVA 0x2050
        // Code size 10 (0xa)
        .maxstack 1
        .locals init (
            [0] class Program/IBase b,
            [1] class Program/C c
        )

        IL_0000: nop
        IL_0001: ldnull
        IL_0002: stloc.0
        IL_0003: newobj instance void Program/C::.ctor()
        IL_0008: stloc.1
        IL_0009: ret
    } // end of method Program::Main

} // end of class Program
