// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public void Flush(FlushScope scope)
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

    TargetPointer ISignature.GetVarArgArgsBase(TargetPointer vaSigCookieAddr)
    {
        // Compute the address of the first argument. On x86 the args are pushed below the cookie
        // pointer (stack grows down on the args walk), so the first argument lies at
        //   vaSigCookieAddr + sizeOfArgs.
        // On all other platforms the first argument follows the cookie pointer in memory
        // (stack grows up on the args walk), so its address is at
        //   vaSigCookieAddr + sizeof(VASigCookie*).
        if (_target.Contracts.RuntimeInfo.GetTargetArchitecture() == RuntimeInfoArchitecture.X86)
        {
            Data.VASigCookie cookie = GetCookie(vaSigCookieAddr);
            return new TargetPointer(vaSigCookieAddr.Value + cookie.SizeOfArgs);
        }

        return new TargetPointer(vaSigCookieAddr.Value + (ulong)_target.PointerSize);
    }

    void ISignature.GetVarArgSignature(TargetPointer vaSigCookieAddr, out TargetPointer signatureAddress, out uint signatureLength)
    {
        Data.VASigCookie cookie = GetCookie(vaSigCookieAddr);

        signatureAddress = cookie.Signature.SignaturePointer;
        signatureLength = cookie.Signature.SignatureLength;
        Debug.Assert(signatureAddress != TargetPointer.Null || signatureLength == 0,
            "VASigCookie has a non-zero signature length but a null signature pointer.");
    }
    private Data.VASigCookie GetCookie(TargetPointer vaSigCookieAddr)
    {
        TargetPointer vaSigCookie = _target.ReadPointer(vaSigCookieAddr);
        return _target.ProcessedData.GetOrAdd<Data.VASigCookie>(vaSigCookie);
    }
}
