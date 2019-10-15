// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.PerformanceData
{
    /// <summary>
    /// Enum of friendly names to CounterSet instance type (maps directory to the native types defined in perflib.h)
    /// </summary>
    public enum CounterSetInstanceType
    {
        /// <summary>
        /// Single means that at any time CounterSet should only have at most 1 active instance.
        /// </summary>
        Single = 0,          // PERF_COUNTERSET_SINGLE_INSTANCE

        /// <summary>
        /// Multiple means that CounterSet could have multiple active instances.
        /// </summary>
        Multiple = 0x00000002, // PERF_COUNTERSET_MULTI_INSTANCES

        /// <summary>
        /// GlobalAggregate means that CounterSet could have multiple active instances, but counter consumption
        /// applications (for example, perfmon) would aggregate raw counter data from different instances.
        /// </summary>
        GlobalAggregate = 0x00000004, // PERF_COUNTERSET_SINGLE_AGGREGATE

        /// <summary>
        /// GlobalAggregateWithHistory is similar to GlobalAggregate, but counter consumption applications
        /// (for example, permfon) would aggregate raw counter data not only from active instances, but also
        /// from instances since consumption applications start.
        /// </summary>
        GlobalAggregateWithHistory = 0x0000000B, // PERF_COUNTERSET_SINGLE_AGGREGATE_HISTORY

        /// <summary>
        /// MultipleInstancesWithAggregate acts similar to Multiple, but it also generate aggregated instace
        /// "_Total" that hosts aggregated raw counter data from all other instances.
        /// </summary>
        MultipleAggregate = 0x00000006, // PERF_COUNTERSET_MULTI_AGGREGATE

        /// <summary>
        /// InstanceAggregate only exists in Longhonr Server. Counter consumption applications aggregate raw
        /// counter data for active instances with the same instance name.
        /// </summary>
        InstanceAggregate = 0x00000016  // PERF_COUNTERSET_INSTANCE_AGGREGATE
    }
}
