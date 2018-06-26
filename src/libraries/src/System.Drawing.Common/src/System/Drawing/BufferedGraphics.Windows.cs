// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing
{
    public sealed partial class BufferedGraphics
    {
        private Graphics _bufferedGraphicsSurface;
        private BufferedGraphicsContext _context;
        private readonly Point _targetLoc;
        private readonly Size _virtualSize;
        private const int RasterOp = 0xcc0020; // RasterOp.SOURCE.GetRop();

        /// <summary>
        /// Internal constructor, this class is created by BufferedGraphicsContext.
        /// </summary>
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

        public void Dispose()
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

        /// <summary>
        /// Allows access to the Graphics wrapper for the buffer.
        /// </summary>
        public Graphics Graphics => _bufferedGraphicsSurface;

        /// <summary>
        /// Renders the buffer to the specified target graphics.
        /// </summary>
        public void Render(Graphics target)
        {
            if (target != null)
            {
                IntPtr targetDC = target.GetHdc();

                try
                {
                    RenderInternal(new HandleRef(target, targetDC));
                }
                finally
                {
                    target.ReleaseHdcInternal(targetDC);
                }
            }
        }

        /// <summary>
        /// Internal method that renders the specified buffer into the target.
        /// </summary>
        private void RenderInternal(HandleRef refTargetDC)
        {
            IntPtr sourceDC = Graphics.GetHdc();

            try
            {
                SafeNativeMethods.BitBlt(refTargetDC, _targetLoc.X, _targetLoc.Y, _virtualSize.Width, _virtualSize.Height,
                                         new HandleRef(Graphics, sourceDC), 0, 0, RasterOp);
            }
            finally
            {
                Graphics.ReleaseHdcInternal(sourceDC);
            }
        }
    }
}
