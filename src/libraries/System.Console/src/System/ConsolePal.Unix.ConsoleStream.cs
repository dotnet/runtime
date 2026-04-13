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
            /// A FileStream wrapping the handle, used to perform reads/writes when the handle is
            /// seekable. RandomAccess.Read/Write use pread/pwrite which always read/write at a
            /// fixed offset; passing fileOffset:0 would keep reading/writing at position 0 rather
            /// than advancing the position, producing incorrect results for seekable files.
            /// </summary>
            private readonly FileStream _fileStream;

            /// <summary>
            /// True if the file handle is seekable (e.g. a regular file) and
            /// <see cref="_fileStream"/> should be used for I/O instead of RandomAccess.
            /// </summary>
            private readonly bool _useFileStreamForIo;

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

                // Create a FileStream wrapper so we can check whether the handle is seekable and,
                // for seekable handles, use it for reads/writes to properly advance the file position.
                // The FileStream is always kept alive (never floated for GC) and disposed in Dispose().
                _fileStream = new FileStream(handle, access, bufferSize: 0);
                _useFileStreamForIo = _fileStream.CanSeek;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _fileStream.Dispose();
                    _handle.Dispose();
                }
                base.Dispose(disposing);
            }

            public override int Read(Span<byte> buffer) =>
#if !TARGET_WASI
                _useReadLine ?
                    ConsolePal.StdInReader.ReadLine(buffer) :
#endif
                    _useFileStreamForIo ?
                        _fileStream.Read(buffer) :
                        RandomAccess.Read(_handle, buffer, fileOffset: 0);

            public override void Write(ReadOnlySpan<byte> buffer)
            {
                if (_useFileStreamForIo)
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
