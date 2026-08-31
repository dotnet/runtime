// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides data for the <see cref='System.ComponentModel.Design.IComponentChangeService.ComponentChanging'/> event.
    /// </summary>
    public sealed class ComponentChangingEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the component that is being changed or that is the parent container of the member being changed.
        /// </summary>
        public object? Component { get; }

        /// <summary>
        /// Gets or sets the member of the component that is about to be changed.
        /// </summary>
        public MemberDescriptor? Member { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Design.ComponentChangingEventArgs'/> class.
        /// </summary>
        public ComponentChangingEventArgs(object? component, MemberDescriptor? member)
        {
            Component = component;
            Member = member;
        }
    }
}
