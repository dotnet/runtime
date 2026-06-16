// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Shared helpers for resolving MethodDesc metadata and signature information.
/// </summary>
internal static class MethodDescInfoHelpers
{
    /// <summary>
    /// Resolves a MethodDescHandle into its module-level metadata objects.
    /// Throws on failure (no metadata, etc.).
    /// </summary>
    public static void GetMethodInfo(
        Target target,
        MethodDescHandle mdh,
        out MetadataReader mdReader,
        out MethodDefinition methodDef,
        out Contracts.ModuleHandle moduleHandle,
        out uint token)
    {
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        TargetPointer mtAddr = rts.GetMethodTable(mdh);
        TypeHandle typeHandle = rts.GetTypeHandle(mtAddr);
        TargetPointer modulePtr = rts.GetModule(typeHandle);
        ILoader loader = target.Contracts.Loader;
        moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);
        token = rts.GetMethodToken(mdh);

        IEcmaMetadata ecmaMetadata = target.Contracts.EcmaMetadata;
        MetadataReader? reader = ecmaMetadata.GetMetadata(moduleHandle);
        if (reader is null)
            throw new NotImplementedException();
        mdReader = reader;

        MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle((int)token);
        methodDef = mdReader.GetMethodDefinition(methodDefHandle);
    }

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
}
