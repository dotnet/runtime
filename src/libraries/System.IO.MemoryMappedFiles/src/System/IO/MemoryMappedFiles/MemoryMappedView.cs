// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Security;

namespace System.IO.MemoryMappedFiles
{
    internal partial class MemoryMappedView : IDisposable
    {
        private readonly SafeMemoryMappedViewHandle _viewHandle;
        private readonly Int64 _pointerOffset;
        private readonly Int64 _size;
        private readonly MemoryMappedFileAccess _access;

        // These control the retry behaviour when lock violation errors occur during Flush:
        private const Int32 MaxFlushWaits = 15;  // must be <=30
        private const Int32 MaxFlushRetriesPerWait = 20;

        [SecurityCritical]
        private unsafe MemoryMappedView(SafeMemoryMappedViewHandle viewHandle, Int64 pointerOffset,
                                            Int64 size, MemoryMappedFileAccess access)
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

        public Int64 PointerOffset
        {
            get { return _pointerOffset; }
        }

        public Int64 Size
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
    }
}
