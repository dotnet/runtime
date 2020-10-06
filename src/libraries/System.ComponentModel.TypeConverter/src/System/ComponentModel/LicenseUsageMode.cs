// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies when the license can be used.
    /// </summary>
    public enum LicenseUsageMode
    {
        /// <summary>
        /// Used during runtime.
        /// </summary>
        Runtime,

        /// <summary>
        /// Used during design time by a visual designer or the compiler.
        /// </summary>
        Designtime,
    }
}
