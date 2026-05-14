// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

// NOTE: some elements of SignatureTypeProvider remain unimplemented or minimally implemented
// as they are not needed for the current usage of ISignature.
// GetModifiedType and GetPinnedType ignore pinning and custom modifiers.
// GetTypeFromReference does not look up the type in another module.
// GetTypeFromSpecification is unimplemented.
// These can be completed as needed.
internal sealed class Signature_1 : ISignature
{
    private readonly Target _target;
    private readonly Dictionary<ModuleHandle, SignatureTypeProvider<TypeHandle>> _thProviders = [];

    internal Signature_1(Target target)
    {
        _target = target;
    }

    public void Flush()
    {
        _thProviders.Clear();
    }

    private SignatureTypeProvider<TypeHandle> GetTypeHandleProvider(ModuleHandle moduleHandle)
    {
        if (_thProviders.TryGetValue(moduleHandle, out SignatureTypeProvider<TypeHandle>? thProvider))
        {
            return thProvider;
        }

        SignatureTypeProvider<TypeHandle> newProvider = new(_target, moduleHandle);
        _thProviders[moduleHandle] = newProvider;
        return newProvider;
    }

    TypeHandle ISignature.DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx)
    {
        SignatureTypeProvider<TypeHandle> provider = GetTypeHandleProvider(moduleHandle);
        MetadataReader mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)!;

        BlobReader blobReader = mdReader.GetBlobReader(blobHandle);
        RuntimeSignatureDecoder<TypeHandle, TypeHandle> decoder = new(provider, _target, mdReader, ctx);
        return decoder.DecodeFieldSignature(ref blobReader);
    }
}
