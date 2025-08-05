// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Contains information about a file load exception.
    /// </summary>
    public class FileLoadExceptionContext
    {
        /// <summary>
        /// Gets or sets the <see cref="FileConfigurationProvider"/> that caused the exception.
        /// </summary>
        public FileConfigurationProvider Provider { get; set; } = null!;

        /// <summary>
        /// Gets or sets the exception that occurred in Load.
        /// </summary>
        public Exception Exception { get; set; } = null!;

        /// <summary>
        /// Gets or sets a value that indicates whether the exception is rethrown.
        /// </summary>
        /// <value>
        /// <see langword="true" /> if the exception isn't rethrown; otherwise, <see langword="false" />.
        /// </value>
        public bool Ignore { get; set; }
    }
}
