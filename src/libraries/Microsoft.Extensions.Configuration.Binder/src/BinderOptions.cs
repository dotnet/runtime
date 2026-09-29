// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Specifies options used by the <see cref="ConfigurationBinder"/>.
    /// </summary>
    public class BinderOptions
    {
        /// <summary>
        /// Gets or sets a value that indicates whether the binder attempts to set all properties or only public properties.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the binder attempts to set all non-read-only properties; <see langword="false" /> if only public properties are set.
        /// </value>
        public bool BindNonPublicProperties { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether exceptions are thrown when converting a value or when a configuration
        /// key is found for which the provided model object doesn't have an appropriate property that matches the key's name.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if an <see cref="System.InvalidOperationException"/> is thrown with a description; <see langword="false" /> if no exceptions are thrown. The default is <see langword="false" />.
        /// </value>
        public bool ErrorOnUnknownConfiguration { get; set; }
    }
}
