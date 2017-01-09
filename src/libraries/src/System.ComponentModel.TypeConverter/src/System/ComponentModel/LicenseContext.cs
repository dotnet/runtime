// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies when the licensed object can be used.</para>
    /// </summary>
    public class LicenseContext : IServiceProvider
    {
        /// <summary>
        ///    <para>When overridden in a derived class, gets a value that specifies when a license can be used.</para>
        /// </summary>
        public virtual LicenseUsageMode UsageMode => LicenseUsageMode.Runtime;

        /// <summary>
        ///    <para>When overridden in a derived class, gets a saved license 
        ///       key for the specified type, from the specified resource assembly.</para>
        /// </summary>
        public virtual string GetSavedLicenseKey(Type type, Assembly resourceAssembly)
        {
            return null;
        }

        /// <summary>
        ///    <para>When overridden in a derived class, will return an object that implements the asked for service.</para>
        /// </summary>
        public virtual object GetService(Type type)
        {
            return null;
        }

        /// <summary>
        ///    <para>When overridden in a derived class, sets a license key for the specified type.</para>
        /// </summary>
        public virtual void SetSavedLicenseKey(Type type, string key)
        {
            // no-op;
        }
    }
}
