// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Hosting
{
    public class WindowsServiceLifetimeOptions
    {
        /// <summary>
        /// The name used to identify the service to the system.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;
    }
}
