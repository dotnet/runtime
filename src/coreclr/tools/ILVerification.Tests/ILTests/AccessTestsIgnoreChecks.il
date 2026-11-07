// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly extern AccessTests
{
}

.assembly extern AccessTestsFriend
{
}

.assembly AccessTestsIgnoreChecks
{
    // IgnoresAccessChecksTo("AccessTests")
    .custom instance void System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute::.ctor(string) = (
        01 00 0b 41 63 63 65 73 73 54 65 73 74 73 00 00
    )

    // IgnoresAccessChecksTo("AccessTestsFriend, PublicKey=00000000000000000400000000000000")
    //                                           ^ AccessTestsFriend has no public key
    .custom instance void System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute::.ctor(string) = (
        01 00 3d 41 63 63 65 73 73 54 65 73 74 73 46 72
        69 65 6e 64 2c 20 50 75 62 6c 69 63 4b 65 79 3d
        30 30 30 30 30 30 30 30 30 30 30 30 30 30 30 30
        30 34 30 30 30 30 30 30 30 30 30 30 30 30 30 30
        00 00
    )
}

.class private auto ansi beforefieldinit System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute
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

    .method public hidebysig instance void Call.PrivateMethod_Valid() cil managed
    {
        call      void [AccessTests]SimpleClass::PrivateMethod()
        ret
    }

    .method public hidebysig instance void Load.PrivateField_Valid() cil managed
    {
        ldsfld      int32 [AccessTests]SimpleClass::privateField
        pop
        ret
    }

    .method public hidebysig instance void Load.PrivateFieldWrongPublicKey_Invalid_FieldAccess() cil managed
    {
        ldsfld      int32 [AccessTestsFriend]AccessTestsFriendType::privateField
        pop
        ret
    }
}
