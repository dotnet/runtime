// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.Configuration
{
    internal sealed class LoggingConfiguration
    {
        public IConfiguration Configuration { get; }

        public LoggingConfiguration(IConfiguration configuration)
        {
            Configuration = configuration;
        }
    }
}
