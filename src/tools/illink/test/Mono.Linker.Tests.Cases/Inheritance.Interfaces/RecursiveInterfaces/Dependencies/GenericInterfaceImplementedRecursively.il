// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }

.assembly 'library' { }

.class public auto ansi abstract sealed beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Nested Types
    .class interface nested public auto ansi abstract beforefieldinit IBase`1<T>
    {
    } // end of class IBase

    .class interface nested public auto ansi abstract beforefieldinit IMiddle`1<T>
        implements class Program/IBase`1<!T>
    {
    } // end of class IMiddle

    .class interface nested public auto ansi abstract beforefieldinit IDerived`1<T>
        implements class Program/IMiddle`1<!T>
    {
    } // end of class IDerived

    .class nested public auto ansi beforefieldinit C
        extends [mscorlib]System.Object
        implements class Program/IDerived`1<int32>
    {
        // Methods
        .method public hidebysig specialname rtspecialname
            instance void .ctor () cil managed
        {
            // Method begins at RVA 0x2066
            // Code size 8 (0x8)
            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: nop
            IL_0007: ret
        } // end of method C::.ctor

    } // end of class C
} // end of class Program
