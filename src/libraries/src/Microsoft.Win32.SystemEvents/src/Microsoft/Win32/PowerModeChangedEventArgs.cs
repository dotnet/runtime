//------------------------------------------------------------------------------
// <copyright file="PowerModeChangedEventArgs.cs" company="Microsoft">
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
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.PowerModeChanged'/> event.</para>
    /// </devdoc>
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name="FullTrust")]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    [HostProtection(MayLeakOnAbort = true)]
    public class PowerModeChangedEventArgs : EventArgs
    {
        private readonly PowerModes mode;
    
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.PowerModeChangedEventArgs'/> class.</para>
        /// </devdoc>
        public PowerModeChangedEventArgs(PowerModes mode) {
            this.mode = mode;
        }
        
        /// <devdoc>
        ///    <para>Gets the power mode.</para>
        /// </devdoc>
        public PowerModes Mode {
            get {
                return mode;
            }
        }
    }
}

