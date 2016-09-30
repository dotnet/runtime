// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using System.ComponentModel;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// <para>Provides an interface to add and remove the event handlers for System.ComponentModel.Design.IComponentChangeService.ComponentAdded, System.ComponentModel.Design.IComponentChangeService.ComponentAdding, System.ComponentModel.Design.IComponentChangeService.ComponentChanged, System.ComponentModel.Design.IComponentChangeService.ComponentChanging, System.ComponentModel.Design.IComponentChangeService.ComponentRemoved, System.ComponentModel.Design.IComponentChangeService.ComponentRemoving, and System.ComponentModel.Design.IComponentChangeService.ComponentRename events.</para>
    /// </summary>

    public interface IComponentChangeService
    {
        /// <summary>
        /// <para>Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentAdded event.</para>
        /// </summary>
        event ComponentEventHandler ComponentAdded;

        /// <summary>
        /// <para>Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentAdding event.</para>
        /// </summary>
        event ComponentEventHandler ComponentAdding;

        /// <summary>
        /// <para>Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentChanged event.</para>
        /// </summary>
        event ComponentChangedEventHandler ComponentChanged;

        /// <summary>
        /// <para>Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.ComponentChanging event.</para>
        /// </summary>
        event ComponentChangingEventHandler ComponentChanging;

        /// <summary>
        /// <para>Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRemoved event.</para>
        /// </summary>
        event ComponentEventHandler ComponentRemoved;

        /// <summary>
        /// <para>Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRemoving event.</para>
        /// </summary>
        event ComponentEventHandler ComponentRemoving;

        /// <summary>
        /// <para>Adds an event handler for the System.ComponentModel.Design.IComponentChangeService.OnComponentRename event.</para>
        /// </summary>
        event ComponentRenameEventHandler ComponentRename;

        /// <summary>
        ///    <para>Announces to the component change service that a particular component has changed.</para>
        /// </summary>
        void OnComponentChanged(object component, MemberDescriptor member, object oldValue, object newValue);

        /// <summary>
        ///    <para>Announces to the component change service that a particular component is changing.</para>
        /// </summary>
        void OnComponentChanging(object component, MemberDescriptor member);
    }
}

