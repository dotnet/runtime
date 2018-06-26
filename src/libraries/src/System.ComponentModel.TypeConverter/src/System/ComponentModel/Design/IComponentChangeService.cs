// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Provides an interface to add and remove the event handlers for System.ComponentModel.Design.IComponentChangeService.ComponentAdded, System.ComponentModel.Design.IComponentChangeService.ComponentAdding, System.ComponentModel.Design.IComponentChangeService.ComponentChanged, System.ComponentModel.Design.IComponentChangeService.ComponentChanging, System.ComponentModel.Design.IComponentChangeService.ComponentRemoved, System.ComponentModel.Design.IComponentChangeService.ComponentRemoving, and System.ComponentModel.Design.IComponentChangeService.ComponentRename events.
    /// </summary>
    public interface IComponentChangeService
    {
        /// <summary>
        /// Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentAdded event.
        /// </summary>
        event ComponentEventHandler ComponentAdded;

        /// <summary>
        /// Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentAdding event.
        /// </summary>
        event ComponentEventHandler ComponentAdding;

        /// <summary>
        /// Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentChanged event.
        /// </summary>
        event ComponentChangedEventHandler ComponentChanged;

        /// <summary>
        /// Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentChanging event.
        /// </summary>
        event ComponentChangingEventHandler ComponentChanging;

        /// <summary>
        /// Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRemoved event.
        /// </summary>
        event ComponentEventHandler ComponentRemoved;

        /// <summary>
        /// Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRemoving event.
        /// </summary>
        event ComponentEventHandler ComponentRemoving;

        /// <summary>
        /// Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRename event.
        /// </summary>
        event ComponentRenameEventHandler ComponentRename;

        /// <summary>
        /// Announces to the component change service that a particular component has changed.
        /// </summary>
        void OnComponentChanged(object component, MemberDescriptor member, object oldValue, object newValue);

        /// <summary>
        /// Announces to the component change service that a particular component is changing.
        /// </summary>
        void OnComponentChanging(object component, MemberDescriptor member);
    }
}

