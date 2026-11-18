// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Private.CoreLib
{
}

// Deliberately unresolved. This AssemblyRef is also used by the ExportedType below.
.assembly extern ILVerifyAssemblyThatDoesNotExist
{
}

.assembly UnusedMetadataReferenceTests
{
}

.module extern ILVerifyManagedModuleThatDoesNotExist.netmodule
.module extern ValidMetadataReferenceModule.netmodule

.file ValidMetadataReferenceModule.netmodule

.class extern forwarder MissingForwardedType
{
    .assembly extern ILVerifyAssemblyThatDoesNotExist
}

// Provides a resolvable TypeRef to System.Object and contains the P/Invoke method whose
// native-library ModuleRef must not be treated as a managed netmodule dependency.
.class public auto ansi beforefieldinit UnusedMetadataReferenceTestsType
       extends [System.Private.CoreLib]System.Object
{
    .method public static pinvokeimpl("ILVerifyNativeLibraryThatDoesNotExist" as "NativeMethod")
            void NativeMethod() cil managed preservesig
    {
    }
}
