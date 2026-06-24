// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy.StressTestApi;

// Handlers for the private DACSTRESSPRIV_REQUEST_* opcodes that the
// in-proc cDAC stress harness (src/coreclr/vm/cdacstress.cpp) issues
// through IXCLRDataProcess::Request. Kept out of SOSDacImpl so the
// stress-only surface is grouped in one place; SOSDacImpl just
// delegates when it sees one of these reqCodes.
internal static unsafe class CdacStressApi
{
    public const uint RequestFlushTargetState   = 0xf2000000;
    public const uint RequestComputeArgGCRefMap = 0xf2000001;

    // HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER).
    private const int HResultErrorInsufficientBuffer = unchecked((int)0x8007007A);

    public static bool IsStressRequest(uint reqCode)
        => reqCode == RequestFlushTargetState
        || reqCode == RequestComputeArgGCRefMap;

    public static int HandleRequest(Target target, uint reqCode, uint inSize, byte* inBuffer, uint outSize, byte* outBuffer)
    {
        return reqCode switch
        {
            RequestFlushTargetState   => HandleFlushTargetState(target, inSize, inBuffer, outSize, outBuffer),
            RequestComputeArgGCRefMap => HandleComputeArgGCRefMap(target, inSize, inBuffer, outSize, outBuffer),
            _ => HResults.E_INVALIDARG,
        };
    }

    private static int HandleFlushTargetState(Target target, uint inSize, byte* inBuffer, uint outSize, byte* outBuffer)
    {
        if (inSize != 0 || inBuffer is not null || outSize != 0 || outBuffer is not null)
            return HResults.E_INVALIDARG;
        target.Flush(FlushScope.ForwardExecution);
        return HResults.S_OK;
    }

    // Mirrors DacStressArgGCRefMapRequest in src/coreclr/inc/dacprivate.h.
    // The caller hands us an [in,out] descriptor with the MethodDesc plus a
    // caller-allocated destination buffer; we write the blob there and
    // populate cbFilled / cbNeeded. The COM `outBuffer` channel is unused.
    [StructLayout(LayoutKind.Sequential)]
    private struct DacStressArgGCRefMapRequest
    {
        public ulong MethodDesc;
        public ulong BlobBuffer;
        public uint  BlobBufferLen;
        public uint  cbFilled;
        public uint  cbNeeded;
    }

    private static int HandleComputeArgGCRefMap(Target target, uint inSize, byte* inBuffer, uint outSize, byte* outBuffer)
    {
        _ = outSize;
        _ = outBuffer;

        if (inBuffer is null || inSize < (uint)Unsafe.SizeOf<DacStressArgGCRefMapRequest>())
            return HResults.E_INVALIDARG;

        // Alignment-safe view of the [in,out] descriptor. The cDAC ABI hands
        // us a `byte*` from a COM marshaller with no guaranteed alignment.
        DacStressArgGCRefMapRequest req = Unsafe.ReadUnaligned<DacStressArgGCRefMapRequest>(inBuffer);

        byte[] blob;
        bool encoded;
        try
        {
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            MethodDescHandle mdh = rts.GetMethodDescHandle(
                new ClrDataAddress(req.MethodDesc).ToTargetPointer(target));
            encoded = target.Contracts.CallingConvention.TryComputeArgGCRefMapBlob(mdh, out blob);
        }
        catch
        {
            req.cbFilled = 0;
            req.cbNeeded = 0;
            Unsafe.WriteUnaligned(inBuffer, req);
            return HResults.E_FAIL;
        }

        if (!encoded)
        {
            req.cbFilled = 0;
            req.cbNeeded = 0;
            Unsafe.WriteUnaligned(inBuffer, req);
            return HResults.E_NOTIMPL;
        }

        uint needed = (uint)blob.Length;
        req.cbNeeded = needed;

        if (req.BlobBuffer == 0 || req.BlobBufferLen < needed)
        {
            req.cbFilled = 0;
            Unsafe.WriteUnaligned(inBuffer, req);
            return HResultErrorInsufficientBuffer;
        }

        byte* dest = (byte*)(nuint)req.BlobBuffer;
        blob.AsSpan().CopyTo(new Span<byte>(dest, (int)req.BlobBufferLen));
        req.cbFilled = needed;
        Unsafe.WriteUnaligned(inBuffer, req);
        return HResults.S_OK;
    }
}
