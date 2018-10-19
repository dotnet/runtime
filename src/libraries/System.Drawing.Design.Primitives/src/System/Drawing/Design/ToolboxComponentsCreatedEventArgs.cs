// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Drawing.Design
{
    /// <summary>
    /// Provides data for the 'ToolboxComponentsCreatedEventArgs' event that occurs
    /// when components are added to the toolbox.
    /// </summary>
    public class ToolboxComponentsCreatedEventArgs : EventArgs
    {
        private readonly IComponent[] _components;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Drawing.Design.ToolboxComponentsCreatedEventArgs'
        /// </summary>
        public ToolboxComponentsCreatedEventArgs(IComponent[] components)
        {
            _components = components;
        }

        /// <summary>
        /// An array storing the toolbox components.
        /// </summary>
        public IComponent[] Components
        {
            get
            {
                return (IComponent[])_components.Clone();
            }
        }
    }
}
