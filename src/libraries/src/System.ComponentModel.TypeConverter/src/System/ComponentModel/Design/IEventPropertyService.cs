// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ComponentModel;
using Microsoft.Win32;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// <para>Provides a set of useful methods for binding <see cref='System.ComponentModel.EventDescriptor'/> objects to user code.</para>
    /// </summary>

    public interface IEventBindingService
    {
        /// <summary>
        ///     This creates a name for an event handling method for the given component
        ///     and event.  The name that is created is guaranteed to be unique in the user's source
        ///     code.
        /// </summary>
        string CreateUniqueMethodName(IComponent component, EventDescriptor e);

        /// <summary>
        ///     Retrieves a collection of strings.  Each string is the name of a method
        ///     in user code that has a signature that is compatible with the given event.
        /// </summary>
        ICollection GetCompatibleMethods(EventDescriptor e);

        /// <summary>
        ///     For properties that are representing events, this will return the event
        ///     that the property represents.
        /// </summary>
        EventDescriptor GetEvent(PropertyDescriptor property);

        /// <summary>
        ///    <para>Converts a set of event descriptors to a set of property descriptors.</para>
        /// </summary>
        PropertyDescriptorCollection GetEventProperties(EventDescriptorCollection events);

        /// <summary>
        ///    <para>
        ///       Converts a single event to a property.
        ///    </para>
        /// </summary>
        PropertyDescriptor GetEventProperty(EventDescriptor e);

        /// <summary>
        ///     Displays the user code for the designer.  This will return true if the user
        ///     code could be displayed, or false otherwise.
        /// </summary>
        bool ShowCode();

        /// <summary>
        ///     Displays the user code for the designer.  This will return true if the user
        ///     code could be displayed, or false otherwise.
        /// </summary>
        bool ShowCode(int lineNumber);

        /// <summary>
        ///     Displays the user code for the given event.  This will return true if the user
        ///     code could be displayed, or false otherwise.
        /// </summary>
        bool ShowCode(IComponent component, EventDescriptor e);
    }
}

