//------------------------------------------------------------------------------
// <copyright file="PowerModeChangedEventHandler.cs" company="Microsoft">
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
    /// <para>Represents the method that will handle the <see cref='Microsoft.Win32.SystemEvents.PowerModeChanged'/> event.</para>
    /// </devdoc>
    [HostProtection(MayLeakOnAbort = true)]
    public delegate void PowerModeChangedEventHandler(object sender, PowerModeChangedEventArgs e);
}

