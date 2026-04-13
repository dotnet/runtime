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

            /// <summary>
            /// A FileStream wrapping the handle when it's a seekable file (e.g., a regular file).
            /// RandomAccess.Read/Write use pread/pwrite which always read/write at a fixed offset;
            /// for seekable files we need a FileStream to properly track the file position.
            /// </summary>
            private readonly FileStream? _fileStream;

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

                // Create a FileStream to determine if the handle is seekable and to use for
                // reads/writes on seekable files. RandomAccess.Read/Write use pread/pwrite which
                // always operate at a specified offset; passing fileOffset:0 would cause them to
                // read/write at position 0 rather than advancing the file position, which produces
                // incorrect results for seekable files like regular files.
                // For non-seekable files (e.g., pipes, terminals), FileStream.CanSeek is false
                // and we fall back to the original RandomAccess-based path.
                FileStream fs = new FileStream(handle, access, bufferSize: 0);
                if (fs.CanSeek)
                {
                    _fileStream = fs;
                }
                // else: fs is not seekable; let it be GC'd. Its finalizer calls Dispose(false)
                // which does NOT close the handle (OSFileStreamStrategy.Dispose skips the handle
                // close when disposing=false), so _handle remains valid.
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _fileStream?.Dispose();
                    _handle.Dispose();
                }
                base.Dispose(disposing);
            }

            public override int Read(Span<byte> buffer) =>
#if !TARGET_WASI
                _useReadLine ?
                    ConsolePal.StdInReader.ReadLine(buffer) :
#endif
                    _fileStream is not null ?
                        _fileStream.Read(buffer) :
                        RandomAccess.Read(_handle, buffer, fileOffset: 0);

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                if (_fileStream is not null)
                {
                    ConsolePal.EnsureConsoleInitialized();
                    lock (Console.Out)
                    {
                        _fileStream.Write(buffer);
                    }
                }
                else
                {
                    ConsolePal.WriteFromConsoleStream(_handle, buffer);
                }
            }

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
