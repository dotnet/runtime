// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// EventSource for Task.Parallel.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Security;
using System.Text;

namespace System.Threading.Tasks
{
    /// <summary>Provides an event source for tracing TPL information.</summary>
    [EventSource(Name = "System.Threading.Tasks.Parallel.EventSource")]
    internal sealed class ParallelEtwProvider : EventSource
    {
        private const string EventSourceSuppressMessage = "Parameters to this method are primitive and are trimmer safe";
        /// <summary>
        /// Defines the singleton instance for the Task.Parallel ETW provider.
        /// </summary>
        public static readonly ParallelEtwProvider Log = new ParallelEtwProvider();

        /// <summary>Prevent external instantiation. All logging should go through the Log instance.</summary>
        private ParallelEtwProvider() { }

        /// <summary>Type of a fork/join operation.</summary>
        public enum ForkJoinOperationType
        {
            /// <summary>Parallel.Invoke.</summary>
            ParallelInvoke = 1,
            /// <summary>Parallel.For.</summary>
            ParallelFor = 2,
            /// <summary>Parallel.ForEach.</summary>
            ParallelForEach = 3
        }

        /// <summary>ETW tasks that have start/stop events.</summary>
        public static class Tasks
        {  // this name is important for EventSource
            /// <summary>A parallel loop.</summary>
            public const EventTask Loop = (EventTask)1;
            /// <summary>A parallel invoke.</summary>
            public const EventTask Invoke = (EventTask)2;

            // Do not use 3, it is used for "TaskExecute" in the TPL provider.
            // Do not use 4, it is used for "TaskWait" in the TPL provider.

            /// <summary>A fork/join task within a loop or invoke.</summary>
            public const EventTask ForkJoin = (EventTask)5;
        }


        /// <summary>Enabled for all keywords.</summary>
        private const EventKeywords ALL_KEYWORDS = (EventKeywords)(-1);


        //-----------------------------------------------------------------------------------
        //
        // TPL Event IDs (must be unique)
        //

        /// <summary>The beginning of a parallel loop.</summary>
        private const int PARALLELLOOPBEGIN_ID = 1;

        /// <summary>The ending of a parallel loop.</summary>
        private const int PARALLELLOOPEND_ID = 2;

        /// <summary>The beginning of a parallel invoke.</summary>
        private const int PARALLELINVOKEBEGIN_ID = 3;

        /// <summary>The ending of a parallel invoke.</summary>
        private const int PARALLELINVOKEEND_ID = 4;

        /// <summary>A task entering a fork/join construct.</summary>
        private const int PARALLELFORK_ID = 5;

        /// <summary>A task leaving a fork/join construct.</summary>
        private const int PARALLELJOIN_ID = 6;


        //-----------------------------------------------------------------------------------
        //
        // Parallel Events
        //

        #region ParallelLoopBegin
        /// <summary>
        /// Denotes the entry point for a Parallel.For or Parallel.ForEach loop
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="ForkJoinContextID">The loop ID.</param>
        /// <param name="OperationType">The kind of fork/join operation.</param>
        /// <param name="InclusiveFrom">The lower bound of the loop.</param>
        /// <param name="ExclusiveTo">The upper bound of the loop.</param>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [Event(PARALLELLOOPBEGIN_ID, Level = EventLevel.Informational, Task = ParallelEtwProvider.Tasks.Loop, Opcode = EventOpcode.Start)]
        public void ParallelLoopBegin(int OriginatingTaskSchedulerID, int OriginatingTaskID,      // PFX_COMMON_EVENT_HEADER
                                      int ForkJoinContextID, ForkJoinOperationType OperationType, // PFX_FORKJOIN_COMMON_EVENT_HEADER
                                      long InclusiveFrom, long ExclusiveTo)
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                // There is no explicit WriteEvent() overload matching this event's fields. Therefore calling
                // WriteEvent() would hit the "params" overload, which leads to an object allocation every time
                // this event is fired. To prevent that problem we will call WriteEventCore(), which works with
                // a stack based EventData array populated with the event fields.
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[6];

