// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.IO.MemoryMappedFiles
{
    public sealed class MemoryMappedViewStream : UnmanagedMemoryStream
    {
        private readonly MemoryMappedView _view;

        internal MemoryMappedViewStream(MemoryMappedView view)
        {
            Debug.Assert(view != null, "view is null");

            _view = view;
            Initialize(_view.ViewHandle, _view.PointerOffset, _view.Size, MemoryMappedFile.GetFileAccess(_view.Access));
        }

        public SafeMemoryMappedViewHandle SafeMemoryMappedViewHandle => _view.ViewHandle;

        public long PointerOffset => _view.PointerOffset;

        public override void SetLength(long value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);

            throw new NotSupportedException(SR.NotSupported_MMViewStreamsFixedLength);
        }


        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing && !_view.IsClosed && CanWrite)
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
        public override void Flush()
        {
            if (!CanSeek)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamIsClosed);
            }

            _view.Flush((UIntPtr)Capacity);
        }
    }
}
