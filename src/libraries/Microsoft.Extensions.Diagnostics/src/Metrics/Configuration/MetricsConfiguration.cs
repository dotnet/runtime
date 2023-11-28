// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Metrics.Configuration
{
    internal sealed class MetricsConfiguration
    {
        public MetricsConfiguration(IConfiguration configuration)
        {
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public IConfiguration Configuration { get; }
    }
}
