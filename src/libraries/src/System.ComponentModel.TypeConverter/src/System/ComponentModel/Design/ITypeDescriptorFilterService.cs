// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Diagnostics;
using System;
using System.Collections;
using System.ComponentModel;

namespace System.ComponentModel.Design
{
    /// <summary>
    ///    <para>
    ///       Modifies the set of type descriptors that a component
    ///       provides.
    ///    </para>
    /// </summary>
    public interface ITypeDescriptorFilterService
    {
        /// <summary>
        ///    <para>
        ///       Provides a way to filter the attributes from a component that are displayed to the user.
        ///    </para>
        /// </summary>
        bool FilterAttributes(IComponent component, IDictionary attributes);

        /// <summary>
        ///    <para>
        ///       Provides a way to filter the events from a component that are displayed to the user.
        ///    </para>
        /// </summary>
        bool FilterEvents(IComponent component, IDictionary events);

        /// <summary>
        ///    <para>
        ///       Provides a way to filter the properties from a component that are displayed to the user.
        ///    </para>
        /// </summary>
        bool FilterProperties(IComponent component, IDictionary properties);
    }
}

