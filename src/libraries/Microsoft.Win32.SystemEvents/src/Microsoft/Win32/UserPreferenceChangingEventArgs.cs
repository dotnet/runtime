//------------------------------------------------------------------------------
// <copyright file="UserPreferenceChangingEventArgs.cs" company="Microsoft">
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
    /// <para>Provides data for the <see cref='Microsoft.Win32.SystemEvents.UserPreferenceChanging'/> event.</para>
    /// </devdoc>
    [HostProtectionAttribute(MayLeakOnAbort = true)]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.InheritanceDemand, Name = "FullTrust")]
    [System.Security.Permissions.PermissionSetAttribute(System.Security.Permissions.SecurityAction.LinkDemand, Name="FullTrust")]
    public class UserPreferenceChangingEventArgs : EventArgs {
    
        private readonly UserPreferenceCategory category;
    
        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='Microsoft.Win32.UserPreferenceChangingEventArgs'/> class.</para>
        /// </devdoc>
        public UserPreferenceChangingEventArgs(UserPreferenceCategory category) {
            this.category = category;
        }
    
        /// <devdoc>
        ///    <para>Gets the category of user preferences that has Changing.</para>
        /// </devdoc>
        public UserPreferenceCategory Category {
            get {
                return category;
            }
        }
    }
}

