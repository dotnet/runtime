// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Drawing
{
    public abstract class Brush : MarshalByRefObject, ICloneable, IDisposable
    {
#if FINALIZATION_WATCH
        private string allocationSite = Graphics.GetAllocationStack();
#endif
        // Handle to native GDI+ brush object to be used on demand.
        private SafeBrushHandle _nativeBrush = null!;

        public abstract object Clone();

        protected internal void SetNativeBrush(IntPtr brush) => SetNativeBrushInternal(new(brush, true));

        internal void SetNativeBrushInternal(SafeBrushHandle brush)
        {
            Interlocked.Exchange(ref _nativeBrush, brush)?.SetHandleAsInvalid();
        }

        internal SafeBrushHandle SafeNativeBrush => _nativeBrush;

        protected Brush()
        {
        }

        private protected Brush(SafeBrushHandle nativeBrush)
        {
            _nativeBrush = nativeBrush;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _nativeBrush != null)
            {
                _nativeBrush.Dispose();
            }
        }
    }
}
