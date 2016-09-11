// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ReorderingBuffer.cs
//
//
// An intermediate buffer that ensures messages are output in the right order.
// Used by blocks (e.g. TransformBlock, TransformManyBlock) when operating in 
// parallel modes that could result in messages being processed out of order.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Threading.Tasks.Dataflow.Internal
{
    /// <summary>Base interface for reordering buffers.</summary>
    internal interface IReorderingBuffer
    {
        /// <summary>Informs the reordering buffer not to expect the message with the specified id.</summary>
        /// <param name="id">The id of the message to be ignored.</param>
        void IgnoreItem(long id);
    }

    /// <summary>Provides a buffer that reorders items according to their incoming IDs.</summary>
    /// <typeparam name="TOutput">Specifies the type of data stored in the items being reordered.</typeparam>
    /// <remarks>
    /// This type expects the first item to be ID==0 and for all subsequent items
    /// to increase IDs sequentially.
    /// </remarks>
    [DebuggerDisplay("Count={CountForDebugging}")]
    [DebuggerTypeProxy(typeof(ReorderingBuffer<>.DebugView))]
    internal sealed class ReorderingBuffer<TOutput> : IReorderingBuffer
    {
        /// <summary>The source that owns this reordering buffer.</summary>
        private readonly object _owningSource;
        /// <summary>A reordering buffer used when parallelism is employed and items may be completed out-of-order.</summary>
        /// <remarks>Also serves as the sync object to protect the contents of this class.</remarks>
        private readonly Dictionary<long, KeyValuePair<bool, TOutput>> _reorderingBuffer = new Dictionary<long, KeyValuePair<bool, TOutput>>();
        /// <summary>Action used to output items in order.</summary>
        private readonly Action<object, TOutput> _outputAction;
        /// <summary>The ID of the next item that should be released from the reordering buffer.</summary>
        private long _nextReorderedIdToOutput = 0;

        /// <summary>Gets the object used to synchronize all access to the reordering buffer's internals.</summary>
        private object ValueLock { get { return _reorderingBuffer; } }

        /// <summary>Initializes the reordering buffer.</summary>
        /// <param name="owningSource">The source that owns this reordering buffer.</param>
        /// <param name="outputAction">The action to invoke when the next in-order item is available to be output.</param>
        internal ReorderingBuffer(object owningSource, Action<object, TOutput> outputAction)
        {
            // Validate and store internal arguments
            Debug.Assert(owningSource != null, "Buffer must be associated with a source.");
            Debug.Assert(outputAction != null, "Action required for when items are to be released.");
            _owningSource = owningSource;
            _outputAction = outputAction;
        }

        /// <summary>Stores the next item as it completes processing.</summary>
        /// <param name="id">The ID of the item.</param>
        /// <param name="item">The completed item.</param>
        /// <param name="itemIsValid">Specifies whether the item is valid (true) or just a placeholder (false).</param>
        internal void AddItem(long id, TOutput item, bool itemIsValid)
        {
            Debug.Assert(id != Common.INVALID_REORDERING_ID, "This ID should never have been handed out.");
            Common.ContractAssertMonitorStatus(ValueLock, held: false);

            // This may be called concurrently, so protect the buffer...
            lock (ValueLock)
            {
                // If this is the next item we need in our ordering, output it.
                if (_nextReorderedIdToOutput == id)
                {
                    OutputNextItem(item, itemIsValid);
                }
                // Otherwise, we're using reordering and we're not ready for this item yet, so store
                // it until we can use it.
                else
                {
                    Debug.Assert((ulong)id > (ulong)_nextReorderedIdToOutput, "Duplicate id.");
                    _reorderingBuffer.Add(id, new KeyValuePair<bool, TOutput>(itemIsValid, item));
                }
            }
        }

        /// <summary>
        /// Determines whether the specified id is next to be output, and if it is
        /// and if the item is "trusted" (meaning it may be output into the output
        /// action as-is), adds it.
        /// </summary>
        /// <param name="id">The id of the item.</param>
        /// <param name="item">The item.</param>
        /// <param name="isTrusted">
        /// Whether to allow the item to be output directly if it is the next item.
        /// </param>
        /// <returns>
        /// null if the item was added.
        /// true if the item was not added but is next in line.
        /// false if the item was not added and is not next in line.
        /// </returns>
        internal bool? AddItemIfNextAndTrusted(long id, TOutput item, bool isTrusted)
        {
            Debug.Assert(id != Common.INVALID_REORDERING_ID, "This ID should never have been handed out.");
            Common.ContractAssertMonitorStatus(ValueLock, held: false);

            lock (ValueLock)
            {
                // If this is in the next item, try to take the fast path.
                if (_nextReorderedIdToOutput == id)
                {
                    // If we trust this data structure to be stored as-is,
                    // output it immediately.  Otherwise, return that it is
                    // next to be output.
                    if (isTrusted)
                    {
                        OutputNextItem(item, itemIsValid: true);
                        return null;
                    }
                    else return true;
                }
                else return false;
            }
        }

        /// <summary>Informs the reordering buffer not to expect the message with the specified id.</summary>
        /// <param name="id">The id of the message to be ignored.</param>
        public void IgnoreItem(long id)
        {
            AddItem(id, default(TOutput), itemIsValid: false);
        }

        /// <summary>Outputs the item.  The item must have already been confirmed to have the next ID.</summary>
        /// <param name="theNextItem">The item to output.</param>
        /// <param name="itemIsValid">Whether the item is valid.</param>
        private void OutputNextItem(TOutput theNextItem, bool itemIsValid)
        {
            Common.ContractAssertMonitorStatus(ValueLock, held: true);

            // Note that we're now looking for a different item, and pass this one through.
            // Then release any items which may be pending.
            _nextReorderedIdToOutput++;
            if (itemIsValid) _outputAction(_owningSource, theNextItem);

            // Try to get the next available item from the buffer and output it.  Continue to do so
            // until we run out of items in the reordering buffer or don't yet have the next ID buffered.
            KeyValuePair<bool, TOutput> nextOutputItemWithValidity;
            while (_reorderingBuffer.TryGetValue(_nextReorderedIdToOutput, out nextOutputItemWithValidity))
            {
                _reorderingBuffer.Remove(_nextReorderedIdToOutput);
                _nextReorderedIdToOutput++;
                if (nextOutputItemWithValidity.Key) _outputAction(_owningSource, nextOutputItemWithValidity.Value);
            }
        }

        /// <summary>Gets a item count for debugging purposes.</summary>
        private int CountForDebugging { get { return _reorderingBuffer.Count; } }

        /// <summary>Provides a debugger type proxy for the buffer.</summary>
        private sealed class DebugView
        {
            /// <summary>The buffer being debugged.</summary>
            private readonly ReorderingBuffer<TOutput> _buffer;

            /// <summary>Initializes the debug view.</summary>
            /// <param name="buffer">The buffer being debugged.</param>
            public DebugView(ReorderingBuffer<TOutput> buffer)
            {
                Debug.Assert(buffer != null, "Need a buffer with which to construct the debug view.");
                _buffer = buffer;
            }

            /// <summary>Gets a dictionary of buffered items and their reordering IDs.</summary>
            public Dictionary<long, KeyValuePair<Boolean, TOutput>> ItemsBuffered { get { return _buffer._reorderingBuffer; } }
            /// <summary>Gets the next ID required for outputting.</summary>
            public long NextIdRequired { get { return _buffer._nextReorderedIdToOutput; } }
        }
    }
}
