// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies identifiers that indicate the nature of the refresh, for use
    /// in refreshing the design time view.
    /// </summary>
    public enum RefreshProperties
    {
        /// <summary>
        /// Indicates to use the no refresh mode.
        /// </summary>
        None,

        /// <summary>
        /// Indicates to use the refresh all refresh mode.
        /// </summary>
        All,

        /// <summary>
        /// Indicates to use the repaint refresh mode.
        /// </summary>
        Repaint,
    }
}
