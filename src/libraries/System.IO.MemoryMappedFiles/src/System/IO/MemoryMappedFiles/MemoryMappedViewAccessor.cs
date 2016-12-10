// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Security;

namespace System.IO.MemoryMappedFiles
{
    public sealed class MemoryMappedViewAccessor : UnmanagedMemoryAccessor
    {
        private readonly MemoryMappedView _view;

        internal MemoryMappedViewAccessor(MemoryMappedView view)
        {
            Debug.Assert(view != null, "view is null");

            _view = view;
            Initialize(_view.ViewHandle, _view.PointerOffset, _view.Size, MemoryMappedFile.GetFileAccess(_view.Access));
        }

        public SafeMemoryMappedViewHandle SafeMemoryMappedViewHandle
        {
            get { return _view.ViewHandle; }
        }

        public long PointerOffset
        {
            get { return _view.PointerOffset; }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                // Explicitly flush the changes.  The OS will do this for us anyway, but not until after the 
                // MemoryMappedFile object itself is closed. 
                if (disposing && !_view.IsClosed)
                {
                    Flush();
                }
            }
            finally
            {
                try
                {
                    _view.Dispose();
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        // Flushes the changes such that they are in sync with the FileStream bits (ones obtained
        // with the win32 ReadFile and WriteFile functions).  Need to call FileStream's Flush to 
        // flush to the disk.
        // NOTE: This will flush all bytes before and after the view up until an offset that is a
        // multiple of SystemPageSize.
        public void Flush()
        {
            if (!IsOpen)
            {
                throw new ObjectDisposedException(nameof(MemoryMappedViewAccessor), SR.ObjectDisposed_ViewAccessorClosed);
            }

            unsafe
            {
                _view.Flush((UIntPtr)Capacity);
            }
        }
    }
}
