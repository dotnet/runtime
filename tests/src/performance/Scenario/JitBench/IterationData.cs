// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance.Api;

namespace JitBench
{
    /// <summary>
    /// Interface used to buffer each scenario iteration/run for later post processing.
    /// </summary>
    internal class IterationData
    {
        public ScenarioExecutionResult ScenarioExecutionResult { get; set; }

        public string StandardOutput { get; set; }

        public double StartupTime { get; set; }

        public double FirstRequestTime { get; set; }

        public double SteadystateTime { get; set; }
    }
}
