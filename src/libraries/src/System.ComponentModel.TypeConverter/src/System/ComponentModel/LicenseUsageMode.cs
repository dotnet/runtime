// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies when the license can be used.</para>
    /// </summary>
    public enum LicenseUsageMode
    {
        /// <summary>
        ///    <para>
        ///       Used during runtime.
        ///    </para>
        /// </summary>
        Runtime,

        /// <summary>
        ///    <para>
        ///       Used during design time by a visual designer or the compiler.
        ///    </para>
        /// </summary>
        Designtime,
    }
}
