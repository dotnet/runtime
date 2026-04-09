// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel
{
    /// <summary>
    /// Provides data for the <see langword='CollectionChange '/> event.
    /// </summary>
    public class CollectionChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.CollectionChangeEventArgs'/> class.
        /// </summary>
        public CollectionChangeEventArgs(CollectionChangeAction action, object? element)
        {
            Action = action;
            Element = element;
        }

        /// <summary>
        /// Gets an action that specifies how the collection changed.
        /// </summary>
        public virtual CollectionChangeAction Action { get; }

        /// <summary>
        /// Gets the instance of the collection with the change.
        /// </summary>
        public virtual object? Element { get; }
    }
}
