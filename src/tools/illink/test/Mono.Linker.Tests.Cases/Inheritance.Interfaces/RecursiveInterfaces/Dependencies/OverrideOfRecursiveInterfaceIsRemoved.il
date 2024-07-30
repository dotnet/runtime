// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }

.assembly 'library' { }

.class public auto ansi beforefieldinit Program
    extends [mscorlib]System.Object
{
    // Nested Types
    .class interface nested public auto ansi abstract beforefieldinit IBaseUnused
    {
        // Methods
        .method public hidebysig abstract virtual static 
            void M () cil managed 
        {
        } // end of method IBaseUnused::M

    } // end of class IBaseUnused

    .class interface nested public auto ansi abstract beforefieldinit IBaseUsed
    {
        // Methods
        .method public hidebysig abstract virtual static 
            void M () cil managed 
        {
        } // end of method IBaseUsed::M

    } // end of class IBaseUsed

    .class interface nested private auto ansi abstract beforefieldinit IMiddleUnused
        implements Program/IBaseUnused,
                   Program/IBaseUsed
    {
        // Methods
        .method public hidebysig abstract virtual static 
            void O () cil managed 
        {
        } // end of method IMiddleUnused::O

    } // end of class IMiddleUnused

    .class interface nested private auto ansi abstract beforefieldinit IDerived
        implements Program/IMiddleUnused
    {
        // Methods
        .method public hidebysig abstract virtual static 
            void N () cil managed 
        {
        } // end of method IDerived::N

    } // end of class IDerived

    .class nested private auto ansi beforefieldinit A
        extends [mscorlib]System.Object
        implements Program/IDerived
    {
        // Methods
        .method public hidebysig static 
            void M () cil managed 
        {
            .override method void Program/IBaseUnused::M()
			.override method void Program/IBaseUsed::M()
            // Method begins at RVA 0x2083
            // Code size 1 (0x1)
            .maxstack 8

            IL_0000: ret
        } // end of method A::M

        .method public hidebysig static 
            void N () cil managed 
        {
            .override method void Program/IDerived::N()
            // Method begins at RVA 0x2083
            // Code size 1 (0x1)
            .maxstack 8

            IL_0000: ret
        } // end of method A::N

        .method public hidebysig static 
            void O () cil managed 
        {
            .override method void Program/IMiddleUnused::O()
            // Method begins at RVA 0x2083
            // Code size 1 (0x1)
            .maxstack 8

            IL_0000: ret
        } // end of method A::O

        .method public hidebysig specialname rtspecialname 
            instance void .ctor () cil managed 
        {
            // Method begins at RVA 0x207b
            // Code size 7 (0x7)
            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ret
        } // end of method A::.ctor

    } // end of class A


    // Methods
    .method public hidebysig static 
        void MyTest () cil managed 
    {
        // Method begins at RVA 0x2050
        // Code size 16 (0x10)
        .maxstack 8

        IL_0000: call void Program::UseNThroughIDerived<class Program/A>()
        IL_0005: call void Program::UseMThroughIDerived<class Program/A>()
        IL_000a: call void Program/A::M()
        IL_000f: call void Program/A::O()
        IL_0014: ret
    } // end of method Program::MyTest

    .method private hidebysig static 
        void UseNThroughIDerived<(Program/IDerived) T> () cil managed 
    {
        .param constraint T, Program/IDerived
            .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                01 00 01 00 00
            )
        // Method begins at RVA 0x2061
        // Code size 12 (0xc)
        .maxstack 8

        IL_0000: constrained. !!T
        IL_0006: call void Program/IDerived::N()
        IL_000b: ret
    } // end of method Program::UseNThroughIDerived

    .method private hidebysig static 
        void UseMThroughIDerived<(Program/IBaseUsed) T> () cil managed 
    {
        .param constraint T, Program/IBaseUnused
            .custom instance void [mscorlib]System.Runtime.CompilerServices.NullableAttribute::.ctor(uint8) = (
                01 00 01 00 00
            )
        // Method begins at RVA 0x206e
        // Code size 12 (0xc)
        .maxstack 8

        IL_0000: constrained. !!T
        IL_0006: call void Program/IBaseUsed::M()
        IL_000b: ret
    } // end of method Program::UseMThroughIDerived

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x207b
        // Code size 7 (0x7)
        .maxstack 8

        IL_0000: ldarg.0
        IL_0001: call instance void [mscorlib]System.Object::.ctor()
        IL_0006: ret
    } // end of method Program::.ctor

} // end of class Program
