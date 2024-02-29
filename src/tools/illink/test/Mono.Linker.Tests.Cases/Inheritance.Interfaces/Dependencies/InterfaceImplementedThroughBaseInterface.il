// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }

.assembly 'library' { }

.class interface public auto ansi abstract beforefieldinit IBase
{
    // Methods
    .method public hidebysig newslot abstract virtual 
        instance void M () cil managed 
    {
    } // end of method IBase::M

} // end of class IBase

.class interface public auto ansi abstract beforefieldinit IDerived
    implements IBase
{
} // end of class IDerived

.class public auto ansi beforefieldinit C
    extends [System.Runtime]System.Object
    implements IDerived
{
    // Methods
    .method private final hidebysig newslot virtual 
        instance void IBase.M () cil managed 
    {
        .override method instance void IBase::M()
        // Method begins at RVA 0x2050
        // Code size 2 (0x2)
        .maxstack 8

        IL_0001: ret
    } // end of method C::IBase.M

    .method public hidebysig specialname rtspecialname 
        instance void .ctor () cil managed 
    {
        // Method begins at RVA 0x2053
        // Code size 8 (0x8)
        .maxstack 8

        IL_0007: ret
    } // end of method C::.ctor
}
