// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeMemoryMappedFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>File handle.</summary>
        internal SafeFileHandle? _fileStreamHandle;

        /// <summary>Whether this instance owns the _fileStreamHandle and should Dispose it when disposed.</summary>
        internal readonly bool _ownsFileHandle;

        /// <summary>The inheritability of the memory-mapped file.</summary>
        internal readonly HandleInheritability _inheritability;

        /// <summary>The access to the memory-mapped file.</summary>
        internal readonly MemoryMappedFileAccess _access;

        /// <summary>The options for the memory-mapped file.</summary>
        internal readonly MemoryMappedFileOptions _options;

        /// <summary>The capacity of the memory-mapped file.</summary>
        internal readonly long _capacity;

        /// <summary>Initializes the memory-mapped file handle.</summary>
        /// <param name="fileHandle">The underlying file handle; may be null.</param>
        /// <param name="ownsFileHandle">Whether this SafeHandle is responsible for Disposing the fileStream.</param>
        /// <param name="inheritability">The inheritability of the memory-mapped file.</param>
        /// <param name="access">The access for the memory-mapped file.</param>
        /// <param name="options">The options for the memory-mapped file.</param>
        /// <param name="capacity">The capacity of the memory-mapped file.</param>
        internal SafeMemoryMappedFileHandle(
            SafeFileHandle? fileHandle, bool ownsFileHandle, HandleInheritability inheritability,
            MemoryMappedFileAccess access, MemoryMappedFileOptions options,
            long capacity)
            : base(ownsHandle: true)
        {
            Debug.Assert(!ownsFileHandle || fileHandle != null, "We can only own a FileStream we're actually given.");

            // Store the arguments.  We'll actually open the map when the view is created.
            _ownsFileHandle = ownsFileHandle;
            _inheritability = inheritability;
            _access = access;
            _options = options;
            _capacity = capacity;

            IntPtr handlePtr;

            if (fileHandle != null)
            {
                bool ignored = false;
                fileHandle.DangerousAddRef(ref ignored);
                _fileStreamHandle = fileHandle;
                handlePtr = fileHandle.DangerousGetHandle();
            }
            else
            {
                handlePtr = IntPtr.MaxValue;
            }

            SetHandle(handlePtr);
        }

        protected override bool ReleaseHandle()
        {
            if (_fileStreamHandle != null)
            {
                SetHandle((IntPtr)(-1));

                if (_ownsFileHandle)
                {
                    // Clean up the file descriptor (either for a file on disk or a shared memory object) if we created it
                    _fileStreamHandle.Dispose();
                }

                _fileStreamHandle.DangerousRelease();
                _fileStreamHandle = null;
            }

            return true;
        }

        public override bool IsInvalid => (long)handle <= 0;
    }
}
