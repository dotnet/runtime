// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Drawing
{
    /// <summary>
    /// The BufferedGraphicsContext class can be used to perform standard double buffer rendering techniques.
    /// </summary>
    public sealed partial class BufferedGraphicsContext : IDisposable
    {
        private Size _maximumBuffer;
        private Size _bufferSize = Size.Empty;

        /// <summary>
        /// Basic constructor.
        /// </summary>
        public BufferedGraphicsContext()
        {
            // By defualt, the size of our maxbuffer will be 3 x standard button size.
            _maximumBuffer.Width = 75 * 3;
            _maximumBuffer.Height = 32 * 3;
        }

        /// <summary>
        /// Allows you to set the maximum width and height of the buffer that will be retained in memory.
        /// You can allocate a buffer of any size, however any request for a buffer that would have a total
        /// memory footprint larger that the maximum size will be allocated temporarily and then discarded 
        /// with the BufferedGraphics is released.
        /// </summary>
        public Size MaximumBuffer
        {
            get => _maximumBuffer;
            set
            {
                if (value.Width <= 0 || value.Height <= 0)
                {
                    throw new ArgumentException(SR.Format(SR.InvalidArgumentValue, nameof(MaximumBuffer), value), nameof(value));
                }

                // If we've been asked to decrease the size of the maximum buffer,
                // then invalidate the older & larger buffer.
                if (value.Width * value.Height < _maximumBuffer.Width * _maximumBuffer.Height)
                {
                    Invalidate();
                }

                _maximumBuffer = value;
            }
        }

        ~BufferedGraphicsContext() => Dispose(false);

        /// <summary>
        /// Returns a BufferedGraphics that is matched for the specified target Graphics object.
        /// </summary>
        public BufferedGraphics Allocate(Graphics targetGraphics, Rectangle targetRectangle)
        {
            if (ShouldUseTempManager(targetRectangle))
            {
                return AllocBufferInTempManager(targetGraphics, IntPtr.Zero, targetRectangle);
            }

            return AllocBuffer(targetGraphics, IntPtr.Zero, targetRectangle);
        }

        /// <summary>
        /// Returns a BufferedGraphics that is matched for the specified target HDC object.
        /// </summary>
        public BufferedGraphics Allocate(IntPtr targetDC, Rectangle targetRectangle)
        {
            if (ShouldUseTempManager(targetRectangle))
            {
                return AllocBufferInTempManager(null, targetDC, targetRectangle);
            }

            return AllocBuffer(null, targetDC, targetRectangle);
        }

        /// <summary>
        /// Returns a BufferedGraphics that is matched for the specified target HDC object.
        /// </summary>
        [SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope")]
        private BufferedGraphics AllocBufferInTempManager(Graphics targetGraphics, IntPtr targetDC, Rectangle targetRectangle)
        {
            BufferedGraphicsContext tempContext = null;
            BufferedGraphics tempBuffer = null;

            try
            {
                tempContext = new BufferedGraphicsContext();
                tempBuffer = tempContext.AllocBuffer(targetGraphics, targetDC, targetRectangle);
                tempBuffer.DisposeContext = true;
            }
            finally
            {
                if (tempContext != null && (tempBuffer == null || (tempBuffer != null && !tempBuffer.DisposeContext)))
                {
                    tempContext.Dispose();
                }
            }

            return tempBuffer;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// This routine allows us to control the point were we start using throw away
        /// managers for painting. Since the buffer manager stays around (by default)
        /// for the life of the app, we don't want to consume too much memory
        /// in the buffer. However, re-allocating the buffer for small things (like
        /// buttons, labels, etc) will hit us on runtime performance.
        /// </summary>
        private bool ShouldUseTempManager(Rectangle targetBounds)
        {
            return (targetBounds.Width * targetBounds.Height) > (MaximumBuffer.Width * MaximumBuffer.Height);
        }
    }
}
