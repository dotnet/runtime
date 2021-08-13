// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides an interface for a designer to add menu items to the Visual Studio
    /// 7.0 menu.
    /// </summary>
    public interface IMenuCommandService
    {
        /// <summary>
        /// Gets or sets an array of type <see cref='System.ComponentModel.Design.DesignerVerb'/>
        /// that indicates the verbs that are currently available.
        /// </summary>
        DesignerVerbCollection Verbs { get; }

        /// <summary>
        /// Adds a menu command to the document.
        /// </summary>
        void AddCommand(MenuCommand command);

        /// <summary>
        /// Adds a verb to the set of global verbs.
        /// </summary>
        void AddVerb(DesignerVerb verb);

        /// <summary>
        /// Searches for the given command ID and returns the <see cref='System.ComponentModel.Design.MenuCommand'/>
        /// associated with it.
        /// </summary>
        MenuCommand? FindCommand(CommandID commandID);

        /// <summary>
        /// Invokes a command on the local form or in the global environment.
        /// </summary>
        bool GlobalInvoke(CommandID commandID);

        /// <summary>
        /// Removes the specified <see cref='System.ComponentModel.Design.MenuCommand'/> from the document.
        /// </summary>
        void RemoveCommand(MenuCommand command);

        /// <summary>
        /// Removes the specified verb from the document.
        /// </summary>
        void RemoveVerb(DesignerVerb verb);

        /// <summary>
        /// Shows the context menu with the specified command ID at the specified location.
        /// </summary>
        void ShowContextMenu(CommandID menuID, int x, int y);
    }
}
