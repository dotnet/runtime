//------------------------------------------------------------------------------
// <copyright file="SessionEndedEventArgs.cs" company="Microsoft">
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
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.SessionEnded'/> event.</para>
    /// </devdoc>
    [HostProtectionAttribute(MayLeakOnAbort = true)]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
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

