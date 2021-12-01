// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// SynchronousChannelMergeEnumerator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel
{
    /// <summary>
    /// This enumerator merges multiple input channels into a single output stream. The merging process just
    /// goes from left-to-right, enumerating each channel in succession in its entirety.
    /// Assumptions:
    ///     Before enumerating this object, all producers for all channels must have finished enqueuing new
    ///     elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class SynchronousChannelMergeEnumerator<T> : MergeEnumerator<T>
    {
        private readonly SynchronousChannel<T>[] _channels; // The channel array we will enumerate, from left-to-right.
        private int _channelIndex; // The current channel index. This moves through the array as we enumerate.
        private T? _currentElement; // The last element remembered during enumeration.

        //-----------------------------------------------------------------------------------
        // Instantiates a new enumerator for a set of channels.
        //

        internal SynchronousChannelMergeEnumerator(
            QueryTaskGroupState taskGroupState, SynchronousChannel<T>[] channels) : base(taskGroupState)
        {
            Debug.Assert(channels != null);
#if DEBUG
            foreach (SynchronousChannel<T> c in channels) Debug.Assert(c != null);
#endif

            _channels = channels;
            _channelIndex = -1;
        }

        //-----------------------------------------------------------------------------------
        // Retrieves the current element.
        //
        // Notes:
        //     This throws if we haven't begun enumerating or have gone past the end of the
        //     data source.
        //

        public override T Current
        {
            get
            {
                // If we're at the beginning or the end of the array, it's invalid to be
                // retrieving the current element. We throw.
                if (_channelIndex == -1 || _channelIndex == _channels.Length)
                {
                    throw new InvalidOperationException(SR.PLINQ_CommonEnumerator_Current_NotStarted);
                }

                return _currentElement!;
            }
        }

        //-----------------------------------------------------------------------------------
        // Positions the enumerator over the next element. This includes merging as we
        // enumerate, by just incrementing indexes, etc.
        //
        // Return Value:
        //     True if there's a current element, false if we've reached the end.
        //

        public override bool MoveNext()
        {
            Debug.Assert(_channels != null);

            // If we're at the start, initialize the index.
            if (_channelIndex == -1)
            {
                _channelIndex = 0;
            }

            // If the index has reached the end, we bail.
            while (_channelIndex != _channels.Length)
            {
                SynchronousChannel<T> current = _channels[_channelIndex];
                Debug.Assert(current != null);

                if (current.Count == 0)
                {
                    // We're done with this channel, move on to the next one. We don't
                    // have to check that it's "done" since this is a synchronous consumer.
                    _channelIndex++;
                }
                else
                {
                    // Remember the "current" element and return.
                    _currentElement = current.Dequeue();
                    return true;
                }
            }

            TraceHelpers.TraceInfo("[timing]: {0}: Completed the merge", DateTime.Now.Ticks);

            // If we got this far, it means we've exhausted our channels.
            Debug.Assert(_channelIndex == _channels.Length);

            return false;
        }
    }
}
