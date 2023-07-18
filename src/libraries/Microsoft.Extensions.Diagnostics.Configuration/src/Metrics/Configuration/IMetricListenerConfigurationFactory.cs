// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Diagnostics.Metrics.Configuration
{
    public interface IMetricListenerConfigurationFactory
    {
        IConfiguration GetConfiguration(Type listenerType);
    }
}
