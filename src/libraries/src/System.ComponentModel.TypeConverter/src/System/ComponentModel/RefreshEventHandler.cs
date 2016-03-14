// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    /// <para>Represents the method that will handle the <see cref='System.ComponentModel.TypeDescriptor.Refresh'/> event
    ///    raised when a <see cref='System.Type'/> or component is changed during design time.</para>
    /// </devdoc>
    [HostProtection(SharedState = true)]
    public delegate void RefreshEventHandler(RefreshEventArgs e);
}
