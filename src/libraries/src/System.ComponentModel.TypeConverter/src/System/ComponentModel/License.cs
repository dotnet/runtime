// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Provides the <see langword='abstract'/> base class for all licenses. A license is
    /// granted to a specific instance of a component.
    /// </summary>
    public abstract class License : IDisposable
    {
        /// <summary>
        /// When overridden in a derived class, gets the license key granted to this component.
        /// </summary>
        public abstract string LicenseKey { get; }

        /// <summary>
        /// When overridden in a derived class, releases the license.
        /// </summary>
        public abstract void Dispose();
    }
}
