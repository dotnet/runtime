// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>
    ///       Specifies the function scope of
    ///       a tab in the properties window.
    ///    </para>
    /// </summary>
    public enum PropertyTabScope
    {
        /// <summary>
        ///    <para>
        ///       This tab will be added to the properties window and can never be
        ///       removed.
        ///    </para>
        /// </summary>
        Static = 0,

        /// <summary>
        ///    <para>
        ///       This tab will be added to the properties window and can
        ///       only be explicitly removed by a component outside the properties window.
        ///    </para>
        /// </summary>
        Global = 1,

        /// <summary>
        ///    <para>
        ///       This tab will be added to the properties window
        ///       and will be removed when the currently selected document changes. This tab is relevant to
        ///       items on the current document.
        ///    </para>
        /// </summary>
        Document = 2,

        /// <summary>
        ///    <para>
        ///       This tab will be added to the properties window for the current component only, and is
        ///       removed when the component is no longer selected.
        ///    </para>
        /// </summary>
        Component = 3,
    }
}
