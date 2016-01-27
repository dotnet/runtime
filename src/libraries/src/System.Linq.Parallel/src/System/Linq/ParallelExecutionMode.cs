// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ParallelQueryExecutionMode.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Linq
{
    /// <summary>
    /// The query execution mode is a hint that specifies how the system should handle
    /// performance trade-offs when parallelizing queries.
    /// </summary>
    public enum ParallelExecutionMode
    {
        /// <summary>
        /// By default, the system will use algorithms for queries
        /// that are ripe for parallelism and will avoid algorithms with high 
        /// overheads that will likely result in slow downs for parallel execution. 
        /// </summary>
        Default = 0,

        /// <summary>
        /// Parallelize the entire query, even if that means using high-overhead algorithms.
        /// </summary>
        ForceParallelism = 1,
    }
}
