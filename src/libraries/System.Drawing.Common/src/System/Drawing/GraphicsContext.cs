// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    using System.Drawing.Drawing2D;

    /// <devdoc>
    ///     Contains information about the context of a Graphics object.
    /// </devdoc>
    internal class GraphicsContext : IDisposable
    {
        /// <devdoc>
        ///     The state that identifies the context.
        /// </devdoc>
        private int _contextState;

        /// <devdoc>
        ///     The context's translate transform.
        /// </devdoc>
        private PointF _transformOffset;

        /// <devdoc>
        ///     The context's clip region.
        /// </devdoc>
        private Region _clipRegion;

        /// <devdoc>
        ///     The next context up the stack.
        /// </devdoc>
        private GraphicsContext _nextContext;

        /// <devdoc>
        ///     The previous context down the stack.
        /// </devdoc>
        private GraphicsContext _prevContext;

        /// <devdoc>
        ///     Flags that determines whether the context was created for a Graphics.Save() operation.
        ///     This kind of contexts are cumulative across subsequent Save() calls so the top context
        ///     info is cumulative.  This is not the same for contexts created for a Graphics.BeginContainer()
        ///     operation, in this case the new context information is reset.  See Graphics.BeginContainer()
        ///     and Graphics.Save() for more information.
        /// </devdoc>
        private bool _isCumulative;

        /// <devdoc>
        ///     Private constructor disallowed.
        /// </devdoc>
        private GraphicsContext()
        {
        }

        public GraphicsContext(Graphics g)
        {
            Matrix transform = g.Transform;
            if (!transform.IsIdentity)
            {
                float[] elements = transform.Elements;
                _transformOffset.X = elements[4];
                _transformOffset.Y = elements[5];
            }
            transform.Dispose();

            Region clip = g.Clip;
            if (clip.IsInfinite(g))
            {
                clip.Dispose();
            }
            else
            {
                _clipRegion = clip;
            }
        }

        /// <devdoc>
        ///     Disposes this and all contexts up the stack.
        /// </devdoc>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <devdoc>
        ///     Disposes this and all contexts up the stack.
        /// </devdoc>
        public void Dispose(bool disposing)
        {
            if (_nextContext != null)
            {
                // Dispose all contexts up the stack since they are relative to this one and its state will be invalid.
                _nextContext.Dispose();
                _nextContext = null;
            }

            if (_clipRegion != null)
            {
                _clipRegion.Dispose();
                _clipRegion = null;
            }
        }

        /// <devdoc>
        ///     The state id representing the GraphicsContext.
        /// </devdoc>
        public int State
        {
            get
            {
                return _contextState;
            }
            set
            {
                _contextState = value;
            }
        }

        /// <devdoc>
        ///     The translate transform in the GraphicsContext.
        /// </devdoc>
        public PointF TransformOffset
        {
            get
            {
                return _transformOffset;
            }
        }

        /// <devdoc>
        ///     The clipping region the GraphicsContext.
        /// </devdoc>
        public Region Clip
        {
            get
            {
                return _clipRegion;
            }
        }

        /// <devdoc>
        ///     The next GraphicsContext object in the stack.
        /// </devdoc>
        public GraphicsContext Next
        {
            get
            {
                return _nextContext;
            }
            set
            {
                _nextContext = value;
            }
        }

        /// <devdoc>
        ///     The previous GraphicsContext object in the stack.
        /// </devdoc>
        public GraphicsContext Previous
        {
            get
            {
                return _prevContext;
            }
            set
            {
                _prevContext = value;
            }
        }

        /// <devdoc>
        ///     Determines whether this context is cumulative or not.  See filed for more info.
        /// </devdoc>
        public bool IsCumulative
        {
            get
            {
                return _isCumulative;
            }
            set
            {
                _isCumulative = value;
            }
        }
    }
}
