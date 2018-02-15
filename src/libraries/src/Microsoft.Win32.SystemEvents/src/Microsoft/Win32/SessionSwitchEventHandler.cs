//------------------------------------------------------------------------------
// <copyright file="SessionSwitchEventHandler.cs" company="Microsoft">
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
    /// <para>Represents the method that will handle the <see cref='Microsoft.Win32.SystemEvents.SessionSwitch'/> event.</para>
    /// </devdoc>
    [HostProtectionAttribute(MayLeakOnAbort = true)]
    public delegate void SessionSwitchEventHandler(object sender, SessionSwitchEventArgs e);
}

