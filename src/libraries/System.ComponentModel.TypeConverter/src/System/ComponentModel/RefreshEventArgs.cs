// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>
    ///       Provides data for the <see cref='System.ComponentModel.TypeDescriptor.Refresh'/> event.
    ///    </para>
    /// </summary>
    public class RefreshEventArgs : EventArgs
    {
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.RefreshEventArgs'/> class with
        ///       the component that has changed.
        ///    </para>
        /// </summary>
        public RefreshEventArgs(object componentChanged)
        {
            ComponentChanged = componentChanged;
            TypeChanged = componentChanged.GetType();
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.RefreshEventArgs'/> class with
        ///       the type of component that has changed.
        ///    </para>
        /// </summary>
        public RefreshEventArgs(Type typeChanged)
        {
            TypeChanged = typeChanged;
        }

        /// <summary>
        ///    <para>
        ///       Gets the component that has changed its properties, events, or extenders.
        ///    </para>
        /// </summary>
        public object ComponentChanged { get; }

        /// <summary>
        ///    <para>
        ///       Gets the type that has changed its properties, or events.
        ///    </para>
        /// </summary>
        public Type TypeChanged { get; }
    }
}
