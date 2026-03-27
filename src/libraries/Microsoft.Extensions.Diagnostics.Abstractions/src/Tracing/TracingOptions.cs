// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Configuration
{
    /// <summary>
    /// Represents options for configuring the tracing system.
    /// </summary>
    public class TracingOptions
    {
        /// <summary>
        /// Gets a list of activity rules that identifies which activity sources, activities, and listeners are enabled.
        /// </summary>
        public IList<TracingRule> Rules { get; } = new List<TracingRule>();
    }
}
