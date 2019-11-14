// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Drawing2D;

namespace System.Drawing
{
    /// <summary>
    /// Contains information about the context of a Graphics object.
    /// </summary>
    internal class GraphicsContext : IDisposable
    {
        /// <summary>
        /// The state that identifies the context.
        /// </summary>
        private int _contextState;

        /// <summary>
        /// The context's translate transform.
        /// </summary>
        private PointF _transformOffset;

        /// <summary>
        /// The context's clip region.
        /// </summary>
        private Region _clipRegion;

        /// <summary>
        /// The next context up the stack.
        /// </summary>
        private GraphicsContext _nextContext;

        /// <summary>
        /// The previous context down the stack.
        /// </summary>
        private GraphicsContext _prevContext;

        /// <summary>
        /// Flags that determines whether the context was created for a Graphics.Save() operation.
        /// This kind of contexts are cumulative across subsequent Save() calls so the top context
        /// info is cumulative.  This is not the same for contexts created for a Graphics.BeginContainer()
        /// operation, in this case the new context information is reset.  See Graphics.BeginContainer()
        /// and Graphics.Save() for more information.
        /// </summary>
        private bool _isCumulative;

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

        /// <summary>
        /// Disposes this and all contexts up the stack.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes this and all contexts up the stack.
        /// </summary>
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

        /// <summary>
        /// The state id representing the GraphicsContext.
        /// </summary>
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

        /// <summary>
        /// The translate transform in the GraphicsContext.
        /// </summary>
        public PointF TransformOffset
        {
            get
            {
                return _transformOffset;
            }
        }

        /// <summary>
        ///     The clipping region the GraphicsContext.
        /// </summary>
        public Region Clip
        {
            get
            {
                return _clipRegion;
            }
        }

        /// <summary>
        /// The next GraphicsContext object in the stack.
        /// </summary>
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

        /// <summary>
        /// The previous GraphicsContext object in the stack.
        /// </summary>
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

        /// <summary>
        /// Determines whether this context is cumulative or not.  See filed for more info.
        /// </summary>
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
