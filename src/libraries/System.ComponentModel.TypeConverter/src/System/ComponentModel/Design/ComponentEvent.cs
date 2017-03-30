// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// <para>Provides data for the System.ComponentModel.Design.IComponentChangeService.ComponentEvent
    /// event raised for component-level events.</para>
    /// </summary>
    public class ComponentEventArgs : EventArgs
    {
        /// <summary>
        ///    <para>
        ///       Gets or sets the component associated with the event.
        ///    </para>
        /// </summary>
        public virtual IComponent Component { get; }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the System.ComponentModel.Design.ComponentEventArgs class.
        ///    </para>
        /// </summary>
        public ComponentEventArgs(IComponent component)
        {
            Component = component;
        }
    }
}
