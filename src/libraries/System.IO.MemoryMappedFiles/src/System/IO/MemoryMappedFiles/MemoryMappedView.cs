// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;

namespace System.IO.MemoryMappedFiles
{
    internal sealed partial class MemoryMappedView : IDisposable
    {
        private readonly SafeMemoryMappedViewHandle _viewHandle;
        private readonly long _pointerOffset;
        private readonly long _size;
        private readonly MemoryMappedFileAccess _access;

        private MemoryMappedView(SafeMemoryMappedViewHandle viewHandle, long pointerOffset, long size, MemoryMappedFileAccess access)
        {
            Debug.Assert(viewHandle != null);

            _viewHandle = viewHandle;
            _pointerOffset = pointerOffset;
            _size = size;
            _access = access;
        }

        public SafeMemoryMappedViewHandle ViewHandle
        {
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

        private void Dispose(bool disposing)
        {
            if (!_viewHandle.IsClosed)
            {
                _viewHandle.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool IsClosed
        {
            get { return _viewHandle.IsClosed; }
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
            long size, long offset, long allocationGranularity,
            out ulong newSize, out long extraMemNeeded, out long newOffset)
        {
            Debug.Assert(size >= 0);
            Debug.Assert(offset >= 0);
            Debug.Assert(allocationGranularity > 0);

            // Determine how much extra memory needs to be allocated to align on the size of allocationGranularity.
            // The newOffset is then moved down by that amount, and the newSize is increased by that amount.

            extraMemNeeded = offset % allocationGranularity;
            newOffset = offset - extraMemNeeded;
            newSize = (size != MemoryMappedFile.DefaultSize) ? (ulong)size + (ulong)extraMemNeeded : 0;

            if (IntPtr.Size == 4 && newSize > uint.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(size), SR.ArgumentOutOfRange_CapacityLargerThanLogicalAddressSpaceNotAllowed);
            }
        }
    }
}
