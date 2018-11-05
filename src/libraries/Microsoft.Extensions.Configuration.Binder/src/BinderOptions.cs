// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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