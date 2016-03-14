// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>
    ///       Provides data for the <see cref='System.ComponentModel.TypeDescriptor.Refresh'/> event.
    ///    </para>
    /// </devdoc>
    [HostProtection(SharedState = true)]
    public class RefreshEventArgs : EventArgs
    {
        private object _componentChanged;
        private Type _typeChanged;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.RefreshEventArgs'/> class with
        ///       the component that has
        ///       changed.
        ///    </para>
        /// </devdoc>
        public RefreshEventArgs(object componentChanged)
        {
            _componentChanged = componentChanged;
            _typeChanged = componentChanged.GetType();
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.RefreshEventArgs'/> class with
        ///       the type
        ///       of component that has changed.
        ///    </para>
        /// </devdoc>
        public RefreshEventArgs(Type typeChanged)
        {
            _typeChanged = typeChanged;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the component that has changed
        ///       its properties, events, or
        ///       extenders.
        ///    </para>
        /// </devdoc>
        public object ComponentChanged
        {
            get
            {
                return _componentChanged;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the type that has changed its properties, or events.
        ///    </para>
        /// </devdoc>
        public Type TypeChanged
        {
            get
            {
                return _typeChanged;
            }
        }
    }
}

