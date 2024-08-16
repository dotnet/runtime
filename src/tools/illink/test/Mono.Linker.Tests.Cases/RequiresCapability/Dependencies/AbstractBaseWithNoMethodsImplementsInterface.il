// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern 'mscorlib' { }

.assembly 'library' { }

.class public auto ansi beforefieldinit AbstractBaseWithNoMethodsImplementsInterface
    extends [mscorlib]System.Object
{
    // Methods
    .method public hidebysig static
        void Run () cil managed
    {
        .custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute::.ctor(string) = (
            01 00 03 52 55 43 00 00
        )
        // Method begins at RVA 0x2050
        // Code size 16 (0x10)
        .maxstack 8

        IL_0000: newobj instance void AbstractBaseWithNoMethodsImplementsInterface/Derived::.ctor()
        IL_0005: castclass AbstractBaseWithNoMethodsImplementsInterface/I
        IL_000a: callvirt instance void AbstractBaseWithNoMethodsImplementsInterface/I::M()
        IL_000f: ret
    } // end of method Program::Main

    .method public hidebysig specialname rtspecialname
        instance void .ctor () cil managed
    {
        // Method begins at RVA 0x2061
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    } // end of method Program::.ctor


    // Nested Types
    .class nested assembly auto ansi abstract beforefieldinit Base
        extends [mscorlib]System.Object
        implements AbstractBaseWithNoMethodsImplementsInterface/I
    {
        // Methods
        .method family hidebysig specialname rtspecialname
            instance void .ctor () cil managed
        {
            // Method begins at RVA 0x2061
            // Code size 7 (0x7)
            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ret
        } // end of method Base::.ctor

    } // end of class Base

    .class interface nested assembly auto ansi abstract beforefieldinit I
    {
        // Methods
        .method public hidebysig newslot abstract virtual
            instance void M () cil managed
        {
            .custom instance void [mscorlib]System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute::.ctor(string) = (
                01 00 03 52 55 43 00 00 // "RUC"
            )
        } // end of method I::M

    } // end of class I

    .class nested assembly auto ansi beforefieldinit Derived
        extends AbstractBaseWithNoMethodsImplementsInterface/Base
    {
        // Methods
        .method public virtual hidebysig
            instance void M () cil managed
        {
            // Method begins at RVA 0x2069
            .maxstack 8

            IL_0000: ret
        } // end of method Derived::M

        .method public hidebysig specialname rtspecialname
            instance void .ctor () cil managed
        {
            // Method begins at RVA 0x2075
            // Code size 7 (0x7)
            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void AbstractBaseWithNoMethodsImplementsInterface/Base::.ctor()
            IL_0006: ret
        } // end of method Derived::.ctor

    } // end of class Derived

} // end of class Program
