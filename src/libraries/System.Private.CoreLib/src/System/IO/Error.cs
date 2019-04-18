// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.IO
{
    /// <summary>
    ///     Provides centralized methods for creating exceptions for System.IO.FileSystem.
    /// </summary>
    internal static class Error
    {
        internal static Exception GetStreamIsClosed()
        {
            return new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
        }

        internal static Exception GetEndOfFile()
        {
            return new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
        }

        internal static Exception GetFileNotOpen()
        {
            return new ObjectDisposedException(null, SR.ObjectDisposed_FileClosed);
        }

        internal static Exception GetReadNotSupported()
        {
            return new NotSupportedException(SR.NotSupported_UnreadableStream);
        }

        internal static Exception GetSeekNotSupported()
        {
            return new NotSupportedException(SR.NotSupported_UnseekableStream);
        }

        internal static Exception GetWriteNotSupported()
        {
            return new NotSupportedException(SR.NotSupported_UnwritableStream);
        }
    }
}
