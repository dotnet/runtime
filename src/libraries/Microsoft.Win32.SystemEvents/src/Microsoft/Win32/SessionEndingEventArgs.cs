// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Win32
{
    /// <devdoc>
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.SessionEnding'/> event.</para>
    /// </devdoc>
    public class SessionEndingEventArgs : EventArgs
    {
        private bool _cancel;
        private readonly SessionEndReasons _reason;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.SessionEndingEventArgs'/> class.</para>
        /// </devdoc>
        public SessionEndingEventArgs(SessionEndReasons reason)
        {
            _reason = reason;
        }

        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether to cancel the user request to end the session.</para>
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

        /// <devdoc>
        ///    <para>Gets how the session is ending.</para>
        /// </devdoc>
        public SessionEndReasons Reason
        {
            get
            {
                return _reason;
            }
        }
    }
}
