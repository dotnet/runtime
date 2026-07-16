// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Shared helpers for resolving MethodDesc signature information.
/// </summary>
internal static class MethodSignatureHelpers
{
    private const uint CLRDATA_METHOD_HAS_THIS = 0x1;

    /// <summary>
    /// Parses raw signature bytes to determine the signature header and argument count.
    /// </summary>
    public static unsafe void GetSignatureInfo(ReadOnlySpan<byte> signature, out SignatureHeader header, out uint numArgs)
    {
        fixed (byte* pSig = signature)
        {
            BlobReader blobReader = new BlobReader(pSig, signature.Length);
            header = blobReader.ReadSignatureHeader();
            if (header.Kind != SignatureKind.Method)
                throw new BadImageFormatException();
            if (header.IsGeneric)
                blobReader.ReadCompressedInteger(); // skip generic arity
            uint paramCount = (uint)blobReader.ReadCompressedInteger();
            numArgs = paramCount + (header.IsInstance ? 1u : 0u);
        }
    }

    public static uint GetMethodFlags(Target target, MethodDescHandle methodDesc)
    {
        if (!target.Contracts.RuntimeTypeSystem.TryGetMethodSignature(methodDesc, out ReadOnlySpan<byte> signature))
            throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;

        GetSignatureInfo(signature, out SignatureHeader header, out _);
        return header.IsInstance ? CLRDATA_METHOD_HAS_THIS : 0;
    }
}
