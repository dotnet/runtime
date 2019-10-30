// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies how the collection is changed.
    /// </summary>
    public enum CollectionChangeAction
    {
        /// <summary>
        /// Specifies that an element is added to the collection.
        /// </summary>
        Add = 1,

        /// <summary>
        /// Specifies that an element is removed from the collection.
        /// </summary>
        Remove = 2,

        /// <summary>
        /// Specifies that the entire collection has changed.
        /// </summary>
        Refresh = 3
    }
}
