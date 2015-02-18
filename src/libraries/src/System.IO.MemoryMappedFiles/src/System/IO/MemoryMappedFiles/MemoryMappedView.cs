// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Security;

namespace System.IO.MemoryMappedFiles
{
    internal partial class MemoryMappedView : IDisposable
    {
        private readonly SafeMemoryMappedViewHandle _viewHandle;
        private readonly long _pointerOffset;
        private readonly long _size;
        private readonly MemoryMappedFileAccess _access;

        [SecurityCritical]
        private unsafe MemoryMappedView(SafeMemoryMappedViewHandle viewHandle, long pointerOffset,
                                        long size, MemoryMappedFileAccess access)
        {
            _viewHandle = viewHandle;
            _pointerOffset = pointerOffset;
            _size = size;
            _access = access;
        }

        public SafeMemoryMappedViewHandle ViewHandle
        {
            [SecurityCritical]
            get { return _viewHandle; }
        }

        public long PointerOffset
        {
            get { return _pointerOffset; }
        }

        public long Size
        {
            get { return _size; }
        }

        public MemoryMappedFileAccess Access
        {
            get { return _access; }
        }

        [SecurityCritical]
        protected virtual void Dispose(bool disposing)
        {
            if (_viewHandle != null && !_viewHandle.IsClosed)
            {
                _viewHandle.Dispose();
            }
        }

        [SecurityCritical]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsClosed
        {
            [SecuritySafeCritical]
            get { return (_viewHandle == null || _viewHandle.IsClosed); }
        }

        /// <summary>
        /// Validates a size and an offset.  They may need to be shifted based on the supplied
        /// <paramref name="allocationGranularity"/>.
        /// </summary>
        /// <param name="size">The requested size.</param>
        /// <param name="offset">The requested offset.</param>
        /// <param name="allocationGranularity">The allowed granularity for size and offset.</param>
        /// <param name="newSize">The shifted size based on the <paramref name="allocationGranularity"/>.</param>
        /// <param name="extraMemNeeded">The amount <paramref name="newSize"/> and <paramref name="newOffset"/> were shifted.</param>
        /// <param name="newOffset">The shifted offset based on the <paramref name="allocationGranularity"/>.</param>
        private static void ValidateSizeAndOffset(
            long size, long offset, int allocationGranularity,
            out ulong newSize, out ulong extraMemNeeded, out ulong newOffset)
        {
            Debug.Assert(size >= 0);
            Debug.Assert(offset >= 0);
            Debug.Assert(allocationGranularity > 0);

            // Determine how much extra memory needs to be allocated to align on the size of allocationGranularity.
            // The newOffset is then moved down by that amount, and the newSize is increased by that amount.

            extraMemNeeded = (ulong)offset % (ulong)allocationGranularity;
            newOffset = (ulong)offset - extraMemNeeded;
            newSize = (size != MemoryMappedFile.DefaultSize) ? (ulong)size + extraMemNeeded : 0;

            if (IntPtr.Size == 4 && newSize > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException("size", SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
            }
        }
    }
}
