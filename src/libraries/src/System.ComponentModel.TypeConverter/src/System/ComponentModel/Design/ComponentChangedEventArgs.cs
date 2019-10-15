// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides data for the <see cref='System.ComponentModel.Design.IComponentChangeService.ComponentChanged'/> event.
    /// </summary>
    public sealed class ComponentChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the component that is the cause of this event.
        /// </summary>
        public object Component { get; }

        /// <summary>
        /// Gets or sets the member that is about to change.
        /// </summary>
        public MemberDescriptor Member { get; }

        /// <summary>
        /// Gets or sets the new value of the changed member.
        /// </summary>
        public object NewValue { get; }

        /// <summary>
        /// Gets or sets the old value of the changed member.
        /// </summary>
        public object OldValue { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.Design.ComponentChangedEventArgs'/> class.
        /// </summary>
        public ComponentChangedEventArgs(object component, MemberDescriptor member, object oldValue, object newValue)
        {
            Component = component;
            Member = member;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
