// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.Runtime.TypeLoader
{
    public enum MetadataFixupKind
    {
        // Metadata fixups that apply to type tokens
        TypeHandle = 0x0,
        GcStaticData = 0x1,
        NonGcStaticData = 0x2,
        UnwrapNullableType = 0x3,
        TypeSize = 0x4,
        AllocateObject = 0x5,
        DefaultConstructor = 0x6,
        // unused = 0x7,
        // unused = 0x8,
        IsInst = 0x9,
        CastClass = 0xa,
        AllocateArray = 0xb,
        CheckArrayElementType = 0xc,
        ArrayOfTypeHandle = 0xd,
        DirectGcStaticData = 0xe,
        DirectNonGcStaticData = 0xf,
        // Insert new fixups applying to type tokens by creating a new block of fixups which apply to type tokens
        EndTypeTokenFixups,

        // Metadata fixups that apply to method tokens
        VirtualCallDispatch = 0x10,
        MethodDictionary = 0x11,
        MethodLdToken = 0x12,
        Method = 0x13,
        UnboxingStubMethod = 0x14,
        CallableMethod = 0x15,

        // Insert new fixups applying to method tokens before this point
        EndMethodTokenFixups,

        // Metadata fixups that apply to a type/method token pair
        NonGenericDirectConstrainedMethod = 0x20,
        NonGenericConstrainedMethod = 0x21,
        GenericConstrainedMethod = 0x22,

        // Insert new fixups applying to type/method token pairs before this point
        EndConstraintMethodFixups,

        // Metadata fixups that apply to a field token
        FieldLdToken = 0x30,
        FieldOffset = 0x31,

        // Insert new fixups applying to field tokens before this point
        EndFieldTokenFixups,

        // Metadata fixups that apply to a signature token
        CallingConventionConverter_NoInstantiatingParam = 0x40,
        CallingConventionConverter_HasInstantiatingParam = 0x41,
        CallingConventionConverter_MaybeInstantiatingParam = 0x42,

        // Insert new fixups applying to signature tokens before this point
        EndSignatureTokenFixups,
    }
}
