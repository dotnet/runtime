// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// ITreeDesigner is a variation of IDesigner that provides support for
    /// generically indicating parent / child relationships within a designer.
    /// </summary>
    public interface ITreeDesigner : IDesigner
    {
        /// <summary>
        /// Retrieves the children of this designer. This will return an empty collection
        /// if this designer has no children.
        /// </summary>
        ICollection Children { get; }

        /// <summary>
        /// Retrieves the parent designer for this designer. This may return null if
        /// there is no parent.
        /// </summary>
        IDesigner? Parent { get; }
    }
}
