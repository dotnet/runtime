// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public class MetricsOptions
    {
        public IList<InstrumentRule> Rules { get; } = new List<InstrumentRule>();
    }
}
