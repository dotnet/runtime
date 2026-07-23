// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Describes a type as encoded in a runtime signature, with an optional exact loaded runtime type.
/// </summary>
public readonly struct SignatureTypeInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureTypeInfo"/> struct.
    /// </summary>
    /// <param name="elementType">The outermost signature element type.</param>
    /// <param name="exactTypeHandle">The exact loaded runtime type, or <see langword="null"/> when it is unavailable.</param>
    /// <param name="genericTypeDefinition">The loaded generic type definition, or <see langword="null"/> when this is not a generic instantiation.</param>
    /// <param name="typeArguments">The generic type arguments.</param>
    public SignatureTypeInfo(
        CorElementType elementType,
        ITypeHandle? exactTypeHandle,
        ITypeHandle? genericTypeDefinition = null,
        ImmutableArray<SignatureTypeInfo> typeArguments = default)
    {
        ElementType = elementType;
        ExactTypeHandle = exactTypeHandle;
        GenericTypeDefinition = genericTypeDefinition;
        TypeArguments = typeArguments.IsDefault ? [] : typeArguments;
    }

    /// <summary>
    /// Gets the outermost signature element type.
    /// </summary>
    public CorElementType ElementType { get; }

    /// <summary>
    /// Gets the exact loaded runtime type, or <see langword="null"/> when it is unavailable.
    /// </summary>
    public ITypeHandle? ExactTypeHandle { get; }

    /// <summary>
    /// Gets the loaded generic type definition, or <see langword="null"/> when this is not a generic instantiation.
    /// </summary>
    public ITypeHandle? GenericTypeDefinition { get; }

    /// <summary>
    /// Gets the generic type arguments.
    /// </summary>
    public ImmutableArray<SignatureTypeInfo> TypeArguments { get; }
}

/// <summary>
/// Provides signature type information independently of exact loaded runtime type identity.
/// </summary>
public interface ITypeInformation : IContract
{
    static string IContract.Name { get; } = nameof(TypeInformation);

    /// <summary>
    /// Decodes the signature of a method.
    /// </summary>
    /// <param name="methodDesc">The method whose signature is decoded.</param>
    /// <returns>The decoded method signature.</returns>
    MethodSignature<SignatureTypeInfo> DecodeMethodSignature(MethodDescHandle methodDesc) => throw new NotImplementedException();

    /// <summary>
    /// Decodes the signature type of a field in the supplied owning type context.
    /// </summary>
    /// <param name="fieldDesc">The field descriptor whose signature is decoded.</param>
    /// <param name="owningType">The owning type context used to resolve generic type parameters.</param>
    /// <returns>The decoded field type.</returns>
    SignatureTypeInfo GetFieldTypeInfo(TargetPointer fieldDesc, SignatureTypeInfo owningType) => throw new NotImplementedException();
}

public readonly struct TypeInformation : ITypeInformation
{
    // Everything throws NotImplementedException
}
