// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Represents the method that will handle the <see cref='System.ComponentModel.Design.IComponentChangeService.ComponentAdding'/> , <see cref='System.ComponentModel.Design.IComponentChangeService.ComponentAdded'/>, <see cref='System.ComponentModel.Design.IComponentChangeService.ComponentRemoving'/>, and
    /// <see cref='System.ComponentModel.Design.IComponentChangeService.ComponentRemoved'/> event raised
    /// for component-level events.
    /// </summary>
    public delegate void ComponentEventHandler(object? sender, ComponentEventArgs e);
}
