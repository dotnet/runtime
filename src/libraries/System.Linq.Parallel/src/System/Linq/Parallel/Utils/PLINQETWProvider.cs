// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// PlinqEtwProvider.cs
//
// EventSource for PLINQ.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;

namespace System.Linq.Parallel
{
    /// <summary>Provides an event source for tracing PLINQ information.</summary>
    [EventSource(
        Name = "System.Linq.Parallel.PlinqEventSource",
        Guid = "159eeeec-4a14-4418-a8fe-faabcd987887")]
    internal sealed class PlinqEtwProvider : EventSource
    {
        /// <summary>
        /// Defines the singleton instance for the PLINQ ETW provider.
        /// The PLINQ Event provider GUID is {159eeeec-4a14-4418-a8fe-faabcd987887}.
        /// </summary>
        internal static PlinqEtwProvider Log = new PlinqEtwProvider();
        /// <summary>Prevent external instantiation.  All logging should go through the Log instance.</summary>
        private PlinqEtwProvider() { }

        /// <summary>Cached id for the default scheduler.</summary>
        /// <remarks>If PLINQ ever supports other schedulers, that information will need to be passed into the query events.</remarks>
        private static readonly int s_defaultSchedulerId = TaskScheduler.Default.Id;
        /// <summary>Static counter used to generate unique IDs</summary>
        private static int s_queryId;

        /// <summary>Generates the next consecutive query ID.</summary>
        [NonEvent]
        internal static int NextQueryId()
        {
            return Interlocked.Increment(ref s_queryId);
        }

        /// <summary>Enabled for all keywords.</summary>
        private const EventKeywords ALL_KEYWORDS = (EventKeywords)(-1);

        /// <summary>ETW tasks that have start/stop events.</summary>
        public static class Tasks // this name is important for EventSource
        {
            /// <summary>A parallel query.</summary>
            public const EventTask Query = (EventTask)1;
            /// <summary>A fork/join task within a query.</summary>
            public const EventTask ForkJoin = (EventTask)2;
        }

        //-----------------------------------------------------------------------------------
        //
        // PLINQ Query Event IDs (must be unique)
        //

        /// <summary>The ID of a parallel query begin event.</summary>
        private const int PARALLELQUERYBEGIN_EVENTID = 1;
        /// <summary>The ID of a parallel query end event.</summary>
        private const int PARALLELQUERYEND_EVENTID = 2;
        /// <summary>The ID of a parallel query fork event.</summary>
        private const int PARALLELQUERYFORK_EVENTID = 3;
        /// <summary>The ID of a parallel query join event.</summary>
        private const int PARALLELQUERYJOIN_EVENTID = 4;

        //-----------------------------------------------------------------------------------
        //
        // PLINQ Query Events
        //

