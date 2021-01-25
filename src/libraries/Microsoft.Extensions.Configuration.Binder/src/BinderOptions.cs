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
    }
}
