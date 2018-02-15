//------------------------------------------------------------------------------
// <copyright file="SessionEndingEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */
namespace Microsoft.Win32 {
    using System;
    using System.Diagnostics;
    using System.Security.Permissions;

    
    /// <devdoc>
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.SessionEnding'/> event.</para>
    /// </devdoc>
    [HostProtectionAttribute(MayLeakOnAbort = true)]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public class SessionEndingEventArgs : EventArgs {
    
        private bool cancel;
        private readonly SessionEndReasons reason;
    
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.SessionEndingEventArgs'/> class.</para>
        /// </devdoc>
        public SessionEndingEventArgs(SessionEndReasons reason) {
            this.reason = reason;
        }
    
        /// <devdoc>
        ///    <para>Gets or sets a value indicating whether to cancel the user request to end the session.</para>
        /// </devdoc>
        public bool Cancel {
            get {
                return cancel;
            }
            set {
                cancel = value;
            }
        }
    
        /// <devdoc>
        ///    <para>Gets how the session is ending.</para>
        /// </devdoc>
        public SessionEndReasons Reason {
            get {
                return reason;
            }
        }
    }
}

