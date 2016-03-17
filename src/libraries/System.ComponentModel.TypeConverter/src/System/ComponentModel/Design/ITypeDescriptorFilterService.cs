// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.ComponentModel.Design
{
    /// <devdoc>
    ///    <para>
    ///       Modifies the set of type descriptors that a component provides.
    ///    </para>
    /// </devdoc>
    public interface ITypeDescriptorFilterService
    {
        /// <devdoc>
        ///    <para>
        ///       Provides a way to filter the attributes from a component that are displayed to the user.
        ///    </para>
        /// </devdoc>
        bool FilterAttributes(IComponent component, IDictionary attributes);

        /// <devdoc>
        ///    <para>
        ///       Provides a way to filter the events from a component that are displayed to the user.
        ///    </para>
        /// </devdoc>
        bool FilterEvents(IComponent component, IDictionary events);

        /// <devdoc>
        ///    <para>
        ///       Provides a way to filter the properties from a component that are displayed to the user.
        ///    </para>
        /// </devdoc>
        bool FilterProperties(IComponent component, IDictionary properties);
    }
}
