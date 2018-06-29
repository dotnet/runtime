// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// TargetRegistry.cs
//
//
// A store of registered targets with a target block.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Threading.Tasks.Dataflow.Internal
{
    /// <summary>Stores targets registered with a source.</summary>
    /// <typeparam name="T">Specifies the type of data accepted by the targets.</typeparam>
    /// <remarks>This type is not thread-safe.</remarks>
    [DebuggerDisplay("Count={Count}")]
    [DebuggerTypeProxy(typeof(TargetRegistry<>.DebugView))]
    internal sealed class TargetRegistry<T>
    {
        /// <summary>
        /// Information about a registered target. This class represents a self-sufficient node in a linked list.
        /// </summary>
        internal sealed class LinkedTargetInfo
        {
            /// <summary>Initializes the LinkedTargetInfo.</summary>
            /// <param name="target">The target block reference for this entry.</param>
            /// <param name="linkOptions">The link options.</param>
            internal LinkedTargetInfo(ITargetBlock<T> target, DataflowLinkOptions linkOptions)
            {
                Debug.Assert(target != null, "The target that is supposed to be linked must not be null.");
                Debug.Assert(linkOptions != null, "The linkOptions must not be null.");

                Target = target;
                PropagateCompletion = linkOptions.PropagateCompletion;
                RemainingMessages = linkOptions.MaxMessages;
            }

            /// <summary>The target block reference for this entry.</summary>
            internal readonly ITargetBlock<T> Target;
            /// <summary>The value of the PropagateCompletion link option.</summary>
            internal readonly bool PropagateCompletion;
            /// <summary>Number of remaining messages to propagate. 
            /// This counter is initialized to the MaxMessages option and 
            /// gets decremented after each successful propagation.</summary>
            internal int RemainingMessages;
            /// <summary>The previous node in the list.</summary>
            internal LinkedTargetInfo Previous;
            /// <summary>The next node in the list.</summary>
            internal LinkedTargetInfo Next;
        }

        /// <summary>A reference to the owning source block.</summary>
        private readonly ISourceBlock<T> _owningSource;
        /// <summary>A mapping of targets to information about them.</summary>
        private readonly Dictionary<ITargetBlock<T>, LinkedTargetInfo> _targetInformation;
        /// <summary>The first node of an ordered list of targets. Messages should be offered to targets starting from First and following Next.</summary>
        private LinkedTargetInfo _firstTarget;
        /// <summary>The last node of the ordered list of targets. This field is used purely as a perf optimization to avoid traversing the list for each Add.</summary>
        private LinkedTargetInfo _lastTarget;
        /// <summary>Number of links with positive RemainingMessages counters.
        /// This is an optimization that allows us to skip dictionary lookup when this counter is 0.</summary>
        private int _linksWithRemainingMessages;

        /// <summary>Initializes the registry.</summary>
        internal TargetRegistry(ISourceBlock<T> owningSource)
        {
            Debug.Assert(owningSource != null, "The TargetRegistry instance must be owned by a source block.");

            _owningSource = owningSource;
            _targetInformation = new Dictionary<ITargetBlock<T>, LinkedTargetInfo>();
        }

        /// <summary>Adds a target to the registry.</summary>
        /// <param name="target">The target to add.</param>
        /// <param name="linkOptions">The link options.</param>
        internal void Add(ref ITargetBlock<T> target, DataflowLinkOptions linkOptions)
        {
            Debug.Assert(target != null, "The target that is supposed to be linked must not be null.");
            Debug.Assert(linkOptions != null, "The link options must not be null.");

            LinkedTargetInfo targetInfo;

            // If the target already exists in the registry, replace it with a new NopLinkPropagator to maintain uniqueness
            if (_targetInformation.TryGetValue(target, out targetInfo)) target = new NopLinkPropagator(_owningSource, target);

            // Add the target to both stores, the list and the dictionary, which are used for different purposes
            var node = new LinkedTargetInfo(target, linkOptions);
            AddToList(node, linkOptions.Append);
            _targetInformation.Add(target, node);

            // Increment the optimization counter if needed
            Debug.Assert(_linksWithRemainingMessages >= 0, "_linksWithRemainingMessages must be non-negative at any time.");
            if (node.RemainingMessages > 0) _linksWithRemainingMessages++;
#if FEATURE_TRACING
            DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
            if (etwLog.IsEnabled())
            {
                etwLog.DataflowBlockLinking(_owningSource, target);
            }
#endif
        }

        /// <summary>Gets whether the registry contains a particular target.</summary>
        /// <param name="target">The target.</param>
        /// <returns>true if the registry contains the target; otherwise, false.</returns>
        internal bool Contains(ITargetBlock<T> target)
        {
            return _targetInformation.ContainsKey(target);
        }

        /// <summary>Removes the target from the registry.</summary>
        /// <param name="target">The target to remove.</param>
        /// <param name="onlyIfReachedMaxMessages">
        /// Only remove the target if it's configured to be unlinked after one propagation.
        /// </param>
        internal void Remove(ITargetBlock<T> target, bool onlyIfReachedMaxMessages = false)
        {
            Debug.Assert(target != null, "Target to remove is required.");

            // If we are implicitly unlinking and there is nothing to be unlinked implicitly, bail
            Debug.Assert(_linksWithRemainingMessages >= 0, "_linksWithRemainingMessages must be non-negative at any time.");
            if (onlyIfReachedMaxMessages && _linksWithRemainingMessages == 0) return;

            // Otherwise take the slow path
            Remove_Slow(target, onlyIfReachedMaxMessages);
        }

        /// <summary>Actually removes the target from the registry.</summary>
        /// <param name="target">The target to remove.</param>
        /// <param name="onlyIfReachedMaxMessages">
        /// Only remove the target if it's configured to be unlinked after one propagation.
        /// </param>
        private void Remove_Slow(ITargetBlock<T> target, bool onlyIfReachedMaxMessages)
        {
            Debug.Assert(target != null, "Target to remove is required.");

            // Make sure we've intended to go the slow route
            Debug.Assert(_linksWithRemainingMessages >= 0, "_linksWithRemainingMessages must be non-negative at any time.");
            Debug.Assert(!onlyIfReachedMaxMessages || _linksWithRemainingMessages > 0, "We shouldn't have ended on the slow path.");

            // If the target is registered...
            LinkedTargetInfo node;
            if (_targetInformation.TryGetValue(target, out node))
            {
                Debug.Assert(node != null, "The LinkedTargetInfo node referenced in the Dictionary must be non-null.");

                // Remove the target, if either there's no constraint on the removal
                // or if this was the last remaining message.
                if (!onlyIfReachedMaxMessages || node.RemainingMessages == 1)
                {
                    RemoveFromList(node);
                    _targetInformation.Remove(target);

                    // Decrement the optimization counter if needed
                    if (node.RemainingMessages == 0) _linksWithRemainingMessages--;
                    Debug.Assert(_linksWithRemainingMessages >= 0, "_linksWithRemainingMessages must be non-negative at any time.");
#if FEATURE_TRACING
                    DataflowEtwProvider etwLog = DataflowEtwProvider.Log;
                    if (etwLog.IsEnabled())
                    {
                        etwLog.DataflowBlockUnlinking(_owningSource, target);
                    }
#endif
                }
                // If the target is to stay and we are counting the remaining messages for this link, decrement the counter
                else if (node.RemainingMessages > 0)
                {
                    Debug.Assert(node.RemainingMessages > 1, "The target should have been removed, because there are no remaining messages.");
                    node.RemainingMessages--;
                }
            }
        }

        /// <summary>Clears the target registry entry points while allowing subsequent traversals of the linked list.</summary>
        internal LinkedTargetInfo ClearEntryPoints()
        {
            // Save _firstTarget so we can return it
            LinkedTargetInfo firstTarget = _firstTarget;

            // Clear out the entry points
            _firstTarget = _lastTarget = null;
            _targetInformation.Clear();
            Debug.Assert(_linksWithRemainingMessages >= 0, "_linksWithRemainingMessages must be non-negative at any time.");
            _linksWithRemainingMessages = 0;

            return firstTarget;
        }

        /// <summary>Propagated completion to the targets of the given linked list.</summary>
        /// <param name="firstTarget">The head of a saved linked list.</param>
        internal void PropagateCompletion(LinkedTargetInfo firstTarget)
        {
            Debug.Assert(_owningSource.Completion.IsCompleted, "The owning source must have completed before propagating completion.");

            // Cache the owning source's completion task to avoid calling the getter many times
            Task owningSourceCompletion = _owningSource.Completion;

            // Propagate completion to those targets that have requested it
            for (LinkedTargetInfo node = firstTarget; node != null; node = node.Next)
            {
                if (node.PropagateCompletion) Common.PropagateCompletion(owningSourceCompletion, node.Target, Common.AsyncExceptionHandler);
            }
        }

        /// <summary>Gets the first node of the ordered target list.</summary>
        internal LinkedTargetInfo FirstTargetNode { get { return _firstTarget; } }

        /// <summary>Adds a LinkedTargetInfo node to the doubly-linked list.</summary>
        /// <param name="node">The node to be added.</param>
        /// <param name="append">Whether to append or to prepend the node.</param>
        internal void AddToList(LinkedTargetInfo node, bool append)
        {
            Debug.Assert(node != null, "Requires a node to be added.");

            // If the list is empty, assign the ends to point to the new node and we are done
            if (_firstTarget == null && _lastTarget == null)
            {
                _firstTarget = _lastTarget = node;
            }
            else
            {
                Debug.Assert(_firstTarget != null && _lastTarget != null, "Both first and last node must either be null or non-null.");
                Debug.Assert(_lastTarget.Next == null, "The last node must not have a successor.");
                Debug.Assert(_firstTarget.Previous == null, "The first node must not have a predecessor.");

                if (append)
                {
                    // Link the new node to the end of the existing list
                    node.Previous = _lastTarget;
                    _lastTarget.Next = node;
                    _lastTarget = node;
                }
                else
                {
                    // Link the new node to the front of the existing list
                    node.Next = _firstTarget;
                    _firstTarget.Previous = node;
                    _firstTarget = node;
                }
            }

            Debug.Assert(_firstTarget != null && _lastTarget != null, "Both first and last node must be non-null after AddToList.");
        }

        /// <summary>Removes the LinkedTargetInfo node from the doubly-linked list.</summary>
        /// <param name="node">The node to be removed.</param>
        internal void RemoveFromList(LinkedTargetInfo node)
        {
            Debug.Assert(node != null, "Node to remove is required.");
            Debug.Assert(_firstTarget != null && _lastTarget != null, "Both first and last node must be non-null before RemoveFromList.");

            LinkedTargetInfo previous = node.Previous;
            LinkedTargetInfo next = node.Next;

            // Remove the node by linking the adjacent nodes
            if (node.Previous != null)
            {
                node.Previous.Next = next;
                node.Previous = null;
            }

            if (node.Next != null)
            {
                node.Next.Previous = previous;
                node.Next = null;
            }

            // Adjust the list ends
            if (_firstTarget == node) _firstTarget = next;
            if (_lastTarget == node) _lastTarget = previous;

            Debug.Assert((_firstTarget != null) == (_lastTarget != null), "Both first and last node must either be null or non-null after RemoveFromList.");
        }

        /// <summary>Gets the number of items in the registry.</summary>
        private int Count { get { return _targetInformation.Count; } }

        /// <summary>Converts the linked list of targets to an array for rendering in a debugger.</summary>
        private ITargetBlock<T>[] TargetsForDebugger
        {
            get
            {
                var targets = new ITargetBlock<T>[Count];
                int i = 0;
                for (LinkedTargetInfo node = _firstTarget; node != null; node = node.Next)
                {
                    targets[i++] = node.Target;
                }

                return targets;
            }
        }



        /// <summary>Provides a nop passthrough for use with TargetRegistry.</summary>
        [DebuggerDisplay("{DebuggerDisplayContent,nq}")]
        [DebuggerTypeProxy(typeof(TargetRegistry<>.NopLinkPropagator.DebugView))]
        private sealed class NopLinkPropagator : IPropagatorBlock<T, T>, ISourceBlock<T>, IDebuggerDisplay
        {
            /// <summary>The source that encapsulates this block.</summary>
            private readonly ISourceBlock<T> _owningSource;
            /// <summary>The target with which this block is associated.</summary>
            private readonly ITargetBlock<T> _target;

            /// <summary>Initializes the passthrough.</summary>
            /// <param name="owningSource">The source that encapsulates this block.</param>
            /// <param name="target">The target to which messages should be forwarded.</param>
            internal NopLinkPropagator(ISourceBlock<T> owningSource, ITargetBlock<T> target)
            {
                Debug.Assert(owningSource != null, "Propagator must be associated with a source.");
                Debug.Assert(target != null, "Target to propagate to is required.");

                // Store the arguments
                _owningSource = owningSource;
                _target = target;
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Targets/Member[@name="OfferMessage"]/*' />
            DataflowMessageStatus ITargetBlock<T>.OfferMessage(DataflowMessageHeader messageHeader, T messageValue, ISourceBlock<T> source, bool consumeToAccept)
            {
                Debug.Assert(source == _owningSource, "Only valid to be used with the source for which it was created.");
                return _target.OfferMessage(messageHeader, messageValue, this, consumeToAccept);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ConsumeMessage"]/*' />
            T ISourceBlock<T>.ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target, out bool messageConsumed)
            {
                return _owningSource.ConsumeMessage(messageHeader, this, out messageConsumed);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReserveMessage"]/*' />
            bool ISourceBlock<T>.ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
            {
                return _owningSource.ReserveMessage(messageHeader, this);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="ReleaseReservation"]/*' />
            void ISourceBlock<T>.ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<T> target)
            {
                _owningSource.ReleaseReservation(messageHeader, this);
            }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Completion"]/*' />
            Task IDataflowBlock.Completion { get { return _owningSource.Completion; } }
            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Complete"]/*' />
            void IDataflowBlock.Complete() { _target.Complete(); }
            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Blocks/Member[@name="Fault"]/*' />
            void IDataflowBlock.Fault(Exception exception) { _target.Fault(exception); }

            /// <include file='XmlDocs/CommonXmlDocComments.xml' path='CommonXmlDocComments/Sources/Member[@name="LinkTo"]/*' />
            IDisposable ISourceBlock<T>.LinkTo(ITargetBlock<T> target, DataflowLinkOptions linkOptions) { throw new NotSupportedException(SR.NotSupported_MemberNotNeeded); }

            /// <summary>The data to display in the debugger display attribute.</summary>
            [SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider")]
            private object DebuggerDisplayContent
            {
                get
                {
                    var displaySource = _owningSource as IDebuggerDisplay;
                    var displayTarget = _target as IDebuggerDisplay;
                    return string.Format("{0} Source=\"{1}\", Target=\"{2}\"",
                        Common.GetNameForDebugger(this),
                        displaySource != null ? displaySource.Content : _owningSource,
                        displayTarget != null ? displayTarget.Content : _target);
                }
            }
            /// <summary>Gets the data to display in the debugger display attribute for this instance.</summary>
            object IDebuggerDisplay.Content { get { return DebuggerDisplayContent; } }

            /// <summary>Provides a debugger type proxy for a passthrough.</summary>
            private sealed class DebugView
            {
                /// <summary>The passthrough.</summary>
                private readonly NopLinkPropagator _passthrough;

                /// <summary>Initializes the debug view.</summary>
                /// <param name="passthrough">The passthrough to view.</param>
                public DebugView(NopLinkPropagator passthrough)
                {
                    Debug.Assert(passthrough != null, "Need a propagator with which to construct the debug view.");
                    _passthrough = passthrough;
                }

                /// <summary>The linked target for this block.</summary>
                public ITargetBlock<T> LinkedTarget { get { return _passthrough._target; } }
            }
        }


        /// <summary>Provides a debugger type proxy for the target registry.</summary>
        private sealed class DebugView
        {
            /// <summary>The registry being debugged.</summary>
            private readonly TargetRegistry<T> _registry;

            /// <summary>Initializes the type proxy.</summary>
            /// <param name="registry">The target registry.</param>
            public DebugView(TargetRegistry<T> registry)
            {
                Debug.Assert(registry != null, "Need a registry with which to construct the debug view.");
                _registry = registry;
            }

            /// <summary>Gets a list of all targets to show in the debugger.</summary>
            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public ITargetBlock<T>[] Targets { get { return _registry.TargetsForDebugger; } }
        }
    }
}
