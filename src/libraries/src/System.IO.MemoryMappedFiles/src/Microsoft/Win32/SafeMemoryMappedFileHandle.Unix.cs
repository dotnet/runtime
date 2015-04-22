// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeMemoryMappedFileHandle : SafeHandle
    {
        /// <summary>Indicates where the FileHandle came from, which then controls if/how it should be cleaned up.</summary>
        internal enum FileStreamSource
        {
            Provided,
            ManufacturedFile,
            ManufacturedSharedMemory,
        }

        /// <summary>Counter used to produce a unique handle value.</summary>
        private static long s_counter = 0;

        /// <summary>
        /// The underlying FileStream.  May be null.  We hold onto the stream rather than just
        /// onto the underlying handle to ensure that logic associated with disposing the stream
        /// (e.g. deleting the file for DeleteOnClose) happens at the appropriate time.
        /// </summary>
        internal readonly FileStream _fileStream;

        /// <summary>Indication as to where the file stream came from, if it exists.</summary>
        internal readonly FileStreamSource _fileStreamSource;

        /// <summary>
        /// The name of the map, currently used to give internal names to anonymous,
        /// memory-backed maps.  This will be null if the map is file-backed or in
        /// some corner cases of memory-backed maps, e.g. read-only.
        /// </summary>
        internal readonly string _mapName;

        /// <summary>The inheritability of the memory-mapped file.</summary>
        internal readonly HandleInheritability _inheritability;

        /// <summary>The access to the memory-mapped file.</summary>
        internal readonly MemoryMappedFileAccess _access;

        /// <summary>The options for the memory-mapped file.</summary>
        internal readonly MemoryMappedFileOptions _options;

        /// <summary>The capacity of the memory-mapped file.</summary>
        internal readonly long _capacity;

        /// <summary>Initializes the memory-mapped file handle.</summary>
        /// <param name="mapName">The name of the map; may be null.</param>
        /// <param name="fileStream">The underlying file stream; may be null.</param>
        /// <param name="fileStreamSource">The source of the file stream.</param>
        /// <param name="inheritability">The inheritability of the memory-mapped file.</param>
        /// <param name="access">The access for the memory-mapped file.</param>
        /// <param name="options">The options for the memory-mapped file.</param>
        /// <param name="capacity">The capacity of the memory-mapped file.</param>
        internal SafeMemoryMappedFileHandle(
            string mapName,
            FileStream fileStream, FileStreamSource fileStreamSource, HandleInheritability inheritability,
            MemoryMappedFileAccess access, MemoryMappedFileOptions options,
            long capacity)
            : base(new IntPtr(-1), ownsHandle: true)
        {
            // Store the arguments.  We'll actually open the map when the view is created.
            _mapName = mapName;
            _fileStream = fileStream;
            _fileStreamSource = fileStreamSource;
            _inheritability = inheritability;
            _access = access;
            _options = options;
            _capacity = capacity;

            // Fake a unique int handle value > 0.
            int nextHandleValue = (int)((Interlocked.Increment(ref s_counter) % (int.MaxValue - 1)) + 1);
            SetHandle(new IntPtr(nextHandleValue));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _fileStream != null && _fileStreamSource != FileStreamSource.Provided)
            {
                // Clean up the file if we created it
                _fileStream.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override unsafe bool ReleaseHandle()
        {
            if (_fileStreamSource == FileStreamSource.ManufacturedSharedMemory)
            {
                Debug.Assert(_mapName != null);
                Debug.Assert(_fileStream != null);
                return Interop.libc.shm_unlink(_mapName) == 0;
            }

            // For _fileHandleSource == File, there's nothing to clean up, as it's either the caller's responsibility
            // or it was created as DeleteOnClose (if it was a temporary backing store).

            return true;
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return (long)handle <= 0; }
        }
    }
}
