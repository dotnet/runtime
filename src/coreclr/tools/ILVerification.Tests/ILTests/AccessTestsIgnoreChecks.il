// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly extern AccessTests
{
}

.assembly AccessTestsIgnoreChecks
{
    // IgnoresAccessChecksTo("AccessTests")
    .custom instance void System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute::.ctor(string) = (
        01 00 0b 41 63 63 65 73 73 54 65 73 74 73 00 00
    )
}

// The IgnoresAccessChecksToAttribute is not part of the BCL surface area; consumers of the
// feature (e.g. the IgnoresAccessChecksToGenerator) declare it in their own assembly. The
// runtime and ilverify recognize it purely by its namespace and name.
.class private auto ansi sealed beforefieldinit System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute
        extends [System.Runtime]System.Attribute
{
    .method public hidebysig specialname rtspecialname instance void .ctor(string assemblyName) cil managed
    {
        ldarg.0
        call        instance void [System.Runtime]System.Attribute::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit AccessTestsIgnoreChecksType
        extends [System.Runtime]System.Object
{
    .method public hidebysig instance void Instantiate.PrivateClass_Valid() cil managed
    {
        newobj      instance void [AccessTests]PrivateClass::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Instantiate.PrivateNested_Valid() cil managed
    {
        newobj      instance void [AccessTests]SimpleClass/PrivateNestedClass::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Instantiate.AssemblyNested_Valid() cil managed
    {
        newobj      instance void [AccessTests]SimpleClass/AssemblyNestedClass::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Instantiate.FamilyNested_Valid() cil managed
    {
        newobj      instance void [AccessTests]SimpleClass/FamilyNestedClass::.ctor()
        pop
        ret
    }

    .method public hidebysig instance void Call.PrivateMethod_Valid() cil managed
    {
        call      void [AccessTests]SimpleClass::PrivateMethod()
        ret
    }

    .method public hidebysig instance void Call.FamilyMethod_Valid() cil managed
    {
        call      void [AccessTests]SimpleClass::FamilyMethod()
        ret
    }

    .method public hidebysig instance void Call.AssemblyMethod_Valid() cil managed
    {
        call      void [AccessTests]SimpleClass::AssemblyMethod()
        ret
    }

    .method public hidebysig instance void Call.FamAndAssemMethod_Valid() cil managed
    {
        call      void [AccessTests]SimpleClass::FamilyAndAssemblyMethod()
        ret
    }

    .method public hidebysig instance void Load.PrivateField_Valid() cil managed
    {
        ldsfld      int32 [AccessTests]SimpleClass::privateField
        pop
        ret
    }

    .method public hidebysig instance void Load.AssemblyField_Valid() cil managed
    {
        ldsfld      int32 [AccessTests]SimpleClass::assemblyField
        pop
        ret
    }

    .method public hidebysig instance void Load.PublicFieldOfPrivateClass_Valid() cil managed
    {
        ldsfld      int32 [AccessTests]SimpleClass/PrivateNestedClass::publicField
        pop
        ret
    }
}
