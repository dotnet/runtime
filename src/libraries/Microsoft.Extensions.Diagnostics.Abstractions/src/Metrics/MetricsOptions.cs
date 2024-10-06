// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// Represents options for configuring the metrics system.
    /// </summary>
    public class MetricsOptions
    {
        /// <summary>
        /// Gets a list of instrument rules that identifies which metrics, instruments, and listeners are enabled.
        /// </summary>
        public IList<InstrumentRule> Rules { get; } = new List<InstrumentRule>();
    }
}
