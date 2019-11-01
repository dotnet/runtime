// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeMemoryMappedFileHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        /// <summary>Counter used to produce a unique handle value.</summary>
        private static long s_counter = 0;

        /// <summary>
        /// The underlying FileStream.  May be null.  We hold onto the stream rather than just
        /// onto the underlying handle to ensure that logic associated with disposing the stream
        /// (e.g. deleting the file for DeleteOnClose) happens at the appropriate time.
        /// </summary>
        internal readonly FileStream _fileStream;

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
            FileStream fileStream, bool ownsFileStream, HandleInheritability inheritability,
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

            // Fake a unique int handle value > 0.
            int nextHandleValue = (int)((Interlocked.Increment(ref s_counter) % (int.MaxValue - 1)) + 1);
            SetHandle(new IntPtr(nextHandleValue));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _ownsFileStream)
            {
                // Clean up the file descriptor (either for a file on disk or a shared memory object) if we created it
                _fileStream.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override unsafe bool ReleaseHandle()
        {
            // Nothing to clean up.  We unlinked immediately after creating the backing store.
            return true;
        }

        public override bool IsInvalid
        {
            get { return (long)handle <= 0; }
        }
    }
}
