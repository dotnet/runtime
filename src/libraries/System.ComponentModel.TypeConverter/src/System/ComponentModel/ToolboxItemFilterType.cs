// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.ComponentModel
{
    /// <summary>
    ///     Specifies the type of filter in a ToolboxItemFilterAttribute.
    /// </summary>
    public enum ToolboxItemFilterType
    {
        /// <summary>
        ///     Specifies that a toolbox item filter string may be allowed.  Allowed is typically used to
        ///     specify that you support the filter string, but don't care if it is accepted or rejected.
        /// </summary>
        Allow,

        /// <summary>
        ///     Specifies that a toolbox item filter string will require custom processing.  This is generally
        ///     specified on the root designer class to indicate that the designer wishes to accept or reject
        ///     a toolbox item through code.  The designer must implement IToolboxUser's IsSupported method.
        /// </summary>
        Custom,

        /// <summary>
        ///     Specifies that a toolbox item filter string should be rejected.  If a designer and a component
        ///     class both have the filter string and one has a type of Prevent, the toolbox item will not
        ///     be available.
        /// </summary>
        Prevent,

        /// <summary>
        ///     Specifies that a toolbox item filter string must be present for a toolbox item to be enabled.
        ///     A designer and component class must both have the filter string, and neither may have a filter
        ///     type of Prevent.
        /// </summary>
        Require
    }
}
