// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>
    ///       Defines the interface
    ///       for extending properties to other components in a container.
    ///    </para>
    /// </devdoc>
    public interface IExtenderProvider
    {
        /// <devdoc>
        ///    <para>
        ///       Specifies
        ///       whether this object can provide its extender properties to
        ///       the specified object.
        ///    </para>
        /// </devdoc>
        bool CanExtend(object extendee);
    }
}
