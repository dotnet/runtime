// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Interop;

public sealed record SignatureContext
{
    /// <summary>Initializes a new instance of the <see cref="SignatureContext"/> class.</summary>
    /// <param name="elementTypeInformation">The signature elements and their marshalling information.</param>
    /// <param name="stubReturnType">The managed return type name.</param>
    /// <param name="additionalAttributes">The additional generated attribute text.</param>
    public SignatureContext(ImmutableArray<TypePositionInfo> elementTypeInformation, string stubReturnType, ImmutableArray<string> additionalAttributes)
    {
        ElementTypeInformation = elementTypeInformation;
        StubReturnType = stubReturnType;
        AdditionalAttributes = additionalAttributes;
    }

    public ImmutableArray<TypePositionInfo> ElementTypeInformation { get; init; }

    public IEnumerable<TypePositionInfo> ManagedParameters => ElementTypeInformation.Where(tpi => !TypePositionInfo.IsSpecialIndex(tpi.ManagedIndex));

    public string StubReturnType { get; init; }

    public IEnumerable<GeneratedParameter> StubParameters
    {
        get
        {
            foreach (TypePositionInfo typeInfo in ElementTypeInformation)
            {
                if (!TypePositionInfo.IsSpecialIndex(typeInfo.ManagedIndex))
                {
                    yield return new GeneratedParameter(
                        typeInfo.ManagedType.FullTypeName,
                        typeInfo.InstanceIdentifier,
                        MarshallerHelpers.GetManagedParameterModifiers(typeInfo));
                }
            }
        }
    }

    public ImmutableArray<string> AdditionalAttributes { get; init; }

    public bool Equals(SignatureContext other)
    {
        // We don't check if the generator factories are equal since
        // the generator factory is deterministically created based on the ElementTypeInformation and Options.
        return other is not null
            && ElementTypeInformation.SequenceEqual(other.ElementTypeInformation)
            && StubReturnType == other.StubReturnType
            && AdditionalAttributes.SequenceEqual(other.AdditionalAttributes);
    }

    public override int GetHashCode()
    {
        throw new UnreachableException();
    }
}
