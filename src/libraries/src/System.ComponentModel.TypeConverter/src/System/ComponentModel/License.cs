// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    /// <para>Provides the <see langword='abstract'/> base class for all licenses. A license is
    ///    granted to a specific instance of a component.</para>
    /// </summary>
    public abstract class License : IDisposable
    {
        /// <summary>
        ///    <para>When overridden in a derived class, gets the license key granted to this component.</para>
        /// </summary>
        public abstract string LicenseKey { get; }

        /// <summary>
        ///    <para>When overridden in a derived class, releases the license.</para>
        /// </summary>
        public abstract void Dispose();
    }
}
