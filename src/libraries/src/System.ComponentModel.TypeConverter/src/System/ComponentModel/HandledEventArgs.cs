// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>
    ///       Provides data for the <see cref='System.ComponentModel.HandledEventArgs.Handled'/>
    ///       event.
    ///    </para>
    /// </devdoc>
    [HostProtection(SharedState = true)]
    public class HandledEventArgs : EventArgs
    {
        /// <devdoc>
        ///     Indicates, on return, whether or not the event was handled in the application's event handler.  
        ///     'true' means the application handled the event, 'false' means it didn't.
        /// </devdoc>
        private bool _handled;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.HandledEventArgs'/> class with
        ///       handled set to <see langword='false'/>.
        ///    </para>
        /// </devdoc>
        public HandledEventArgs() : this(false)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.HandledEventArgs'/> class with
        ///       handled set to the given value.
        ///    </para>
        /// </devdoc>
        public HandledEventArgs(bool defaultHandledValue)
        : base()
        {
            _handled = defaultHandledValue;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value
        ///       indicating whether the event is handled.
        ///    </para>
        /// </devdoc>
        public bool Handled
        {
            get
            {
                return _handled;
            }
            set
            {
                _handled = value;
            }
        }
    }
}
