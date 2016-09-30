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
    /// <para>Represents the method that will handle the System.ComponentModel.Design.IDesignerEventService.DesignerEvent
    /// event raised when a document is created or disposed.</para>
    /// </summary>
    public delegate void DesignerEventHandler(object sender, DesignerEventArgs e);
}

