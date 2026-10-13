// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Strategies;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO
{
    public static partial class RandomAccess
    {
        internal static unsafe void SetFileLength(SafeFileHandle handle, long length) =>
            FileStreamHelpers.CheckFileCall(Interop.Sys.FTruncate(handle, length), handle.Path);

        internal static unsafe int ReadAtOffset(SafeFileHandle handle, Span<byte> buffer, long fileOffset)
        {
            while (true)
            {
                if (handle.TryCompleteReadAt(fileOffset, buffer, out int readResult, out Interop.ErrorInfo errorInfo))
                {
                    return handle.CheckFileCall(readResult, errorInfo);
                }

                Interop.Sys.Poll(handle, Interop.PollEvents.POLLIN, timeout: -1, out _);
            }
        }

        internal static unsafe long ReadScatterAtOffset(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset)
        {
            while (true)
            {
                if (handle.TryCompleteReadAt(fileOffset, buffers, out long readResult, out Interop.ErrorInfo errorInfo))
                {
                    return handle.CheckFileCall(readResult, errorInfo);
                }

                Interop.Sys.Poll(handle, Interop.PollEvents.POLLIN, timeout: -1, out _);
            }
        }

        internal static ValueTask<int> ReadAtOffsetAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            => handle.ReadAtAsync(fileOffset, buffer, cancellationToken, strategy);

        private static ValueTask<long> ReadScatterAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            => handle.ReadAtAsync(fileOffset, buffers, cancellationToken);

        internal static unsafe void WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
            => WriteAtOffset(handle, buffer, ref fileOffset);

        internal static void WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, ref long fileOffset)
        {
            if (buffer.IsEmpty)
            {
                return;
            }

            while (true)
            {
                if (handle.TryCompleteWriteAt(fileOffset, buffer, out int bytesWritten, out Interop.ErrorInfo errorInfo))
                {
                    fileOffset += bytesWritten;
                    handle.CheckFileCall(errorInfo);
                    return;
                }

                fileOffset += bytesWritten;
                buffer = buffer.Slice(bytesWritten);

                Interop.Sys.Poll(handle, Interop.PollEvents.POLLOUT, timeout: -1, out _);
            }
        }

        internal static unsafe void WriteGatherAtOffset(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset)
        {
            int bufferIndex = 0;
            int bufferOffset = 0;

            while (true)
            {
                if (handle.TryCompleteWriteAt(ref fileOffset, buffers, ref bufferIndex, ref bufferOffset, out Interop.ErrorInfo errorInfo))
                {
                    handle.CheckFileCall(errorInfo);
                    return;
                }

                Interop.Sys.Poll(handle, Interop.PollEvents.POLLOUT, timeout: -1, out _);
            }
        }

        internal static ValueTask WriteAtOffsetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            => handle.WriteAtAsync(fileOffset, buffer, cancellationToken, strategy);

        private static ValueTask WriteGatherAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            => handle.WriteAtAsync(fileOffset, buffers, cancellationToken);
    }
}
