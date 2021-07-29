// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeMemoryMappedFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>
        /// The underlying FileStream.  May be null.  We hold onto the stream rather than just
        /// onto the underlying handle to ensure that logic associated with disposing the stream
        /// (e.g. deleting the file for DeleteOnClose) happens at the appropriate time.
        /// </summary>
        private readonly FileStream? _fileStream;
        /// <summary>The FileStream's handle, cached to avoid repeated accesses to FileStream.SafeFileHandle that could, in theory, change.</summary>
        internal SafeFileHandle? _fileStreamHandle;

        /// <summary>Whether this SafeHandle owns the _fileStream and should Dispose it when disposed.</summary>
        internal readonly bool _ownsFileStream;

        /// <summary>The inheritability of the memory-mapped file.</summary>
        internal readonly HandleInheritability _inheritability;

        /// <summary>The access to the memory-mapped file.</summary>
        internal readonly MemoryMappedFileAccess _access;

        /// <summary>The options for the memory-mapped file.</summary>
        internal readonly MemoryMappedFileOptions _options;

        /// <summary>The capacity of the memory-mapped file.</summary>
        internal readonly long _capacity;

        /// <summary>Initializes the memory-mapped file handle.</summary>
        /// <param name="fileStream">The underlying file stream; may be null.</param>
        /// <param name="ownsFileStream">Whether this SafeHandle is responsible for Disposing the fileStream.</param>
        /// <param name="inheritability">The inheritability of the memory-mapped file.</param>
        /// <param name="access">The access for the memory-mapped file.</param>
        /// <param name="options">The options for the memory-mapped file.</param>
        /// <param name="capacity">The capacity of the memory-mapped file.</param>
        internal SafeMemoryMappedFileHandle(
            FileStream? fileStream, bool ownsFileStream, HandleInheritability inheritability,
            MemoryMappedFileAccess access, MemoryMappedFileOptions options,
            long capacity)
            : base(ownsHandle: true)
        {
            Debug.Assert(!ownsFileStream || fileStream != null, "We can only own a FileStream we're actually given.");

            // Store the arguments.  We'll actually open the map when the view is created.
            _fileStream = fileStream;
            _ownsFileStream = ownsFileStream;
            _inheritability = inheritability;
            _access = access;
            _options = options;
            _capacity = capacity;

            IntPtr handlePtr;

            if (fileStream != null)
            {
                bool ignored = false;
                SafeFileHandle handle = fileStream.SafeFileHandle;
                handle.DangerousAddRef(ref ignored);
                _fileStreamHandle = handle;
                handlePtr = handle.DangerousGetHandle();
            }
            else
            {
                handlePtr = IntPtr.MaxValue;
            }

            SetHandle(handlePtr);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _ownsFileStream)
            {
                // Clean up the file descriptor (either for a file on disk or a shared memory object) if we created it
                _fileStream!.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override bool ReleaseHandle()
        {
            if (_fileStreamHandle != null)
            {
                SetHandle((IntPtr) (-1));
                _fileStreamHandle.DangerousRelease();
                _fileStreamHandle = null;
            }

            return true;
        }

        public override bool IsInvalid => (long)handle <= 0;
    }
}
