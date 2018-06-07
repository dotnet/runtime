// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel.Design
{
    /// <summary>
    /// Modifies the set of type descriptors that a component provides.
    /// </summary>
    public interface ITypeDescriptorFilterService
    {
        /// <summary>
        /// Provides a way to filter the attributes from a component that are displayed to the user.
        /// </summary>
        bool FilterAttributes(IComponent component, IDictionary attributes);

        /// <summary>
        /// Provides a way to filter the events from a component that are displayed to the user.
        /// </summary>
        bool FilterEvents(IComponent component, IDictionary events);

        /// <summary>
        /// Provides a way to filter the properties from a component that are displayed to the user.
        /// </summary>
        bool FilterProperties(IComponent component, IDictionary properties);
    }
}
