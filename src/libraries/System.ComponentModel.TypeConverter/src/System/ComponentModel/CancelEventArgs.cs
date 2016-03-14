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
    ///       Provides data for the <see cref='System.ComponentModel.CancelEventArgs.Cancel'/>
    ///       event.
    ///    </para>
    /// </devdoc>
    [HostProtection(SharedState = true)]
    public class CancelEventArgs : EventArgs
    {
        /// <devdoc>
        ///     Indicates, on return, whether or not the operation should be cancelled
        ///     or not.  'true' means cancel it, 'false' means don't.
        /// </devdoc>
        private bool _cancel;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.CancelEventArgs'/> class with
        ///       cancel set to <see langword='false'/>.
        ///    </para>
        /// </devdoc>
        public CancelEventArgs() : this(false)
        {
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.CancelEventArgs'/> class with
        ///       cancel set to the given value.
        ///    </para>
        /// </devdoc>
        public CancelEventArgs(bool cancel)
        : base()
        {
            _cancel = cancel;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets a value
        ///       indicating whether the operation should be cancelled.
        ///    </para>
        /// </devdoc>
        public bool Cancel
        {
            get
            {
                return _cancel;
            }
            set
            {
                _cancel = value;
            }
        }
    }
}
