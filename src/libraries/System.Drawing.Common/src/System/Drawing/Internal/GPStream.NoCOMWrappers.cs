// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.Drawing.Internal
{
    internal sealed partial class GPStream : Interop.Ole32.IStream
    {
        public Interop.Ole32.IStream Clone()
        {
            // The cloned object should have the same current "position"
            return new GPStream(_dataStream)
            {
                _virtualPosition = _virtualPosition
            };
        }

        public unsafe void CopyTo(Interop.Ole32.IStream pstm, ulong cb, ulong* pcbRead, ulong* pcbWritten)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);

            ulong remaining = cb;
            ulong totalWritten = 0;
            ulong totalRead = 0;

            fixed (byte* b = buffer)
            {
                while (remaining > 0)
                {
                    uint read = remaining < (ulong)buffer.Length ? (uint)remaining : (uint)buffer.Length;
                    Read(b, read, &read);
                    remaining -= read;
                    totalRead += read;

                    if (read == 0)
                    {
                        break;
                    }

                    uint written;
                    pstm.Write(b, read, &written);
                    totalWritten += written;
                }
            }

            ArrayPool<byte>.Shared.Return(buffer);

            if (pcbRead != null)
                *pcbRead = totalRead;

            if (pcbWritten != null)
                *pcbWritten = totalWritten;
        }
    }
}
