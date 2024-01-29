// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    /// <summary>Provides an event source for tracing TPL information.</summary>
    [EventSource(
        Name = "System.Threading.Tasks.TplEventSource",
        Guid = "2e5dba47-a3d2-4d16-8ee0-6671ffdcd7b5"
        )]
    [EventSourceAutoGenerate]
    internal sealed partial class TplEventSource : EventSource
    {
        private const string EventSourceSuppressMessage = "Parameters to this method are primitive and are trimmer safe";
        /// Used to determine if tasks should generate Activity IDs for themselves
        internal bool TasksSetActivityIds;        // This keyword is set
        internal bool Debug;
        private bool DebugActivityId;

        private const int DefaultAppDomainID = 1;

        /// <summary>
        /// Get callbacks when the ETW sends us commands`
        /// </summary>
        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.TasksFlowActivityIds))
                ActivityTracker.Instance.Enable();
            else
                TasksSetActivityIds = IsEnabled(EventLevel.Informational, Keywords.TasksSetActivityIds);

            Debug = IsEnabled(EventLevel.Informational, Keywords.Debug);
            DebugActivityId = IsEnabled(EventLevel.Informational, Keywords.DebugActivityId);
        }

        /// <summary>
        /// Defines the singleton instance for the TPL ETW provider.
        /// </summary>
        public static readonly TplEventSource Log = new TplEventSource();

        // Parameterized constructor to block initialization and ensure the EventSourceGenerator is creating the default constructor
        // as you can't make a constructor partial.
        private TplEventSource(int _) { }

        /// <summary>Configured behavior of a task wait operation.</summary>
        public enum TaskWaitBehavior : int
        {
            /// <summary>A synchronous wait.</summary>
            Synchronous = 1,
            /// <summary>An asynchronous await.</summary>
            Asynchronous = 2
        }

        /// <summary>ETW tasks that have start/stop events.</summary>
        public static class Tasks // this name is important for EventSource
        {
            /// <summary>A parallel loop.</summary>
            public const EventTask Loop = (EventTask)1;
            /// <summary>A parallel invoke.</summary>
            public const EventTask Invoke = (EventTask)2;
            /// <summary>Executing a Task.</summary>
            public const EventTask TaskExecute = (EventTask)3;
            /// <summary>Waiting on a Task.</summary>
            public const EventTask TaskWait = (EventTask)4;
            /// <summary>A fork/join task within a loop or invoke.</summary>
            public const EventTask ForkJoin = (EventTask)5;
            /// <summary>A task is scheduled to execute.</summary>
            public const EventTask TaskScheduled = (EventTask)6;
            /// <summary>An await task continuation is scheduled to execute.</summary>
            public const EventTask AwaitTaskContinuationScheduled = (EventTask)7;

            /// <summary>AsyncCausalityFunctionality.</summary>
            public const EventTask TraceOperation = (EventTask)8;
            public const EventTask TraceSynchronousWork = (EventTask)9;
        }

        public static class Keywords // this name is important for EventSource
        {
            /// <summary>
            /// Only the most basic information about the workings of the task library
            /// This sets activity IDS and logs when tasks are schedules (or waits begin)
            /// But are otherwise silent
            /// </summary>
            public const EventKeywords TaskTransfer = (EventKeywords)1;
            /// <summary>
            /// TaskTranser events plus events when tasks start and stop
            /// </summary>
            public const EventKeywords Tasks = (EventKeywords)2;
            /// <summary>
            /// Events associted with the higher level parallel APIs
            /// </summary>
            public const EventKeywords Parallel = (EventKeywords)4;
            /// <summary>
            /// These are relatively verbose events that effectively just redirect
            /// the windows AsyncCausalityTracer to ETW
            /// </summary>
            public const EventKeywords AsyncCausalityOperation = (EventKeywords)8;
            public const EventKeywords AsyncCausalityRelation = (EventKeywords)0x10;
            public const EventKeywords AsyncCausalitySynchronousWork = (EventKeywords)0x20;

            /// <summary>
            /// Emit the stops as well as the schedule/start events
            /// </summary>
            public const EventKeywords TaskStops = (EventKeywords)0x40;

            /// <summary>
            /// TasksFlowActivityIds indicate that activity ID flow from one task
            /// to any task created by it.
            /// </summary>
            public const EventKeywords TasksFlowActivityIds = (EventKeywords)0x80;

            /// <summary>
            /// Events related to the happenings of async methods.
            /// </summary>
            public const EventKeywords AsyncMethod = (EventKeywords)0x100;

            /// <summary>
            /// TasksSetActivityIds will cause the task operations to set Activity Ids
            /// This option is incompatible with TasksFlowActivityIds flow is ignored
            /// if that keyword is set.   This option is likely to be removed in the future
            /// </summary>
            public const EventKeywords TasksSetActivityIds = (EventKeywords)0x10000;

            /// <summary>
            /// Relatively Verbose logging meant for debugging the Task library itself. Will probably be removed in the future
            /// </summary>
            public const EventKeywords Debug = (EventKeywords)0x20000;
            /// <summary>
            /// Relatively Verbose logging meant for debugging the Task library itself.  Will probably be removed in the future
            /// </summary>
            public const EventKeywords DebugActivityId = (EventKeywords)0x40000;
        }

        //-----------------------------------------------------------------------------------
        //
        // TPL Event IDs (must be unique)
        //

        /// <summary>A task is scheduled to a task scheduler.</summary>
        private const int TASKSCHEDULED_ID = 7;
        /// <summary>A task is about to execute.</summary>
        private const int TASKSTARTED_ID = 8;
        /// <summary>A task has finished executing.</summary>
        private const int TASKCOMPLETED_ID = 9;
        /// <summary>A wait on a task is beginning.</summary>
        private const int TASKWAITBEGIN_ID = 10;
        /// <summary>A wait on a task is ending.</summary>
        private const int TASKWAITEND_ID = 11;
        /// <summary>A continuation of a task is scheduled.</summary>
        private const int AWAITTASKCONTINUATIONSCHEDULED_ID = 12;
        /// <summary>A continuation of a taskWaitEnd is complete </summary>
        private const int TASKWAITCONTINUATIONCOMPLETE_ID = 13;
        /// <summary>A continuation of a taskWaitEnd is complete </summary>
        private const int TASKWAITCONTINUATIONSTARTED_ID = 19;

        private const int TRACEOPERATIONSTART_ID = 14;
        private const int TRACEOPERATIONSTOP_ID = 15;
        private const int TRACEOPERATIONRELATION_ID = 16;
        private const int TRACESYNCHRONOUSWORKSTART_ID = 17;
        private const int TRACESYNCHRONOUSWORKSTOP_ID = 18;


        //-----------------------------------------------------------------------------------
        //
        // Task Events
        //

        // These are all verbose events, so we need to call IsEnabled(EventLevel.Verbose, ALL_KEYWORDS)
        // call. However since the IsEnabled(l,k) call is more expensive than IsEnabled(), we only want
        // to incur this cost when instrumentation is enabled. So the Task codepaths that call these
        // event functions still do the check for IsEnabled()

        #region TaskScheduled
        /// <summary>
        /// Fired when a task is queued to a TaskScheduler.
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="TaskID">The task ID.</param>
        /// <param name="CreatingTaskID">The task ID</param>
        /// <param name="TaskCreationOptions">The options used to create the task.</param>
        /// <param name="appDomain">The ID for the current AppDomain.</param>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [Event(TASKSCHEDULED_ID, Task = Tasks.TaskScheduled, Version = 1, Opcode = EventOpcode.Send,
         Level = EventLevel.Informational, Keywords = Keywords.TaskTransfer | Keywords.Tasks,
         Message = "Task {2} scheduled to TaskScheduler {0}.")]
        public void TaskScheduled(
            int OriginatingTaskSchedulerID, int OriginatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int TaskID, int CreatingTaskID, int TaskCreationOptions, int appDomain = DefaultAppDomainID)
        {
            // IsEnabled() call is an inlined quick check that makes this very fast when provider is off
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.TaskTransfer | Keywords.Tasks))
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[6];
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID));
                    eventPayload[0].Reserved = 0;
                    eventPayload[1].Size = sizeof(int);
                    eventPayload[1].DataPointer = ((IntPtr)(&OriginatingTaskID));
                    eventPayload[1].Reserved = 0;
                    eventPayload[2].Size = sizeof(int);
                    eventPayload[2].DataPointer = ((IntPtr)(&TaskID));
                    eventPayload[2].Reserved = 0;
                    eventPayload[3].Size = sizeof(int);
                    eventPayload[3].DataPointer = ((IntPtr)(&CreatingTaskID));
                    eventPayload[3].Reserved = 0;
                    eventPayload[4].Size = sizeof(int);
                    eventPayload[4].DataPointer = ((IntPtr)(&TaskCreationOptions));
                    eventPayload[4].Reserved = 0;
                    eventPayload[5].Size = sizeof(int);
                    eventPayload[5].DataPointer = ((IntPtr)(&appDomain));
                    eventPayload[5].Reserved = 0;
                    if (TasksSetActivityIds)
                    {
                        Guid childActivityId = CreateGuidForTaskID(TaskID);
                        WriteEventWithRelatedActivityIdCore(TASKSCHEDULED_ID, &childActivityId, 6, eventPayload);
                    }
                    else
                        WriteEventCore(TASKSCHEDULED_ID, 6, eventPayload);
                }
            }
        }
        #endregion

        #region TaskStarted
        /// <summary>
        /// Fired just before a task actually starts executing.
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="TaskID">The task ID.</param>
        [Event(TASKSTARTED_ID,
         Level = EventLevel.Informational, Keywords = Keywords.Tasks,
         Message = "Task {2} executing.")]
        public void TaskStarted(
            int OriginatingTaskSchedulerID, int OriginatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int TaskID)
        {
            if (IsEnabled(EventLevel.Informational, Keywords.Tasks))
                WriteEvent(TASKSTARTED_ID, OriginatingTaskSchedulerID, OriginatingTaskID, TaskID);
        }
        #endregion

        #region TaskCompleted
        /// <summary>
        /// Fired right after a task finished executing.
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="TaskID">The task ID.</param>
        /// <param name="IsExceptional">Whether the task completed due to an error.</param>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [Event(TASKCOMPLETED_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.TaskStops,
         Message = "Task {2} completed.")]
        public void TaskCompleted(
            int OriginatingTaskSchedulerID, int OriginatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int TaskID, bool IsExceptional)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.Tasks))
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[4];
                    int isExceptionalInt = IsExceptional ? 1 : 0;
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID));
                    eventPayload[0].Reserved = 0;
                    eventPayload[1].Size = sizeof(int);
                    eventPayload[1].DataPointer = ((IntPtr)(&OriginatingTaskID));
                    eventPayload[1].Reserved = 0;
                    eventPayload[2].Size = sizeof(int);
                    eventPayload[2].DataPointer = ((IntPtr)(&TaskID));
                    eventPayload[2].Reserved = 0;
                    eventPayload[3].Size = sizeof(int);
                    eventPayload[3].DataPointer = ((IntPtr)(&isExceptionalInt));
                    eventPayload[3].Reserved = 0;
                    WriteEventCore(TASKCOMPLETED_ID, 4, eventPayload);
                }
            }
        }
        #endregion

        #region TaskWaitBegin
        /// <summary>
        /// Fired when starting to wait for a taks's completion explicitly or implicitly.
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="TaskID">The task ID.</param>
        /// <param name="Behavior">Configured behavior for the wait.</param>
        /// <param name="ContinueWithTaskID">
        /// If known, if 'TaskID' has a 'continueWith' task, mention give its ID here.
        /// 0 means unknown.   This allows better visualization of the common sequential chaining case.
        /// </param>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [Event(TASKWAITBEGIN_ID, Version = 3, Task = Tasks.TaskWait, Opcode = EventOpcode.Send,
         Level = EventLevel.Informational, Keywords = Keywords.TaskTransfer | Keywords.Tasks,
         Message = "Beginning wait ({3}) on Task {2}.")]
        public void TaskWaitBegin(
            int OriginatingTaskSchedulerID, int OriginatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int TaskID, TaskWaitBehavior Behavior, int ContinueWithTaskID)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.TaskTransfer | Keywords.Tasks))
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[5];
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID));
                    eventPayload[0].Reserved = 0;
                    eventPayload[1].Size = sizeof(int);
                    eventPayload[1].DataPointer = ((IntPtr)(&OriginatingTaskID));
                    eventPayload[1].Reserved = 0;
                    eventPayload[2].Size = sizeof(int);
                    eventPayload[2].DataPointer = ((IntPtr)(&TaskID));
                    eventPayload[2].Reserved = 0;
                    eventPayload[3].Size = sizeof(int);
                    eventPayload[3].DataPointer = ((IntPtr)(&Behavior));
                    eventPayload[3].Reserved = 0;
                    eventPayload[4].Size = sizeof(int);
                    eventPayload[4].DataPointer = ((IntPtr)(&ContinueWithTaskID));
                    eventPayload[4].Reserved = 0;
                    if (TasksSetActivityIds)
                    {
                        Guid childActivityId = CreateGuidForTaskID(TaskID);
                        WriteEventWithRelatedActivityIdCore(TASKWAITBEGIN_ID, &childActivityId, 5, eventPayload);
                    }
                    else
                    {
                        WriteEventCore(TASKWAITBEGIN_ID, 5, eventPayload);
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Fired when the wait for a tasks completion returns.
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="TaskID">The task ID.</param>
        [Event(TASKWAITEND_ID,
         Level = EventLevel.Verbose, Keywords = Keywords.Tasks,
         Message = "Ending wait on Task {2}.")]
        public void TaskWaitEnd(
            int OriginatingTaskSchedulerID, int OriginatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int TaskID)
        {
            // Log an event if indicated.
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Tasks))
                WriteEvent(TASKWAITEND_ID, OriginatingTaskSchedulerID, OriginatingTaskID, TaskID);
        }

        /// <summary>
        /// Fired when the work (method) associated with a TaskWaitEnd completes
        /// </summary>
        /// <param name="TaskID">The task ID.</param>
        [Event(TASKWAITCONTINUATIONCOMPLETE_ID,
         Level = EventLevel.Verbose, Keywords = Keywords.TaskStops)]
        public void TaskWaitContinuationComplete(int TaskID)
        {
            // Log an event if indicated.
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.TaskStops))
                WriteEvent(TASKWAITCONTINUATIONCOMPLETE_ID, TaskID);
        }

        /// <summary>
        /// Fired when the work (method) associated with a TaskWaitEnd completes
        /// </summary>
        /// <param name="TaskID">The task ID.</param>
        [Event(TASKWAITCONTINUATIONSTARTED_ID,
         Level = EventLevel.Verbose, Keywords = Keywords.Tasks)]
        public void TaskWaitContinuationStarted(int TaskID)
        {
            // Log an event if indicated.
            if (IsEnabled() && IsEnabled(EventLevel.Verbose, Keywords.Tasks))
                WriteEvent(TASKWAITCONTINUATIONSTARTED_ID, TaskID);
        }

        /// <summary>
        /// Fired when an asynchronous continuation for a task is scheduled
        /// </summary>
        /// <param name="OriginatingTaskSchedulerID">The scheduler ID.</param>
        /// <param name="OriginatingTaskID">The task ID.</param>
        /// <param name="ContinueWithTaskId">The ID of the continuation object.</param>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [Event(AWAITTASKCONTINUATIONSCHEDULED_ID, Task = Tasks.AwaitTaskContinuationScheduled, Opcode = EventOpcode.Send,
         Level = EventLevel.Informational, Keywords = Keywords.TaskTransfer | Keywords.Tasks)]
        public void AwaitTaskContinuationScheduled(
            int OriginatingTaskSchedulerID, int OriginatingTaskID,  // PFX_COMMON_EVENT_HEADER
            int ContinueWithTaskId)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.TaskTransfer | Keywords.Tasks))
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[3];
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&OriginatingTaskSchedulerID));
                    eventPayload[0].Reserved = 0;
                    eventPayload[1].Size = sizeof(int);
                    eventPayload[1].DataPointer = ((IntPtr)(&OriginatingTaskID));
                    eventPayload[1].Reserved = 0;
                    eventPayload[2].Size = sizeof(int);
                    eventPayload[2].DataPointer = ((IntPtr)(&ContinueWithTaskId));
                    eventPayload[2].Reserved = 0;
                    if (TasksSetActivityIds)
                    {
                        Guid continuationActivityId = CreateGuidForTaskID(ContinueWithTaskId);
                        WriteEventWithRelatedActivityIdCore(AWAITTASKCONTINUATIONSCHEDULED_ID, &continuationActivityId, 3, eventPayload);
                    }
                    else
                        WriteEventCore(AWAITTASKCONTINUATIONSCHEDULED_ID, 3, eventPayload);
                }
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [Event(TRACEOPERATIONSTART_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.AsyncCausalityOperation)]
        public void TraceOperationBegin(int TaskID, string OperationName, long RelatedContext)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalityOperation))
            {
                unsafe
                {
                    fixed (char* operationNamePtr = OperationName)
                    {
                        EventData* eventPayload = stackalloc EventData[3];
                        eventPayload[0].Size = sizeof(int);
                        eventPayload[0].DataPointer = ((IntPtr)(&TaskID));
                        eventPayload[0].Reserved = 0;

                        eventPayload[1].Size = ((OperationName.Length + 1) * 2);
                        eventPayload[1].DataPointer = ((IntPtr)operationNamePtr);
                        eventPayload[1].Reserved = 0;

                        eventPayload[2].Size = sizeof(long);
                        eventPayload[2].DataPointer = ((IntPtr)(&RelatedContext));
                        eventPayload[2].Reserved = 0;
                        WriteEventCore(TRACEOPERATIONSTART_ID, 3, eventPayload);
                    }
                }
            }
        }

        [Event(TRACEOPERATIONRELATION_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.AsyncCausalityRelation)]
        public void TraceOperationRelation(int TaskID, CausalityRelation Relation)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalityRelation))
                WriteEvent(TRACEOPERATIONRELATION_ID, TaskID, (int)Relation);                // optimized overload for this exists
        }

        [Event(TRACEOPERATIONSTOP_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.AsyncCausalityOperation)]
        public void TraceOperationEnd(int TaskID, AsyncCausalityStatus Status)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalityOperation))
                WriteEvent(TRACEOPERATIONSTOP_ID, TaskID, (int)Status);                     // optimized overload for this exists
        }

        [Event(TRACESYNCHRONOUSWORKSTART_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.AsyncCausalitySynchronousWork)]
        public void TraceSynchronousWorkBegin(int TaskID, CausalitySynchronousWork Work)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalitySynchronousWork))
                WriteEvent(TRACESYNCHRONOUSWORKSTART_ID, TaskID, (int)Work);               // optimized overload for this exists
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [Event(TRACESYNCHRONOUSWORKSTOP_ID, Version = 1,
         Level = EventLevel.Informational, Keywords = Keywords.AsyncCausalitySynchronousWork)]
        public void TraceSynchronousWorkEnd(CausalitySynchronousWork Work)
        {
            if (IsEnabled() && IsEnabled(EventLevel.Informational, Keywords.AsyncCausalitySynchronousWork))
            {
                unsafe
                {
                    EventData* eventPayload = stackalloc EventData[1];
                    eventPayload[0].Size = sizeof(int);
                    eventPayload[0].DataPointer = ((IntPtr)(&Work));
                    eventPayload[0].Reserved = 0;

                    WriteEventCore(TRACESYNCHRONOUSWORKSTOP_ID, 1, eventPayload);
                }
            }
        }

        [NonEvent]
        public unsafe void RunningContinuation(int TaskID, object Object) { RunningContinuation(TaskID, (long)*((void**)Unsafe.AsPointer(ref Object))); }
        [Event(20, Keywords = Keywords.Debug)]
        private void RunningContinuation(int TaskID, long Object)
        {
            if (Debug)
                WriteEvent(20, TaskID, Object);
        }

        [NonEvent]
        public unsafe void RunningContinuationList(int TaskID, int Index, object Object) { RunningContinuationList(TaskID, Index, (long)*((void**)Unsafe.AsPointer(ref Object))); }

        [Event(21, Keywords = Keywords.Debug)]
        public void RunningContinuationList(int TaskID, int Index, long Object)
        {
            if (Debug)
                WriteEvent(21, TaskID, Index, Object);
        }

        [Event(23, Keywords = Keywords.Debug)]
        public void DebugFacilityMessage(string Facility, string Message) { WriteEvent(23, Facility, Message); }

        [Event(24, Keywords = Keywords.Debug)]
        public void DebugFacilityMessage1(string Facility, string Message, string Value1) { WriteEvent(24, Facility, Message, Value1); }

        [Event(25, Keywords = Keywords.DebugActivityId)]
        public void SetActivityId(Guid NewId)
        {
            if (DebugActivityId)
                WriteEvent(25, NewId);
        }

        [Event(26, Keywords = Keywords.Debug)]
        public void NewID(int TaskID)
        {
            if (Debug)
                WriteEvent(26, TaskID);
        }

        [NonEvent]
        public void IncompleteAsyncMethod(IAsyncStateMachineBox stateMachineBox)
        {
            Diagnostics.Debug.Assert(stateMachineBox != null);
            if (IsEnabled() && IsEnabled(EventLevel.Warning, Keywords.AsyncMethod))
            {
                IAsyncStateMachine stateMachine = stateMachineBox.GetStateMachineObject();
                if (stateMachine != null)
                {
                    string description = AsyncMethodBuilderCore.GetAsyncStateMachineDescription(stateMachine);
                    IncompleteAsyncMethod(description);
                }
            }
        }

        [Event(27, Level = EventLevel.Warning, Keywords = Keywords.AsyncMethod)]
        private void IncompleteAsyncMethod(string stateMachineDescription) =>
            WriteEvent(27, stateMachineDescription);

        /// <summary>
        /// Activity IDs are GUIDS but task IDS are integers (and are not unique across appdomains
        /// This routine creates a process wide unique GUID given a task ID
        /// </summary>
        internal static Guid CreateGuidForTaskID(int taskID)
        {
            // The thread pool generated a process wide unique GUID from a task GUID by
            // using the taskGuid, the appdomain ID, and 8 bytes of 'randomization' chosen by
            // using the last 8 bytes  as the provider GUID for this provider.
            // These were generated by CreateGuid, and are reasonably random (and thus unlikely to collide
            int pid = Environment.ProcessId;
            return new Guid(taskID,
                            (short)DefaultAppDomainID, (short)(DefaultAppDomainID >> 16),
                            (byte)pid, (byte)(pid >> 8), (byte)(pid >> 16), (byte)(pid >> 24),
                            0xff, 0xdc, 0xd7, 0xb5);
        }
    }
}
