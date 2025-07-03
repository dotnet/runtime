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
    internal static class NetworkFramework
    {
        public static Task<ProtocolToken> EncryptAsync(
                SafeDeleteNwContext securityContext,
                ReadOnlyMemory<byte> input,
                int _/*headerSize*/,
                int _1/*trailerSize*/)
        {
            Debug.Assert(input.Length > 0, $"{nameof(input.Length)} > 0 since {nameof(CanEncryptEmptyMessage)} is false");

            return securityContext.EncryptAsync(input);
        }
        public static async Task<(SecurityStatusPal, int, int)> DecryptAsync(
                SafeDeleteNwContext securityContext,
                Memory<byte> buffer)
        {
            int offset = 0;
            int count = await securityContext.DecryptAsync(buffer).ConfigureAwait(false);
            return (new SecurityStatusPal(SecurityStatusPalErrorCode.OK), offset, count);
        }
    }
}
