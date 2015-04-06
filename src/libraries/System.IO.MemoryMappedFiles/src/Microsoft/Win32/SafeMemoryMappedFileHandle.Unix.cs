// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeMemoryMappedFileHandle : SafeHandle
    {
        /// <summary>Counter used to produce a unique handle value.</summary>
        private static long s_counter = 0;

        /// <summary>The underlying SafeFileHandle.  May be null.</summary>
        internal readonly SafeFileHandle _fileHandle;

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
        /// <param name="fileHandle">The underlying file handle; may be null.</param>
        /// <param name="inheritability">The inheritability of the memory-mapped file.</param>
        /// <param name="access">The access for the memory-mapped file.</param>
        /// <param name="options">The options for the memory-mapped file.</param>
        /// <param name="capacity">The capacity of the memory-mapped file.</param>
        internal SafeMemoryMappedFileHandle(
            string mapName,
            SafeFileHandle fileHandle, HandleInheritability inheritability,
            MemoryMappedFileAccess access, MemoryMappedFileOptions options,
            long capacity)
            : base(new IntPtr(-1), ownsHandle: true)
        {
            // Store the arguments.  We'll actually open the map when the view is created.
            _mapName = mapName;
            _fileHandle = fileHandle;
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
            if (disposing && _mapName != null && _fileHandle != null)
            {
                // with a non-null name, the file handle is a shared memory object we created; no one else references it
                _fileHandle.Dispose(); 
            }
            base.Dispose(disposing);
        }

        protected override unsafe bool ReleaseHandle()
        {
            return _mapName != null ?
                Interop.libc.shm_unlink(_mapName) == 0 :
                true; // if no mapName, nothing to release
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return (long)handle <= 0; }
        }
    }
}
