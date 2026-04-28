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
            => handle.Read(fileOffset, buffer);

        internal static unsafe long ReadScatterAtOffset(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset)
            => handle.Read(fileOffset, buffers);

        internal static ValueTask<int> ReadAtOffsetAsync(SafeFileHandle handle, Memory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            => handle.ReadAsync(fileOffset, buffer, cancellationToken, strategy);

        private static ValueTask<long> ReadScatterAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<Memory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            => handle.ReadAsync(fileOffset, buffers, cancellationToken);

        internal static unsafe void WriteAtOffset(SafeFileHandle handle, ReadOnlySpan<byte> buffer, long fileOffset)
            => handle.Write(fileOffset, buffer);

        internal static unsafe void WriteGatherAtOffset(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset)
            => handle.Write(fileOffset, buffers);

        internal static ValueTask WriteAtOffsetAsync(SafeFileHandle handle, ReadOnlyMemory<byte> buffer, long fileOffset, CancellationToken cancellationToken, OSFileStreamStrategy? strategy = null)
            => handle.WriteAsync(fileOffset, buffer, cancellationToken, strategy);

        private static ValueTask WriteGatherAtOffsetAsync(SafeFileHandle handle, IReadOnlyList<ReadOnlyMemory<byte>> buffers, long fileOffset, CancellationToken cancellationToken)
            => handle.WriteAsync(fileOffset, buffers, cancellationToken);
    }
}
