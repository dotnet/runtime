// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// DataflowEtwProvider.cs
//
//
// EventSource for Dataflow.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using System.Diagnostics.Tracing;

namespace System.Threading.Tasks.Dataflow.Internal
{
    /// <summary>Provides an event source for tracing Dataflow information.</summary>
    [EventSource(
        Name = "System.Threading.Tasks.Dataflow.DataflowEventSource",
        Guid = "16F53577-E41D-43D4-B47E-C17025BF4025")]
    internal sealed class DataflowEtwProvider : EventSource
    {
        /// <summary>
        /// Defines the singleton instance for the dataflow ETW provider.
        /// The dataflow provider GUID is {16F53577-E41D-43D4-B47E-C17025BF4025}.
        /// </summary>
        internal static readonly DataflowEtwProvider Log = new DataflowEtwProvider();
        /// <summary>Prevent external instantiation.  All logging should go through the Log instance.</summary>
        private DataflowEtwProvider() { }

        /// <summary>Enabled for all keywords.</summary>
        private const EventKeywords ALL_KEYWORDS = (EventKeywords)(-1);

        //-----------------------------------------------------------------------------------
        //
        // Dataflow Event IDs (must be unique)
        //

        /// <summary>The event ID for when we encounter a new dataflow block object that hasn't had its name traced to the trace file.</summary>
        private const int DATAFLOWBLOCKCREATED_EVENTID = 1;
        /// <summary>The event ID for the task launched event.</summary>
        private const int TASKLAUNCHED_EVENTID = 2;
        /// <summary>The event ID for the block completed event.</summary>
        private const int BLOCKCOMPLETED_EVENTID = 3;
        /// <summary>The event ID for the block linked event.</summary>
        private const int BLOCKLINKED_EVENTID = 4;
        /// <summary>The event ID for the block unlinked event.</summary>
        private const int BLOCKUNLINKED_EVENTID = 5;

        //-----------------------------------------------------------------------------------
        //
        // Dataflow Events
        //

#region Block Creation
        /// <summary>Trace an event for when a new block is instantiated.</summary>
        /// <param name="block">The dataflow block that was created.</param>
        /// <param name="dataflowBlockOptions">The options with which the block was created.</param>
        [NonEvent]
        internal void DataflowBlockCreated(IDataflowBlock block, DataflowBlockOptions dataflowBlockOptions)
        {
            Debug.Assert(block != null, "Block needed for the ETW event.");
            Debug.Assert(dataflowBlockOptions != null, "Options needed for the ETW event.");

            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                DataflowBlockCreated(
                    Common.GetNameForDebugger(block, dataflowBlockOptions),
                    Common.GetBlockId(block));
            }
        }

        [Event(DATAFLOWBLOCKCREATED_EVENTID, Level = EventLevel.Informational)]
        private void DataflowBlockCreated(string blockName, int blockId)
        {
            WriteEvent(DATAFLOWBLOCKCREATED_EVENTID, blockName, blockId);
        }
#endregion

#region Task Launching
        /// <summary>Trace an event for a block launching a task to handle messages.</summary>
        /// <param name="block">The owner block launching a task.</param>
        /// <param name="task">The task being launched for processing.</param>
        /// <param name="reason">The reason the task is being launched.</param>
        /// <param name="availableMessages">The number of messages available to be handled by the task.</param>
        [NonEvent]
        internal void TaskLaunchedForMessageHandling(
            IDataflowBlock block, Task task, TaskLaunchedReason reason, int availableMessages)
        {
            Debug.Assert(block != null, "Block needed for the ETW event.");
            Debug.Assert(task != null, "Task needed for the ETW event.");
            Debug.Assert(reason == TaskLaunchedReason.ProcessingInputMessages || reason == TaskLaunchedReason.OfferingOutputMessages,
                "The reason should be a supported value from the TaskLaunchedReason enumeration.");
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                TaskLaunchedForMessageHandling(Common.GetBlockId(block), reason, availableMessages, task.Id);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "WriteEvent Parameters are trimmer safe")]
        [Event(TASKLAUNCHED_EVENTID, Level = EventLevel.Informational)]
        private void TaskLaunchedForMessageHandling(int blockId, TaskLaunchedReason reason, int availableMessages, int taskId)
        {
            WriteEvent(TASKLAUNCHED_EVENTID, blockId, reason, availableMessages, taskId);
        }

        /// <summary>Describes the reason a task is being launched.</summary>
        internal enum TaskLaunchedReason
        {
            /// <summary>A task is being launched to process incoming messages.</summary>
            ProcessingInputMessages = 1,
            /// <summary>A task is being launched to offer outgoing messages to linked targets.</summary>
            OfferingOutputMessages = 2,
        }
