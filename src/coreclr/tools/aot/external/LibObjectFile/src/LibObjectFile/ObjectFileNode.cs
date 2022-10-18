// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile
{
    public abstract class ObjectFileNode
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObjectFileNode _parent;

        /// <summary>
        /// Gets or sets the offset of this section or segment in the parent <see cref="TParentFile"/>.
        /// </summary>
        public ulong Offset { get; set; }

        /// <summary>
        /// Gets the containing parent.
        /// </summary>
        public ObjectFileNode Parent
        {
            get => _parent;

            internal set
            {
                if (value == null)
                {
                    _parent = null;
                }
                else
                {
                    ValidateParent(value);
                }

                _parent = value;
            }
        }

        protected virtual void ValidateParent(ObjectFileNode parent)
        {
        }

        /// <summary>
        /// Index within the containing list in the <see cref="TParentFile"/>
        /// </summary>
        public uint Index { get; internal set; }

        /// <summary>
        /// Gets or sets the size of this section or segment in the parent <see cref="TParentFile"/>.
        /// </summary>
        public virtual ulong Size { get; set; }

        /// <summary>
        /// Checks if the specified offset is contained by this instance.
        /// </summary>
        /// <param name="offset">The offset to check if it belongs to this instance.</param>
        /// <returns><c>true</c> if the offset is within the segment or section range.</returns>
        public bool Contains(ulong offset)
        {
            return offset >= Offset && offset < Offset + Size;
        }

        /// <summary>
        /// Checks this instance contains either the beginning or the end of the specified section or segment.
        /// </summary>
        /// <param name="node">The specified section or segment.</param>
        /// <returns><c>true</c> if the either the offset or end of the part is within this segment or section range.</returns>
        public bool Contains(ObjectFileNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            return Contains((ulong)node.Offset) || node.Size != 0 && Contains((ulong)(node.Offset + node.Size - 1));
        }

        /// <summary>
        /// Verifies the integrity of this file.
        /// </summary>
        /// <returns>The result of the diagnostics</returns>
        public DiagnosticBag Verify()
        {
            var diagnostics = new DiagnosticBag();
            Verify(diagnostics);
            return diagnostics;
        }

        /// <summary>
        /// Verifies the integrity of this file.
        /// </summary>
        /// <param name="diagnostics">A DiagnosticBag instance to receive the diagnostics.</param>
        public virtual void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));
        }

        protected static void AttachChild<TParent, T>(TParent parent, T child, ref T field, bool allowNull) where T : ObjectFileNode where TParent : ObjectFileNode
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (!allowNull && child == null) throw new ArgumentNullException(nameof(child));

            if (field != null)
            {
                field.Parent = null;
            }

            if (child?.Parent != null) throw new InvalidOperationException($"Cannot set the {child.GetType()} as it already belongs to another {child.Parent.GetType()} instance");
            field = child;

            if (child != null)
            {
                child.Parent = parent;
            }
        }
    }

    ///// <summary>
    ///// Base class for a part of a file.
    ///// </summary>
    //public abstract class ObjectFileNode<TParentFile, TLayoutContext> : ObjectFileNode where TParentFile : ObjectFileNode
    //{
    //    protected override void ValidateParent(ObjectFileNode parent)
    //    {
    //        if (!(parent is TParentFile))
    //        {
    //            throw new ArgumentException($"Parent must inherit from type {nameof(TParentFile)}");
    //        }
    //    }
        
    //    internal void UpdateLayoutInternal(TLayoutContext layout)
    //    {
    //        UpdateLayoutImpl(layout);
    //    }

    //    /// <summary>
    //    /// Updates the layout of this object.
    //    /// </summary>
    //    /// <param name="layout">The layout context of this object.</param>
    //    protected abstract void UpdateLayoutImpl(TLayoutContext layout);
        
    //    /// <summary>
    //    /// Gets the containing <see cref="TParentFile"/>. Might be null if this section or segment
    //    /// does not belong to an existing <see cref="TParentFile"/>.
    //    /// </summary>
    //    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    //    public new TParentFile Parent
    //    {
    //        get => (TParentFile) base.Parent;
    //        internal set => base.Parent = value;
    //    }
    //}
}