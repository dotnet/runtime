// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides data for the System.ComponentModel.Design.IComponentChangeService.ComponentEvent
    /// event raised for component-level events.
    /// </summary>
    public class ComponentEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the component associated with the event.
        /// </summary>
        public virtual IComponent? Component { get; }

        /// <summary>
        /// Initializes a new instance of the System.ComponentModel.Design.ComponentEventArgs class.
        /// </summary>
        public ComponentEventArgs(IComponent? component)
        {
            Component = component;
        }
    }
}
