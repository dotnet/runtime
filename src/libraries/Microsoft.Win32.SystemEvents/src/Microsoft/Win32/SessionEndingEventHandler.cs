//------------------------------------------------------------------------------
// <copyright file="SessionEndingEventHandler.cs" company="Microsoft">
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
    /// <para>Represents the method that will handle the <see cref='Microsoft.Win32.SystemEvents.SessionEnding'/> event.</para>
    /// </devdoc>
    [HostProtectionAttribute(MayLeakOnAbort = true)]
    public delegate void SessionEndingEventHandler(object sender, SessionEndingEventArgs e);
}

