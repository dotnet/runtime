// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>Provides an interface for a designer to add menu items to the Visual Studio
    ///       7.0 menu.</para>
    /// </summary>

    public interface IMenuCommandService
    {
        /// <summary>
        /// <para>Gets or sets an array of type <see cref='System.ComponentModel.Design.DesignerVerb'/> 
        /// that indicates the verbs that are currently available.</para>
        /// </summary>
        DesignerVerbCollection Verbs { get; }

        /// <summary>
        ///    <para>
        ///       Adds a menu command to the document.
        ///    </para>
        /// </summary>
        void AddCommand(MenuCommand command);

        /// <summary>
        ///    <para>
        ///       Adds a verb to the set of global verbs.
        ///    </para>
        /// </summary>
        void AddVerb(DesignerVerb verb);

        /// <summary>
        ///    <para>
        ///       Searches for the given command ID and returns
        ///       the <see cref='System.ComponentModel.Design.MenuCommand'/>
        ///       associated with it.
        ///    </para>
        /// </summary>
        MenuCommand FindCommand(CommandID commandID);

        /// <summary>
        ///    <para>Invokes a command on the local form or in the global environment.</para>
        /// </summary>
        bool GlobalInvoke(CommandID commandID);

        /// <summary>
        ///    <para>
        ///       Removes the specified <see cref='System.ComponentModel.Design.MenuCommand'/> from the document.
        ///    </para>
        /// </summary>
        void RemoveCommand(MenuCommand command);

        /// <summary>
        ///    <para>
        ///       Removes the specified verb from the document.
        ///    </para>
        /// </summary>
        void RemoveVerb(DesignerVerb verb);

        /// <summary>
        ///    <para>Shows the context menu with the specified command ID at the specified
        ///       location.</para>
        /// </summary>
        void ShowContextMenu(CommandID menuID, int x, int y);
    }
}

