// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Defines the interface for extending properties to other components in a container.
    /// </summary>
    public interface IExtenderProvider
    {
        /// <summary>
        /// Specifies whether this object can provide its extender properties to
        /// the specified object.
        /// </summary>
        bool CanExtend(object extendee);
    }
}