#endregion

#region Block Completion
        /// <summary>Trace an event for a block completing.</summary>
        /// <param name="block">The block that's completing.</param>
        [NonEvent]
        internal void DataflowBlockCompleted(IDataflowBlock block)
        {
            Debug.Assert(block != null, "Block needed for the ETW event.");
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                Task? completionTask = Common.GetPotentiallyNotSupportedCompletionTask(block);
                bool blockIsCompleted = completionTask != null && completionTask.IsCompleted;
                Debug.Assert(blockIsCompleted, "Block must be completed for this event to be valid.");
                if (blockIsCompleted)
                {
                    var reason = (BlockCompletionReason)completionTask!.Status;
                    string exceptionData = string.Empty;

                    if (completionTask.IsFaulted)
                    {
                        try { exceptionData = string.Join(Environment.NewLine, completionTask.Exception!.InnerExceptions.Select(static e => e.ToString())); }
                        catch { }
                    }

                    DataflowBlockCompleted(Common.GetBlockId(block), reason, exceptionData);
                }
            }
        }

        /// <summary>Describes the reason a block completed.</summary>
        internal enum BlockCompletionReason
        {
            /// <summary>The block completed successfully.</summary>
            RanToCompletion = (int)TaskStatus.RanToCompletion,
            /// <summary>The block completed due to an error.</summary>
            Faulted = (int)TaskStatus.Faulted,
            /// <summary>The block completed due to cancellation.</summary>
            Canceled = (int)TaskStatus.Canceled
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "WriteEvent Parameters are trimmer safe")]
        [Event(BLOCKCOMPLETED_EVENTID, Level = EventLevel.Informational)]
        private void DataflowBlockCompleted(int blockId, BlockCompletionReason reason, string exceptionData)
        {
            WriteEvent(BLOCKCOMPLETED_EVENTID, blockId, reason, exceptionData);
        }
#endregion

#region Linking
        /// <summary>Trace an event for a block linking.</summary>
        /// <param name="source">The source block linking to a target.</param>
        /// <param name="target">The target block being linked from a source.</param>
        [NonEvent]
        internal void DataflowBlockLinking<T>(ISourceBlock<T> source, ITargetBlock<T> target)
        {
            Debug.Assert(source != null, "Source needed for the ETW event.");
            Debug.Assert(target != null, "Target needed for the ETW event.");
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                DataflowBlockLinking(Common.GetBlockId(source), Common.GetBlockId(target));
            }
        }

        [Event(BLOCKLINKED_EVENTID, Level = EventLevel.Informational)]
        private void DataflowBlockLinking(int sourceId, int targetId)
        {
            WriteEvent(BLOCKLINKED_EVENTID, sourceId, targetId);
        }
#endregion

#region Unlinking
        /// <summary>Trace an event for a block unlinking.</summary>
        /// <param name="source">The source block unlinking from a target.</param>
        /// <param name="target">The target block being unlinked from a source.</param>
        [NonEvent]
        internal void DataflowBlockUnlinking<T>(ISourceBlock<T> source, ITargetBlock<T> target)
        {
            Debug.Assert(source != null, "Source needed for the ETW event.");
            Debug.Assert(target != null, "Target needed for the ETW event.");
            if (IsEnabled(EventLevel.Informational, ALL_KEYWORDS))
            {
                // Try catch exists to prevent against faulty blocks or blocks that only partially implement the interface
                DataflowBlockUnlinking(Common.GetBlockId(source), Common.GetBlockId(target));
            }
        }

        [Event(BLOCKUNLINKED_EVENTID, Level = EventLevel.Informational)]
        private void DataflowBlockUnlinking(int sourceId, int targetId)
        {
            WriteEvent(BLOCKUNLINKED_EVENTID, sourceId, targetId);
        }
#endregion
    }
}
