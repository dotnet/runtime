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

        // Used by the IServiceCollection overload of UseWindowsService to indicated that WindowsServiceLifetime
        // should verify IHostEnvironment.ContentRootPath = AppContext.BaseDirectory (usually the default).
        // This should also be the content root for the IHostBuilder overload unless it's been overridden, but
        // we don't want to break people who might have successfully overridden IHostBuilder's ContentRoot.
        internal bool ValidateContentRoot { get; set; }
    }
}
