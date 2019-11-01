// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Provides data for the <see cref='System.ComponentModel.TypeDescriptor.Refresh(object)'/> event.
    /// </summary>
    public class RefreshEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.RefreshEventArgs'/> class with
        /// the component that has changed.
        /// </summary>
        public RefreshEventArgs(object componentChanged)
        {
            ComponentChanged = componentChanged;
            TypeChanged = componentChanged?.GetType();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.RefreshEventArgs'/> class with
        /// the type of component that has changed.
        /// </summary>
        public RefreshEventArgs(Type typeChanged)
        {
            TypeChanged = typeChanged;
        }

        /// <summary>
        /// Gets the component that has changed its properties, events, or extenders.
        /// </summary>
        public object ComponentChanged { get; }

        /// <summary>
        /// Gets the type that has changed its properties, or events.
        /// </summary>
        public Type TypeChanged { get; }
    }
}
