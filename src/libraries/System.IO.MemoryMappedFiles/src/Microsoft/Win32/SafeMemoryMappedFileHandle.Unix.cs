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

        /// <summary>The underlying SafeFileHandle.  May be null to use anonymous backing storage.</summary>
        internal readonly SafeFileHandle _fileHandle;

        /// <summary>The inheritability of the memory-mapped file.</summary>
        internal readonly HandleInheritability _inheritability;

        /// <summary>The access to the memory-mapped file.</summary>
        internal readonly MemoryMappedFileAccess _access;

        /// <summary>The options for the memory-mapped file.</summary>
        internal readonly MemoryMappedFileOptions _options;

        /// <summary>The capacity of the memory-mapped file.</summary>
        internal readonly long _capacity;

        /// <summary>Initializes the memory-mapped file handle.</summary>
        /// <param name="fileHandle">The underlying file handle; this may be null in the case of a page-file backed memory-mapped file.</param>
        /// <param name="inheritability">The inheritability of the memory-mapped file.</param>
        /// <param name="access">The access for the memory-mapped file.</param>
        /// <param name="options">The options for the memory-mapped file.</param>
        /// <param name="capacity">The capacity of the memory-mapped file.</param>
        internal SafeMemoryMappedFileHandle(
            SafeFileHandle fileHandle, HandleInheritability inheritability,
            MemoryMappedFileAccess access, MemoryMappedFileOptions options,
            long capacity)
            : base(new IntPtr(-1), ownsHandle: true)
        {
            // Store the arguments.  We'll actually open the map when the view is created.
            _fileHandle = fileHandle;
            _inheritability = inheritability;
            _access = access;
            _options = options;
            _capacity = capacity;

            // Fake a unique int handle value > 0.
            int nextHandleValue = (int)((Interlocked.Increment(ref s_counter) % (int.MaxValue - 1)) + 1);
            SetHandle(new IntPtr(nextHandleValue));
        }

        protected override unsafe bool ReleaseHandle()
        {
            // The actual handle we're holding in the SafeHandle is fake, so nothing to do.
            return true;
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return (long)handle <= 0; }
        }
    }
}
