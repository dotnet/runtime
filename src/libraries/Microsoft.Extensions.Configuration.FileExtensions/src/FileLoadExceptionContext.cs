// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public FileConfigurationProvider Provider { get; set; }

        /// <summary>
        /// The exception that occured in Load.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// If true, the exception will not be rethrown.
        /// </summary>
        public bool Ignore { get; set; }
    }
}