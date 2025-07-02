// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Security;

internal static partial class SslStreamPal
{
    internal const bool UseAsyncDecrypt = true;

    public static int GetAvailableDecryptedBytes(SafeDeleteContext context)
    {
        if (context is SafeDeleteSslContext) return 0;
        var result = ((SafeDeleteNwContext)context).BytesReadyFromConnection;
        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(context, $"result: {result}", "GetAvailableDecryptedBytes");
        return result;
    }

    public static int ReadDecryptedData(SafeDeleteContext context, Span<byte> buffer)
    {
        var result = ((SafeDeleteNwContext)context).Read(buffer);
        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(context, $"result: {result} bytes", "ReadDecryptedData");
        return result;
    }

    public static Task<SecurityStatusPalErrorCode>? ExtractDecryptionTask(SafeDeleteContext context)
    {
        var nwContext = (SafeDeleteNwContext)context;
        var bytesReady = nwContext.BytesReadyFromConnection;
        if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(context, $"BytesReadyFromConnection: {bytesReady}", "ExtractDecryptionTask");
        lock (context)
        {
            return bytesReady > 0 ? Task.FromResult(SecurityStatusPalErrorCode.OK) : nwContext.DecryptTask?.Task;
        }
    }

    public static ProtocolToken EncryptMessageNetworkFramework(
            SafeDeleteNwContext securityContext,
            ReadOnlyMemory<byte> input,
            int _/*headerSize*/,
            int _1/*trailerSize*/)
    {
        ProtocolToken token = default;
        Debug.Assert(input.Length > 0, $"{nameof(input.Length)} > 0 since {nameof(CanEncryptEmptyMessage)} is false");

        unsafe
        {
            MemoryHandle memHandle = input.Pin();
            try
            {
                securityContext.Encrypt(memHandle.Pointer, input.Length, ref token);
                return token;
            }
            finally
            {
                memHandle.Dispose();
            }
        }
    }
    public static SecurityStatusPal DecryptMessageNetworkFramework(
            SafeDeleteNwContext securityContext,
            Span<byte> buffer,
            out int offset,
            out int count)
    {
        offset = 0;
        count = securityContext.Decrypt(buffer);
        if (GetAvailableDecryptedBytes(securityContext) == 0 && securityContext.DecryptTask?.Task == null)
        {
            securityContext.StartDecrypt();
        }
        return new SecurityStatusPal(SecurityStatusPalErrorCode.OK);
    }
}
