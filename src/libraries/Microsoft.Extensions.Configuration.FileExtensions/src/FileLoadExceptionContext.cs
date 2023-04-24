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
        /// The <see cref="FileConfigurationProvider"/> that caused the exception.
        /// </summary>
        public FileConfigurationProvider Provider { get; set; } = null!;

        /// <summary>
        /// The exception that occurred in Load.
        /// </summary>
        public Exception Exception { get; set; } = null!;

        /// <summary>
        /// If true, the exception will not be rethrown.
        /// </summary>
        public bool Ignore { get; set; }
    }
}
