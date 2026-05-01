// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.SignatureHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/* NOTE: some elements of SignatureTypeProvider remain unimplemented or minimally implemented
    * as they are not needed for the current usage of ISignatureDecoder.
    * GetModifiedType and GetPinnedType ignore pinning and custom modifiers.
    * GetTypeFromReference does not look up the type in another module.
    * GetTypeFromSpecification is unimplemented.
    * These can be completed as needed.
    */

internal sealed class SignatureDecoder_1 : ISignatureDecoder
{
    private readonly Target _target;
    private readonly Dictionary<ModuleHandle, SignatureTypeProvider<TypeHandle>> _thProviders = [];
    private readonly Dictionary<ModuleHandle, SignatureTypeProvider<MethodDescHandle>> _mdhProviders = [];
    private readonly Dictionary<ModuleHandle, GcSignatureTypeProvider> _gcProviders = [];

    internal SignatureDecoder_1(Target target)
    {
        _target = target;
    }

    public void Flush()
    {
        _thProviders.Clear();
        _mdhProviders.Clear();
        _gcProviders.Clear();
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

    private SignatureTypeProvider<MethodDescHandle> GetMethodDescHandleProvider(ModuleHandle moduleHandle)
    {
        if (_mdhProviders.TryGetValue(moduleHandle, out SignatureTypeProvider<MethodDescHandle>? mdhProvider))
        {
            return mdhProvider;
        }
        SignatureTypeProvider<MethodDescHandle> newProvider = new(_target, moduleHandle);
        _mdhProviders[moduleHandle] = newProvider;
        return newProvider;
    }


    private GcSignatureTypeProvider GetGcProvider(ModuleHandle moduleHandle)
    {
        if (_gcProviders.TryGetValue(moduleHandle, out GcSignatureTypeProvider? provider))
            return provider;

        GcSignatureTypeProvider newProvider = new(_target, moduleHandle);
        _gcProviders[moduleHandle] = newProvider;
        return newProvider;
    }

    TypeHandle ISignatureDecoder.DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx)
    {
        SignatureTypeProvider<TypeHandle> provider = GetTypeHandleProvider(moduleHandle);
        MetadataReader mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)!;

        RuntimeSignatureDecoder<TypeHandle, TypeHandle, BlobHandleSignatureReader> decoder = new(
            provider, _target, ctx,
            new BlobHandleSignatureReader(mdReader, blobHandle, _target.IsLittleEndian), mdReader);
        return decoder.DecodeFieldSignature();
    }

    MethodSignature<GcTypeKind> ISignatureDecoder.DecodeMethodSignatureForGC(BlobHandle blobHandle, ModuleHandle moduleHandle)
    {
        GcSignatureTypeProvider provider = GetGcProvider(moduleHandle);
        MetadataReader mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)!;

        RuntimeSignatureDecoder<GcTypeKind, object?, BlobHandleSignatureReader> decoder = new(
            provider, _target, genericContext: null,
            new BlobHandleSignatureReader(mdReader, blobHandle, _target.IsLittleEndian), mdReader);
        return decoder.DecodeMethodSignature();
    }
}