                    eventPayload[0] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID))
                    };
                    eventPayload[1] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&OriginatingTaskID))
                    };
                    eventPayload[2] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&ForkJoinContextID))
                    };
                    eventPayload[3] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&OperationType))
                    };
                    eventPayload[4] = new EventData
                    {
                        Size = sizeof(long),
                        DataPointer = ((IntPtr)(&InclusiveFrom))
                    };
                    eventPayload[5] = new EventData
                    {
                        Size = sizeof(long),
                        DataPointer = ((IntPtr)(&ExclusiveTo))
                    };

                    WriteEventCore(PARALLELLOOPBEGIN_ID, 6, eventPayload);
                }
            }
        }
        #endregion ParallelLoopBegin

        #region ParallelLoopEnd
        /// <summary>
        /// Denotes the end of a Parallel.For or Parallel.ForEach loop.
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="ForkJoinContextID">The loop ID.</param>
        /// <param name="TotalIterations">the total number of iterations processed.</param>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [Event(PARALLELLOOPEND_ID, Level = EventLevel.Informational, Task = ParallelEtwProvider.Tasks.Loop, Opcode = EventOpcode.Stop)]
        public void ParallelLoopEnd(int OriginatingTaskSchedulerID, int OriginatingTaskID,  // PFX_COMMON_EVENT_HEADER
                                    int ForkJoinContextID, long TotalIterations)
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                // There is no explicit WriteEvent() overload matching this event's fields.
                // Therefore calling WriteEvent() would hit the "params" overload, which leads to an object allocation every time this event is fired.
                // To prevent that problem we will call WriteEventCore(), which works with a stack based EventData array populated with the event fields
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[4];

                    eventPayload[0] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID))
                    };
                    eventPayload[1] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&OriginatingTaskID))
                    };
                    eventPayload[2] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&ForkJoinContextID))
                    };
                    eventPayload[3] = new EventData
                    {
                        Size = sizeof(long),
                        DataPointer = ((IntPtr)(&TotalIterations))
                    };

                    WriteEventCore(PARALLELLOOPEND_ID, 4, eventPayload);
                }
            }
        }
        #endregion ParallelLoopEnd

        #region ParallelInvokeBegin
        /// <summary>Denotes the entry point for a Parallel.Invoke call.</summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="ForkJoinContextID">The invoke ID.</param>
        /// <param name="OperationType">The kind of fork/join operation.</param>
        /// <param name="ActionCount">The number of actions being invoked.</param>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [Event(PARALLELINVOKEBEGIN_ID, Level = EventLevel.Informational, Task = ParallelEtwProvider.Tasks.Invoke, Opcode = EventOpcode.Start)]
        public void ParallelInvokeBegin(int OriginatingTaskSchedulerID, int OriginatingTaskID,      // PFX_COMMON_EVENT_HEADER
                                        int ForkJoinContextID, ForkJoinOperationType OperationType, // PFX_FORKJOIN_COMMON_EVENT_HEADER
                                        int ActionCount)
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                // There is no explicit WriteEvent() overload matching this event's fields.
                // Therefore calling WriteEvent() would hit the "params" overload, which leads to an object allocation every time this event is fired.
                // To prevent that problem we will call WriteEventCore(), which works with a stack based EventData array populated with the event fields
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[5];

                    eventPayload[0] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID))
                    };
                    eventPayload[1] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&OriginatingTaskID))
                    };
                    eventPayload[2] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&ForkJoinContextID))
                    };
                    eventPayload[3] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&OperationType))
                    };
                    eventPayload[4] = new EventData
                    {
                        Size = sizeof(int),
                        DataPointer = ((IntPtr)(&ActionCount))
                    };

                    WriteEventCore(PARALLELINVOKEBEGIN_ID, 5, eventPayload);
                }
            }
        }
        #endregion ParallelInvokeBegin

        #region ParallelInvokeEnd
        /// <summary>
        /// Denotes the exit point for a Parallel.Invoke call.
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="ForkJoinContextID">The invoke ID.</param>
        [Event(PARALLELINVOKEEND_ID, Level = EventLevel.Informational, Task = ParallelEtwProvider.Tasks.Invoke, Opcode = EventOpcode.Stop)]
        public void ParallelInvokeEnd(int OriginatingTaskSchedulerID, int OriginatingTaskID,  // PFX_COMMON_EVENT_HEADER
                                      int ForkJoinContextID)
        {
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
                WriteEvent(PARALLELINVOKEEND_ID, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID);
        }
        #endregion ParallelInvokeEnd

        #region ParallelFork
        /// <summary>
        /// Denotes the start of an individual task that's part of a fork/join context.
        /// Before this event is fired, the start of the new fork/join context will be marked
        /// with another event that declares a unique context ID.
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="ForkJoinContextID">The invoke ID.</param>
        [Event(PARALLELFORK_ID, Level = EventLevel.Verbose, Task = ParallelEtwProvider.Tasks.ForkJoin, Opcode = EventOpcode.Start)]
        public void ParallelFork(int OriginatingTaskSchedulerID, int OriginatingTaskID,  // PFX_COMMON_EVENT_HEADER
                                 int ForkJoinContextID)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
                WriteEvent(PARALLELFORK_ID, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID);
        }
        #endregion ParallelFork

        #region ParallelJoin
        /// <summary>
        /// Denotes the end of an individual task that's part of a fork/join context.
        /// This should match a previous ParallelFork event with a matching "OriginatingTaskID"
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="ForkJoinContextID">The invoke ID.</param>
        [Event(PARALLELJOIN_ID, Level = EventLevel.Verbose, Task = ParallelEtwProvider.Tasks.ForkJoin, Opcode = EventOpcode.Stop)]
        public void ParallelJoin(int OriginatingTaskSchedulerID, int OriginatingTaskID,  // PFX_COMMON_EVENT_HEADER
                                 int ForkJoinContextID)
        {
            if (IsEnabled(EventLevel.Verbose, ALL_KEYWORDS))
                WriteEvent(PARALLELJOIN_ID, OriginatingTaskSchedulerID, OriginatingTaskID, ForkJoinContextID);
        }
        #endregion ParallelJoin

    }  // class ParallelEtwProvider
}  // namespace
