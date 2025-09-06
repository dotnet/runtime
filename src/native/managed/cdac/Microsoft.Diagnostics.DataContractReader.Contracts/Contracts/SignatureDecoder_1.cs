// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class SignatureDecoder_1 : ISignatureDecoder
{
    private readonly Target _target;
    private Dictionary<ModuleHandle, SignatureTypeProvider<TypeHandle>> _thProviders;
    private Dictionary<ModuleHandle, SignatureTypeProvider<MethodDescHandle>> _mdhProviders;

    internal SignatureDecoder_1(Target target)
    {
        _target = target;
        _thProviders = new Dictionary<ModuleHandle, SignatureTypeProvider<TypeHandle>>();
        _mdhProviders = new Dictionary<ModuleHandle, SignatureTypeProvider<MethodDescHandle>>();
    }


    private SignatureTypeProvider<T> GetProvider<T>(ModuleHandle moduleHandle)
    {
        if (typeof(T) == typeof(TypeHandle))
        {
            if (_thProviders.TryGetValue(moduleHandle, out SignatureTypeProvider<TypeHandle>? thProvider))
            {
                return (SignatureTypeProvider<T>)(object)thProvider;
            }

            SignatureTypeProvider<TypeHandle> newProvider = new(_target, moduleHandle);
            _thProviders[moduleHandle] = newProvider;
            return (SignatureTypeProvider<T>)(object)newProvider;
        }
        else if (typeof(T) == typeof(MethodDescHandle))
        {
            if (_mdhProviders.TryGetValue(moduleHandle, out SignatureTypeProvider<MethodDescHandle>? mdhProvider))
            {
                return (SignatureTypeProvider<T>)(object)mdhProvider;
            }

            SignatureTypeProvider<MethodDescHandle> newProvider = new(_target, moduleHandle);
            _mdhProviders[moduleHandle] = newProvider;
            return (SignatureTypeProvider<T>)(object)newProvider;
        }
        else
        {
            throw new NotSupportedException($"Type parameter {typeof(T)} is not supported.");
        }
    }
    TypeHandle ISignatureDecoder.DecodeFieldSignature(BlobHandle blobHandle, ModuleHandle moduleHandle, TypeHandle ctx)
    {
        SignatureTypeProvider<TypeHandle> provider = GetProvider<TypeHandle>(moduleHandle);
        MetadataReader mdReader = _target.Contracts.EcmaMetadata.GetMetadata(moduleHandle)!;
        BlobReader blobReader = mdReader.GetBlobReader(blobHandle);
        SignatureDecoder<TypeHandle, TypeHandle> decoder = new(provider, mdReader, ctx);
        // Implementation pending
        return decoder.DecodeFieldSignature(ref blobReader);
    }

}
