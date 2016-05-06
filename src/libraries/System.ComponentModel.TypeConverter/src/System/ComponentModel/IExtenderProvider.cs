// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>
    ///       Defines the interface
    ///       for extending properties to other components in a container.
    ///    </para>
    /// </summary>
    public interface IExtenderProvider
    {
        /// <summary>
        ///    <para>
        ///       Specifies
        ///       whether this object can provide its extender properties to
        ///       the specified object.
        ///    </para>
        /// </summary>
        bool CanExtend(object extendee);
    }
}
