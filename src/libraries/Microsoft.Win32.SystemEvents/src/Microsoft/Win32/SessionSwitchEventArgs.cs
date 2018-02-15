//------------------------------------------------------------------------------
// <copyright file="SessionSwitchEventArgs.cs" company="Microsoft">
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
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.SessionSwitch'/> event.</para>
    /// </devdoc>    
    [HostProtectionAttribute(MayLeakOnAbort = true)]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public class SessionSwitchEventArgs : EventArgs {
    
        private readonly SessionSwitchReason reason;
    
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.SessionSwitchEventArgs'/> class.</para>
        /// </devdoc>
        public SessionSwitchEventArgs(SessionSwitchReason reason) {
            this.reason = reason;
        }
    
        /// <devdoc>
        ///    <para>Gets the reason for the session switch.</para>
        /// </devdoc>
        public SessionSwitchReason Reason {
            get {
                return reason;
            }
        }
    }
}

