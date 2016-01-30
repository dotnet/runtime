// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ParallelMergeOptions.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

namespace System.Linq
{
    /// <summary>
    /// Specifies the preferred type of output merge to use in a query. This is a hint only, and may not be
    /// respected by the system when parallelizing all queries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use <b>NotBuffered</b> for queries that will be consumed and output as streams, this has the lowest latency
    /// between beginning query execution and elements being yielded. For some queries, such as those involving a 
    /// sort (OrderBy, OrderByDescending), buffering is essential and a hint of NotBuffered or AutoBuffered will 
    /// be ignored.
    /// </para>
    /// <para>
    /// Use <b>AutoBuffered</b> for most cases; this is the default.  It strikes a balance between latency and
    /// overall performance.
    /// </para>
    /// <para>
    /// Use <b>FullyBuffered</b> for queries when the entire output can be processed before the information is 
    /// needed. This option offers the best performance when all of the output can be accumulated before yielding
    /// any information, though it is not suitable for stream processing or showing partial results mid-query.
    /// </para>
    /// </remarks>
    public enum ParallelMergeOptions
    {
        /// <summary>
        /// Use the default merge type, which is AutoBuffered.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Use a merge without output buffers. As soon as result elements have been computed, 
        /// make that element available to the consumer of the query.
        /// </summary>
        NotBuffered = 1,

        /// <summary>
        /// Use a merge with output buffers of a size chosen by the system. Results
        /// will accumulate into an output buffer before they are available to the consumer of
        /// the query.
        /// </summary>
        AutoBuffered = 2,

        /// <summary>
        /// Use a merge with full output buffers. The system will accumulate all of the
        /// results before making any of them available to the consumer of the query.
        /// </summary>
        FullyBuffered = 3
    }
}
