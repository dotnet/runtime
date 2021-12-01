// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System.Drawing
{
    /// <summary>
    /// Contains information about the context of a Graphics object.
    /// </summary>
    internal sealed class GraphicsContext : IDisposable
    {
        public GraphicsContext(Graphics g)
        {
            TransformOffset = g.TransformElements.Translation;
            Clip = g.GetRegionIfNotInfinite();
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
            // Dispose all contexts up the stack since they are relative to this one and its state will be invalid.
            Next?.Dispose();
            Next = null;

            Clip?.Dispose();
            Clip = null;
        }

        /// <summary>
        /// The state id representing the GraphicsContext.
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// The translate transform in the GraphicsContext.
        /// </summary>
        public Vector2 TransformOffset { get; private set; }

        /// <summary>
        /// The clipping region the GraphicsContext.
        /// </summary>
        public Region? Clip { get; private set; }

        /// <summary>
        /// The next GraphicsContext object in the stack.
        /// </summary>
        public GraphicsContext? Next { get; set; }

        /// <summary>
        /// The previous GraphicsContext object in the stack.
        /// </summary>
        public GraphicsContext? Previous { get; set; }

        /// <summary>
        /// Flag that determines whether the context was created for a Graphics.Save() operation.
        /// This kind of contexts are cumulative across subsequent Save() calls so the top context
        /// info is cumulative.  This is not the same for contexts created for a Graphics.BeginContainer()
        /// operation, in this case the new context information is reset.  See Graphics.BeginContainer()
        /// and Graphics.Save() for more information.
        /// </summary>
        public bool IsCumulative { get; set; }
    }
}
