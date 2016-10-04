// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// <para>Represents the method that will handle the <see cref='System.ComponentModel.Design.IDesignerEventService.ActiveDesignerChanged'/>
    /// event raised on changes to the currently active document.</para>
    /// </summary>
    public delegate void ActiveDesignerEventHandler(object sender, ActiveDesignerEventArgs e);
}
