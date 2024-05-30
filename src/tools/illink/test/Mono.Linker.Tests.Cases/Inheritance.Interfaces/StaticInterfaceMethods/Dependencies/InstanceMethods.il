// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }

.assembly 'library' { }

.class public auto ansi abstract sealed beforefieldinit InstanceMethods
    extends [mscorlib]System.Object
{
    // Nested Types
    .class nested public auto ansi beforefieldinit TypeWithMethodAccessedViaInterface
        extends [mscorlib]System.Object
        implements InstanceMethods/IInt
    {
        // Methods
        .method private final hidebysig newslot virtual
            instance int32 GetInt () cil managed
        {
            .override method instance int32 InstanceMethods/IInt::GetInt()
            // Method begins at RVA 0x2070
            // Code size 2 (0x2)
            .maxstack 8

            IL_0000: ldc.i4.0
            IL_0001: ret
        } // end of method TypeWithMethodAccessedViaInterface::GetInt

        .method public hidebysig specialname rtspecialname
            instance void .ctor () cil managed
        {
            // Method begins at RVA 0x2068
            // Code size 7 (0x7)
            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ret
        } // end of method TypeWithMethodAccessedViaInterface::.ctor

    } // end of class TypeWithMethodAccessedViaInterface

    .class nested public auto ansi beforefieldinit TypeWithMethodAccessedDirectly
        extends [mscorlib]System.Object
        implements InstanceMethods/IInt
    {
        // Methods
        .method public final hidebysig newslot virtual
            instance int32 GetInt () cil managed
        {
            .override method instance int32 InstanceMethods/IInt::GetInt()
            // Method begins at RVA 0x2070
            // Code size 2 (0x2)
            .maxstack 8

            IL_0000: ldc.i4.0
            IL_0001: ret
        } // end of method TypeWithMethodAccessedDirectly::GetInt

        .method public hidebysig specialname rtspecialname
            instance void .ctor () cil managed
        {
            // Method begins at RVA 0x2068
            // Code size 7 (0x7)
            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ret
        } // end of method TypeWithMethodAccessedDirectly::.ctor

    } // end of class TypeWithMethodAccessedDirectly

    .class nested public auto ansi beforefieldinit TypeWithMethodAccessedViaReflection
        extends [mscorlib]System.Object
        implements InstanceMethods/IInt
    {
        // Methods
        .method public final hidebysig newslot virtual
            instance int32 GetInt () cil managed
        {
            .override method instance int32 InstanceMethods/IInt::GetInt()
            // Method begins at RVA 0x2070
            // Code size 2 (0x2)
            .maxstack 8

            IL_0000: ldc.i4.0
            IL_0001: ret
        } // end of method TypeWithMethodAccessedViaReflection::GetInt

        .method public hidebysig specialname rtspecialname
            instance void .ctor () cil managed
        {
            // Method begins at RVA 0x2068
            // Code size 7 (0x7)
            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ret
        } // end of method TypeWithMethodAccessedViaReflection::.ctor

    } // end of class TypeWithMethodAccessedViaReflection

    .class nested public auto ansi beforefieldinit TypeWithMethodKeptByDynamicDependency
        extends [mscorlib]System.Object
        implements InstanceMethods/IInt
    {
        // Methods
        .method public final hidebysig newslot virtual
            instance int32 GetInt () cil managed
        {
            .override method instance int32 InstanceMethods/IInt::GetInt()
            // Method begins at RVA 0x2070
            // Code size 2 (0x2)
            .maxstack 8

            IL_0000: ldc.i4.0
            IL_0001: ret
        } // end of method TypeWithMethodKeptByDynamicDependency::GetInt

        .method public hidebysig specialname rtspecialname
            instance void .ctor () cil managed
        {
            // Method begins at RVA 0x2068
            // Code size 7 (0x7)
            .maxstack 8

            IL_0000: ldarg.0
            IL_0001: call instance void [mscorlib]System.Object::.ctor()
            IL_0006: ret
        } // end of method TypeWithMethodKeptByDynamicDependency::.ctor

    } // end of class TypeWithMethodKeptByDynamicDependency

    .class interface nested public auto ansi abstract beforefieldinit IInt
    {
        // Methods
        .method public hidebysig newslot abstract virtual
            instance int32 GetInt () cil managed
        {
        } // end of method IInt::GetInt

    } // end of class IInt


    // Methods
    .method public hidebysig static
        void Test () cil managed
    {
        // Method begins at RVA 0x2050
        // Code size 23 (0x17)
        .maxstack 8

        IL_0000: newobj instance void InstanceMethods/TypeWithMethodAccessedViaInterface::.ctor()
        IL_0005: callvirt instance int32 InstanceMethods/IInt::GetInt()
        IL_000a: pop
        IL_000b: newobj instance void InstanceMethods/TypeWithMethodAccessedDirectly::.ctor()
        IL_0010: call instance int32 InstanceMethods/TypeWithMethodAccessedDirectly::GetInt()
        IL_0015: pop
        IL_0016: ret
    } // end of method InstanceMethods::Test
} // end of class InstanceMethods
