// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides data for the <see cref='System.ComponentModel.Design.IComponentChangeService.ComponentRename'/> event.
    /// </summary>
    public class ComponentRenameEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the component that is being renamed.
        /// </summary>
        public object? Component { get; }

        /// <summary>
        /// Gets or sets the name of the component before the rename.
        /// </summary>
        public virtual string? OldName { get; }

        /// <summary>
        /// Gets or sets the current name of the component.
        /// </summary>
        public virtual string? NewName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Design.ComponentRenameEventArgs'/>
        /// class.
        /// </summary>
        public ComponentRenameEventArgs(object? component, string? oldName, string? newName)
        {
            OldName = oldName;
            NewName = newName;
            Component = component;
        }
    }
}
