// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Security;

namespace System.IO.MemoryMappedFiles
{
    public sealed class MemoryMappedViewStream : UnmanagedMemoryStream
    {
        private readonly MemoryMappedView _view;

        [SecurityCritical]
        internal unsafe MemoryMappedViewStream(MemoryMappedView view)
        {
            Debug.Assert(view != null, "view is null");

            _view = view;
            Initialize(_view.ViewHandle, _view.PointerOffset, _view.Size, MemoryMappedFile.GetFileAccess(_view.Access));
        }

        public SafeMemoryMappedViewHandle SafeMemoryMappedViewHandle
        {
            [SecurityCritical]
            get { return _view != null ? _view.ViewHandle : null; }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException(SR.NotSupported_MMViewStreamsFixedLength);
        }

        public long PointerOffset
        {
            get
            {
                if (_view == null)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ViewIsNull);
                }

                return _view.PointerOffset;
            }
        }

        [SecuritySafeCritical]
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && _view != null && !_view.IsClosed)
                {
                    Flush();
                }
            }
            finally
            {
                try
                {
                    if (_view != null)
                    {
                        _view.Dispose();
                    }
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
        [SecurityCritical]
        public override void Flush()
        {
            if (!CanSeek)
            {
                throw __Error.GetStreamIsClosed();
            }

            unsafe
            {
                if (_view != null)
                {
                    _view.Flush((UIntPtr)Capacity);
                }
            }
        }
    }
}
