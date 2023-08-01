// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public static class MetricsBuilderConfigurationExtensions
    {
        // TODO:
        public static IMetricsBuilder AddConfiguration(this IMetricsBuilder builder, IConfiguration configuration) => builder;
    }
}
