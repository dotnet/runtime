// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Drawing
{
    /// <summary>
    /// The BufferedGraphics class can be thought of as a "Token" or "Reference" to the buffer that a
    /// BufferedGraphicsContext creates. While a BufferedGraphics is outstanding, the memory associated with the
    /// buffer is locked. The general design is such that under normal conditions a single BufferedGraphics will be in
    /// use at one time for a given BufferedGraphicsContext.
    /// </summary>
    public sealed partial class BufferedGraphics : IDisposable
    {
        private Graphics _targetGraphics;
        private readonly IntPtr _targetDC;

        /// <summary>
        /// Determines if we need to dispose of the Context when this is disposed.
        /// </summary>
        internal bool DisposeContext { get; set; }

        /// <summary>
        /// Renders the buffer to the original graphics used to allocate the buffer.
        /// </summary>
        public void Render()
        {
            if (_targetGraphics != null)
            {
                Render(_targetGraphics);
            }
            else
            {
                RenderInternal(new HandleRef(Graphics, _targetDC));
            }
        }

        /// <summary>
        /// Renders the buffer to the specified target HDC.
        /// </summary>
        public void Render(IntPtr targetDC) => RenderInternal(new HandleRef(null, targetDC));
    }
}
