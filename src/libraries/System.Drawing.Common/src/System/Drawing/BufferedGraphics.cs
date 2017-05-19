// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    /// <include file='doc\BufferedGraphics.uex' path='docs/doc[@for="BufferedGraphics"]/*' />
    /// <devdoc>
    ///         The BufferedGraphics class can be thought of as a "Token" or "Reference" to the
    ///         buffer that a BufferedGraphicsContext creates. While a BufferedGraphics is 
    ///         outstanding, the memory associated with the buffer is locked. The general design
    ///         is such that under normal conditions a single BufferedGraphics will be in use at
    ///         one time for a given BufferedGraphicsContext.
    /// </devdoc>
    public sealed class BufferedGraphics : IDisposable
    {
        private Graphics _bufferedGraphicsSurface;
        private Graphics _targetGraphics;
        private BufferedGraphicsContext _context;
        private IntPtr _targetDC;
        private Point _targetLoc;
        private Size _virtualSize;
        private bool _disposeContext;
        private static int s_rop = 0xcc0020; // RasterOp.SOURCE.GetRop();

        /// <include file='doc\BufferedGraphics.uex' path='docs/doc[@for="BufferedGraphics.BufferedGraphics"]/*' />
        /// <devdoc>
        ///         Internal constructor, this class is created by the BufferedGraphicsContext.
        /// </devdoc>
        internal BufferedGraphics(Graphics bufferedGraphicsSurface, BufferedGraphicsContext context, Graphics targetGraphics,
                                  IntPtr targetDC, Point targetLoc, Size virtualSize)
        {
            _context = context;
            _bufferedGraphicsSurface = bufferedGraphicsSurface;
            _targetDC = targetDC;
            _targetGraphics = targetGraphics;
            _targetLoc = targetLoc;
            _virtualSize = virtualSize;
        }

        ~BufferedGraphics()
        {
            Dispose(false);
        }

        /// <include file='doc\BufferedGraphics.uex' path='docs/doc[@for="BufferedGraphics.Dispose"]/*' />
        /// <devdoc>
        ///         Disposes the object and releases the lock on the memory.
        /// </devdoc>
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_context != null)
                {
                    _context.ReleaseBuffer(this);

                    if (DisposeContext)
                    {
                        _context.Dispose();
                        _context = null;
                    }
                }
                if (_bufferedGraphicsSurface != null)
                {
                    _bufferedGraphicsSurface.Dispose();
                    _bufferedGraphicsSurface = null;
                }
            }
        }

        /// <include file='doc\BufferedGraphics.uex' path='docs/doc[@for="BufferedGraphics.DisposeContext"]/*' />
        /// <devdoc>
        ///         Internal property - determines if we need to dispose of the Context when this is disposed
        /// </devdoc>
        internal bool DisposeContext
        {
            get
            {
                return _disposeContext;
            }
            set
            {
                _disposeContext = value;
            }
        }

        /// <include file='doc\BufferedGraphics.uex' path='docs/doc[@for="BufferedGraphics.Graphics"]/*' />
        /// <devdoc>
        ///         Allows access to the Graphics wrapper for the buffer.
        /// </devdoc>
        public Graphics Graphics
        {
            get
            {
                Debug.Assert(_bufferedGraphicsSurface != null, "The BufferedGraphicsSurface is null!");
                return _bufferedGraphicsSurface;
            }
        }

        /// <include file='doc\BufferedGraphics.uex' path='docs/doc[@for="BufferedGraphics.Render"]/*' />
        /// <devdoc>
        ///         Renders the buffer to the original graphics used to allocate the buffer.
        /// </devdoc>
        public void Render()
        {
            if (_targetGraphics != null)
            {
                Render(_targetGraphics);
            }
            else
            {
                RenderInternal(new HandleRef(Graphics, _targetDC), this);
            }
        }

        /// <include file='doc\BufferedGraphics.uex' path='docs/doc[@for="BufferedGraphics.Render1"]/*' />
        /// <devdoc>
        ///         Renders the buffer to the specified target graphics.
        /// </devdoc>
        public void Render(Graphics target)
        {
            if (target != null)
            {
                IntPtr targetDC = target.GetHdc();

                try
                {
                    RenderInternal(new HandleRef(target, targetDC), this);
                }
                finally
                {
                    target.ReleaseHdcInternal(targetDC);
                }
            }
        }

        /// <include file='doc\BufferedGraphics.uex' path='docs/doc[@for="BufferedGraphics.Render2"]/*' />
        /// <devdoc>
        ///         Renders the buffer to the specified target HDC.
        /// </devdoc>
        public void Render(IntPtr targetDC)
        {
            RenderInternal(new HandleRef(null, targetDC), this);
        }

        /// <include file='doc\BufferedGraphics.uex' path='docs/doc[@for="BufferedGraphics.RenderInternal"]/*' />
        /// <devdoc>
        ///         Internal method that renders the specified buffer into the target.
        /// </devdoc>
        private void RenderInternal(HandleRef refTargetDC, BufferedGraphics buffer)
        {
            IntPtr sourceDC = buffer.Graphics.GetHdc();

            try
            {
                SafeNativeMethods.BitBlt(refTargetDC, _targetLoc.X, _targetLoc.Y, _virtualSize.Width, _virtualSize.Height,
                                         new HandleRef(buffer.Graphics, sourceDC), 0, 0, s_rop);
            }
            finally
            {
                buffer.Graphics.ReleaseHdcInternal(sourceDC);
            }
        }
    }
}
