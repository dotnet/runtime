// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Win32 {
    using System;
    using System.Diagnostics;
    using System.Security.Permissions;

    
    /// <devdoc>
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.SessionEnded'/> event.</para>
    /// </devdoc>
    public class SessionEndedEventArgs : EventArgs {
    
        private readonly SessionEndReasons reason;
    
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.SessionEndedEventArgs'/> class.</para>
        /// </devdoc>
        public SessionEndedEventArgs(SessionEndReasons reason) {
            this.reason = reason;
        }
    
        /// <devdoc>
        ///    <para>Gets how the session ended.</para>
        /// </devdoc>
        public SessionEndReasons Reason {
            get {
                return reason;
            }
        }
    }
}

