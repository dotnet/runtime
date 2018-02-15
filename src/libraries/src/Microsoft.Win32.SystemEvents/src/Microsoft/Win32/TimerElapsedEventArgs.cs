//------------------------------------------------------------------------------
// <copyright file="TimerElapsedEvenArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------
namespace Microsoft.Win32 {
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;
    
    /// <devdoc>
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.TimerElapsed'/> event.</para>
    /// </devdoc>
    [HostProtectionAttribute(MayLeakOnAbort = true)]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public class TimerElapsedEventArgs : EventArgs {
        private readonly IntPtr timerId;
    
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.TimerElapsedEventArgs'/> class.</para>
        /// </devdoc>
        public TimerElapsedEventArgs(IntPtr timerId) {
            this.timerId = timerId;
        }
        
        /// <devdoc>
        ///    <para>Gets the ID number for the timer.</para>
        /// </devdoc>
        public IntPtr TimerId {
            get {
                return this.timerId;
            }
        }
    }
}

