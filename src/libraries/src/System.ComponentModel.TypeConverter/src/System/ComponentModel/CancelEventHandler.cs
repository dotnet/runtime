// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Represents the method that will handle the event raised when canceling an
    ///       event.</para>
    /// </devdoc>
    [HostProtection(SharedState = true)]
    public delegate void CancelEventHandler(object sender, CancelEventArgs e);
}
