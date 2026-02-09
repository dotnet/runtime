// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

namespace Microsoft.Extensions.Logging
{
    [UnsupportedOSPlatform("browser")]
    internal static partial class EventLogExtensions
    {
        internal static IConfiguration GetFormatterOptionsSection(this ILoggerProviderConfiguration<EventLogLoggerProvider> providerConfiguration)
        {
            return providerConfiguration.Configuration.GetSection("FormatterOptions");
        }
    }
}
