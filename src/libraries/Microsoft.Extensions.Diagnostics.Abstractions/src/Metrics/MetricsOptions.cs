// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// Options for configuring the metrics system.
    /// </summary>
    public class MetricsOptions
    {
        /// <summary>
        /// A list of <see cref="InstrumentRule"/>'s that identify which metrics, instruments, and listeners are enabled.
        /// </summary>
        public IList<InstrumentRule> Rules { get; } = new List<InstrumentRule>();
    }
}
