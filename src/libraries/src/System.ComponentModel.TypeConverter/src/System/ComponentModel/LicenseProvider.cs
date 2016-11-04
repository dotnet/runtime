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
    /// <para>Provides the <see langword='abstract'/> base class for implementing a <see cref='System.ComponentModel.LicenseProvider'/>.</para>
    /// </summary>
    public abstract class LicenseProvider
    {
        /// <summary>
        ///    <para>When overridden in a derived class, gets a license for an <paramref name="instance "/>or <paramref name="type "/>
        ///       of component.</para>
        /// </summary>
        public abstract License GetLicense(LicenseContext context, Type type, object instance, bool allowExceptions);
    }
}
