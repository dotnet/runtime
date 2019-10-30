// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Specialized
{
    /// <summary>
    /// This enum describes the action that caused a CollectionChanged event.
    /// </summary>
    public enum NotifyCollectionChangedAction
    {
        /// <summary>
        /// One or more items were added to the collection.
        /// </summary>
        Add,

        /// <summary>
        /// One or more items were removed from the collection.
        /// </summary>
        Remove,

        /// <summary>
        /// One or more items were replaced in the collection.
        /// </summary>
        Replace,

        /// <summary>
        /// One or more items were moved within the collection.
        /// </summary>
        Move,

        /// <summary>
        /// The contents of the collection changed dramatically.
        /// </summary>
        Reset,
    }
}
