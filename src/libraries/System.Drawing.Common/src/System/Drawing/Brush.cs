// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Threading;

namespace System.Drawing
{
    public abstract class Brush : MarshalByRefObject, ICloneable, IDisposable
    {
        // Handle to native GDI+ brush object to be used on demand.
        private SafeBrushHandle _nativeBrush = null!;

        public abstract object Clone();

        protected internal void SetNativeBrush(IntPtr brush) => SetNativeBrushInternal(new(brush, true));

        internal void SetNativeBrushInternal(SafeBrushHandle brush)
        {
            Interlocked.Exchange(ref _nativeBrush, brush)?.SetHandleAsInvalid();
        }

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        internal SafeBrushHandle SafeBrushHandle => _nativeBrush;

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
