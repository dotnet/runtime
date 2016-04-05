// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <devdoc>
    /// <para>Provides data for the <see langword='CollectionChange '/> event.</para>
    /// </devdoc>
    public class CollectionChangeEventArgs : EventArgs
    {
        private readonly CollectionChangeAction _action;
        private readonly object _element;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.CollectionChangeEventArgs'/> class.</para>
        /// </devdoc>
        public CollectionChangeEventArgs(CollectionChangeAction action, object element)
        {
            _action = action;
            _element = element;
        }

        /// <devdoc>
        ///    <para>Gets an action that specifies how the collection changed.</para>
        /// </devdoc>
        public virtual CollectionChangeAction Action
        {
            get
            {
                return _action;
            }
        }

        /// <devdoc>
        ///    <para>Gets the instance of the collection with the change. </para>
        /// </devdoc>
        public virtual object Element
        {
            get
            {
                return _element;
            }
        }
    }
}
