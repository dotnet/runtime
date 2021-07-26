// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Options class used by the <see cref="ConfigurationBinder"/>.
    /// </summary>
    public class BinderOptions
    {
        /// <summary>
        /// When false (the default), the binder will only attempt to set public properties.
        /// If true, the binder will attempt to set all non read-only properties.
        /// </summary>
        public bool BindNonPublicProperties { get; set; }

        /// <summary>
        /// When false (the default), no exceptions are thrown when a configuration key is found for which the
        /// provided model object does not have an appropriate property which matches the key's name.
        /// When true, an <see cref="System.InvalidOperationException"/> is thrown with a description
        /// of the missing properties.
        /// </summary>
        public bool ErrorOnUnknownConfiguration { get; set; }
    }
}
