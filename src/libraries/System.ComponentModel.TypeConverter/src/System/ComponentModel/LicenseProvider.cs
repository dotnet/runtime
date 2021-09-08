// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel
{
    /// <summary>
    /// Provides the <see langword='abstract'/> base class for implementing a <see cref='System.ComponentModel.LicenseProvider'/>.
    /// </summary>
    public abstract class LicenseProvider
    {
        /// <summary>
        /// When overridden in a derived class, gets a license for an <paramref name="instance "/>or <paramref name="type "/>
        /// of component.
        /// </summary>
        public abstract License? GetLicense(LicenseContext context, Type type, object? instance, bool allowExceptions);
    }
}
