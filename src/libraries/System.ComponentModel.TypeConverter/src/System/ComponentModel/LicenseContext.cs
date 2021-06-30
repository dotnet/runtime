// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies when the licensed object can be used.
    /// </summary>
    public class LicenseContext : IServiceProvider
    {
        /// <summary>
        /// When overridden in a derived class, gets a value that specifies when a license can be used.
        /// </summary>
        public virtual LicenseUsageMode UsageMode => LicenseUsageMode.Runtime;

        /// <summary>
        /// When overridden in a derived class, gets a saved license
        /// key for the specified type, from the specified resource assembly.
        /// </summary>
        public virtual string? GetSavedLicenseKey(Type type, Assembly? resourceAssembly) => null;

        /// <summary>
        /// When overridden in a derived class, will return an object that implements the asked for service.
        /// </summary>
        public virtual object? GetService(Type type) => null;

        /// <summary>
        /// When overridden in a derived class, sets a license key for the specified type.
        /// </summary>
        public virtual void SetSavedLicenseKey(Type type, string key)
        {
            // no-op;
        }
    }
}
