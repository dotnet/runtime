// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Win32
{
    /// <devdoc>
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.SessionSwitch'/> event.</para>
    /// </devdoc>
    public class SessionSwitchEventArgs : EventArgs
    {
        private readonly SessionSwitchReason _reason;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.SessionSwitchEventArgs'/> class.</para>
        /// </devdoc>
        public SessionSwitchEventArgs(SessionSwitchReason reason)
        {
            _reason = reason;
        }

        /// <devdoc>
        ///    <para>Gets the reason for the session switch.</para>
        /// </devdoc>
        public SessionSwitchReason Reason
        {
            get
            {
                return _reason;
            }
        }
    }
}
