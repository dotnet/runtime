
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }

.assembly 'library' { }

.class public auto ansi abstract sealed beforefieldinit Library
    extends [mscorlib]System.Object
{
    .class interface nested public auto ansi abstract beforefieldinit I0
    {
    } // end of class I0

    .class interface nested public auto ansi abstract beforefieldinit I00
        implements Library/I0
    {
    } // end of class I00

    .class interface nested public auto ansi abstract beforefieldinit I01
        implements Library/I0
    {
    } // end of class I01

    .class interface nested public auto ansi abstract beforefieldinit I000
        implements Library/I00
    {
    } // end of class I000

    .class interface nested public auto ansi abstract beforefieldinit I010
        implements Library/I01
    {
    } // end of class I010

    .class interface nested public auto ansi abstract beforefieldinit I0100
        implements Library/I010
    {
    } // end of class I010

    .class nested public auto ansi beforefieldinit MyClass
        extends [mscorlib]System.Object
        implements Library/I0100,
                   Library/I000
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

} // end of class Library
