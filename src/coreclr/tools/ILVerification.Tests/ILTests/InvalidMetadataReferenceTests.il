// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Private.CoreLib
{
}

.assembly extern ILVerifyAssemblyThatDoesNotExist
{
}

.assembly InvalidMetadataReferenceTests
{
}

.class public auto ansi abstract sealed beforefieldinit InvalidMetadataReferenceTestsType
       extends [System.Private.CoreLib]System.Object
{
    .method private static void MissingTypeInMissingAssembly() cil managed
    {
        ldnull
        castclass [ILVerifyAssemblyThatDoesNotExist]ILVerifyTypeInMissingAssembly
        pop
        ret
    }

    .method private static void MissingTypeInResolvedAssembly() cil managed
    {
        ldnull
        castclass [System.Private.CoreLib]ILVerifyTypeThatDoesNotExist
        pop
        ret
    }

    .method private static void MissingMethodReference() cil managed
    {
        call void [System.Private.CoreLib]System.Object::ILVerifyMethodThatDoesNotExist()
        ret
    }

    .method private static void MissingFieldReference() cil managed
    {
        ldsfld int32 [System.Private.CoreLib]System.String::ILVerifyFieldThatDoesNotExist
        pop
        ret
    }

    .method private static void MissingTypeSpecification() cil managed
    {
        ldnull
        castclass class [System.Private.CoreLib]System.Collections.Generic.List`1<
            class [System.Private.CoreLib]ILVerifyTypeSpecTypeThatDoesNotExist>
        pop
        ret
    }

    .method private static void GenericMethod<T>() cil managed
    {
        ret
    }

    .method private static void MissingMethodSpecification() cil managed
    {
        call void InvalidMetadataReferenceTestsType::GenericMethod<
            class [System.Private.CoreLib]ILVerifyMethodSpecTypeThatDoesNotExist>()
        ret
    }

    .method private static void MissingStandaloneMethodSignature() cil managed
    {
        ldnull
        calli void(class [System.Private.CoreLib]ILVerifyStandaloneMethodTypeThatDoesNotExist)
        ret
    }

    .method private static void MissingStandaloneLocalSignature() cil managed
    {
        .locals init (
            [0] class [System.Private.CoreLib]ILVerifyStandaloneLocalTypeThatDoesNotExist
        )

        ret
    }
}
