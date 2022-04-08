// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using Microsoft.Quic;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static class MsQuicAlpnHelper
    {
        public static unsafe void Prepare(List<SslApplicationProtocol> alpnProtocols, [NotNull] out MemoryHandle[]? handles, [NotNull] out QUIC_BUFFER[]? buffers)
        {
            handles = ArrayPool<MemoryHandle>.Shared.Rent(alpnProtocols.Count);
            buffers = ArrayPool<QUIC_BUFFER>.Shared.Rent(alpnProtocols.Count);

            try
            {
                for (int i = 0; i < alpnProtocols.Count; ++i)
                {
                    ReadOnlyMemory<byte> alpnProtocol = alpnProtocols[i].Protocol;
                    MemoryHandle h = alpnProtocol.Pin();

                    handles[i] = h;
                    buffers[i].Buffer = (byte*)h.Pointer;
                    buffers[i].Length = (uint)alpnProtocol.Length;
                }
            }
            catch
            {
                Return(ref handles, ref buffers);
                throw;
            }
        }

        public static void Return(ref MemoryHandle[]? handles, ref QUIC_BUFFER[]? buffers)
        {
            if (handles is MemoryHandle[] notNullHandles)
            {
                foreach (MemoryHandle h in notNullHandles)
                {
                    h.Dispose();
                }

                handles = null;
                ArrayPool<MemoryHandle>.Shared.Return(notNullHandles);
            }

            if (buffers is QUIC_BUFFER[] notNullBuffers)
            {
                buffers = null;
                ArrayPool<QUIC_BUFFER>.Shared.Return(notNullBuffers);
            }
        }
    }
}
