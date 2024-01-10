// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace System
{
    internal static partial class ConsolePal
    {
        /// <summary>Provides a stream to use for Unix console input or output.</summary>
        private sealed class UnixConsoleStream : ConsoleStream
        {
            /// <summary>The file descriptor for the opened file.</summary>
            private readonly SafeFileHandle _handle;

            private readonly bool _useReadLine;

            /// <summary>Initialize the stream.</summary>
            /// <param name="handle">The file handle wrapped by this stream.</param>
            /// <param name="access">FileAccess.Read or FileAccess.Write.</param>
            /// <param name="useReadLine">Use ReadLine API for reading.</param>
            internal UnixConsoleStream(SafeFileHandle handle, FileAccess access, bool useReadLine = false)
                : base(access)
            {
                Debug.Assert(handle != null, "Expected non-null console handle");
                Debug.Assert(!handle.IsInvalid, "Expected valid console handle");
                _handle = handle;
                _useReadLine = useReadLine;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _handle.Dispose();
                }
                base.Dispose(disposing);
            }

            public override int Read(Span<byte> buffer) =>
#if !TARGET_WASI
                _useReadLine ?
                    ConsolePal.StdInReader.ReadLine(buffer) :
#endif
                    ConsolePal.Read(_handle, buffer);

            public override void Write(ReadOnlySpan<byte> buffer) =>
                ConsolePal.WriteFromConsoleStream(_handle, buffer);

            public override void Flush()
            {
                if (_handle.IsClosed)
                {
                    throw Error.GetFileNotOpen();
                }
                base.Flush();
            }
        }
    }
}