        #region ParallelQueryBegin
        /// <summary>
        /// Denotes the entry point for a PLINQ Query, and declares the fork/join context ID
        /// which will be shared by subsequent events fired by tasks that service this query
        /// </summary>
        /// <param name="queryId">The ID of the query.</param>
        [NonEvent]
        internal void ParallelQueryBegin(int queryId)
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                int taskId = Task.CurrentId ?? 0;
                ParallelQueryBegin(s_defaultSchedulerId, taskId, queryId);
            }
        }

        /// <summary>
        /// Denotes the entry point for a PLINQ query, and declares the fork/join context ID
        /// which will be shared by subsequent events fired by tasks that service this query
        /// </summary>
        /// <param name="taskSchedulerId">The ID of the task scheduler to which the query is scheduled.</param>
        /// <param name="taskId">The ID of the task starting the query; 0 if there is no task.</param>
        /// <param name="queryId">The ID of the query.</param>
        [Event(PARALLELQUERYBEGIN_EVENTID, Level = EventLevel.Informational, Task = PlinqEtwProvider.Tasks.Query, Opcode = EventOpcode.Start)]
        private void ParallelQueryBegin(int taskSchedulerId, int taskId, int queryId)
        {
            WriteEvent(PARALLELQUERYBEGIN_EVENTID, taskSchedulerId, taskId, queryId);
        }
        #endregion

        #region ParallelQueryEnd
        /// <summary>
        /// Denotes the end of PLINQ query that was declared previously with the same query ID.
        /// </summary>
        /// <param name="queryId">The ID of the query.</param>
        [NonEvent]
        internal void ParallelQueryEnd(int queryId)
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                int taskId = Task.CurrentId ?? 0;
                ParallelQueryEnd(s_defaultSchedulerId, taskId, queryId);
            }
        }

        /// <summary>
        /// Denotes the end of PLINQ query that was declared previously with the same query ID.
        /// </summary>
        /// <param name="taskSchedulerId">The ID of the task scheduler to which the query was scheduled.</param>
        /// <param name="taskId">The ID of the task ending the query; 0 if there is no task.</param>
        /// <param name="queryId">The ID of the query.</param>
        [Event(PARALLELQUERYEND_EVENTID, Level = EventLevel.Informational, Task = PlinqEtwProvider.Tasks.Query, Opcode = EventOpcode.Stop)]
        private void ParallelQueryEnd(int taskSchedulerId, int taskId, int queryId)
        {
            WriteEvent(PARALLELQUERYEND_EVENTID, taskSchedulerId, taskId, queryId);
        }
        #endregion

        #region ParallelQueryFork
        /// <summary>
        /// Denotes the start of an individual task that will service a parallel query.
        /// Before this event is fired, the query ID must have been declared with a
        /// ParallelQueryBegin event.
        /// </summary>
        /// <param name="queryId">The ID of the query.</param>
        [NonEvent]
        internal void ParallelQueryFork(int queryId)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                int taskId = Task.CurrentId ?? 0;
                ParallelQueryFork(s_defaultSchedulerId, taskId, queryId);
            }
        }

        /// <summary>
        /// Denotes the start of an individual task that will service a parallel query.
        /// Before this event is fired, the query ID must have been declared with a
        /// ParallelQueryBegin event.
        /// </summary>
        /// <param name="taskSchedulerId">The ID of the task scheduler to which the task was scheduled.</param>
        /// <param name="taskId">The ID of the task joining the query.</param>
        /// <param name="queryId">The ID of the query.</param>
        [Event(PARALLELQUERYFORK_EVENTID, Level = EventLevel.Verbose, Task = PlinqEtwProvider.Tasks.ForkJoin, Opcode = EventOpcode.Start)]
        private void ParallelQueryFork(int taskSchedulerId, int taskId, int queryId)
        {
            WriteEvent(PARALLELQUERYFORK_EVENTID, taskSchedulerId, taskId, queryId);
        }
        #endregion

        #region ParallelQueryJoin
        /// <summary>
        /// Denotes the end of an individual task that serviced a parallel query.
        /// This should match a previous ParallelQueryFork event with a matching query ID.
        /// </summary>
        /// <param name="queryId">The ID of the query.</param>
        [NonEvent]
        internal void ParallelQueryJoin(int queryId)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
            {
                int taskId = Task.CurrentId ?? 0;
                ParallelQueryJoin(s_defaultSchedulerId, taskId, queryId);
            }
        }

        /// <summary>
        /// Denotes the end of an individual task that serviced a parallel query.
        /// This should match a previous ParallelQueryFork event with a matching query ID.
        /// </summary>
        /// <param name="taskSchedulerId">The ID of the task scheduler to which the task was scheduled.</param>
        /// <param name="taskId">The ID of the task joining the query.</param>
        /// <param name="queryId">The ID of the query.</param>
        [Event(PARALLELQUERYJOIN_EVENTID, Level = EventLevel.Verbose, Task = PlinqEtwProvider.Tasks.ForkJoin, Opcode = EventOpcode.Stop)]
        private void ParallelQueryJoin(int taskSchedulerId, int taskId, int queryId)
        {
            WriteEvent(PARALLELQUERYJOIN_EVENTID, taskSchedulerId, taskId, queryId);
        }
        #endregion

    }
}
