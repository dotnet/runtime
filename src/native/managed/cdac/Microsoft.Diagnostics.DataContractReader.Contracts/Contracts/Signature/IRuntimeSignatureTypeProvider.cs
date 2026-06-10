// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

/// <summary>
/// Superset of SRM's <see cref="ISignatureTypeProvider{TType, TGenericContext}"/>
/// that adds support for runtime-internal type codes
/// (<c>ELEMENT_TYPE_INTERNAL</c> 0x21 and <c>ELEMENT_TYPE_CMOD_INTERNAL</c> 0x22).
/// </summary>
/// <remarks>
/// Providers implementing this interface automatically satisfy SRM's
/// <see cref="ISignatureTypeProvider{TType, TGenericContext}"/> and can be used
/// with both SRM's <c>SignatureDecoder</c> and our
/// <see cref="RuntimeSignatureDecoder{TType, TGenericContext}"/>.
/// </remarks>
public interface IRuntimeSignatureTypeProvider<TType, TGenericContext>
    : ISignatureTypeProvider<TType, TGenericContext>
{
    /// <summary>
    /// Classify an <c>ELEMENT_TYPE_INTERNAL</c> (0x21) type by resolving the
    /// embedded TypeHandle pointer via the target's runtime type system.
    /// </summary>
    TType GetInternalType(TargetPointer typeHandlePointer);

    /// <summary>
    /// Classify an <c>ELEMENT_TYPE_CMOD_INTERNAL</c> (0x22) custom modifier by
    /// resolving the embedded TypeHandle pointer via the target's runtime type system.
    /// </summary>
    TType GetInternalModifiedType(TargetPointer typeHandlePointer, TType unmodifiedType, bool isRequired);
}
