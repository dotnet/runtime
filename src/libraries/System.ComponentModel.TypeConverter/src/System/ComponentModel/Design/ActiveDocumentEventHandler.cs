// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Represents the method that will handle the <see cref='System.ComponentModel.Design.IDesignerEventService.ActiveDesignerChanged'/>
    /// event raised on changes to the currently active document.
    /// </summary>
    public delegate void ActiveDesignerEventHandler(object? sender, ActiveDesignerEventArgs e);
}
