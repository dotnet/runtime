// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// AsynchronousChannelMergeEnumerator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Threading;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// An enumerator that merges multiple one-to-one channels into a single output
    /// stream, including any necessary blocking and synchronization. This is an
    /// asynchronous enumerator, i.e. the producers may be inserting items into the
    /// channels concurrently with the consumer taking items out of them. Therefore,
    /// enumerating this object can cause the current thread to block.
    ///
    /// We use a biased choice algorithm to choose from our consumer channels. I.e. we
    /// will prefer to process elements in a fair round-robin fashion, but will
    /// occasionally bypass this if a channel is empty.
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class AsynchronousChannelMergeEnumerator<T> : MergeEnumerator<T>
    {
        private AsynchronousChannel<T>[] _channels; // The channels being enumerated.
        private IntValueEvent _consumerEvent; // The consumer event.
        private bool[] _done;       // Tracks which channels are done.
        private int _channelIndex;  // The next channel from which we'll dequeue.
        private T _currentElement;  // The remembered element from the previous MoveNext.

        //-----------------------------------------------------------------------------------
        // Allocates a new enumerator over a set of one-to-one channels.
        //

        internal AsynchronousChannelMergeEnumerator(
            QueryTaskGroupState taskGroupState, AsynchronousChannel<T>[] channels, IntValueEvent consumerEvent)
            : base(taskGroupState)
        {
            Debug.Assert(channels != null);
#if DEBUG
            foreach (AsynchronousChannel<T> c in channels) Debug.Assert(c != null);
#endif

            _channels = channels;
            _channelIndex = -1; // To catch calls to Current before MoveNext.
            _done = new bool[_channels.Length]; // Initialized to { false }, i.e. no channels done.
            _consumerEvent = consumerEvent;
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
                if (_channelIndex == -1 || _channelIndex == _channels.Length)
                {
                    throw new InvalidOperationException(SR.PLINQ_CommonEnumerator_Current_NotStarted);
                }

                return _currentElement;
            }
        }

        //-----------------------------------------------------------------------------------
        // Positions the enumerator over the next element. This includes merging as we
        // enumerate, which may also involve waiting for producers to generate new items.
        //
        // Return Value:
        //     True if there's a current element, false if we've reached the end.
        //

        public override bool MoveNext()
        {
            // On the first call to MoveNext, we advance the position to a real channel.
            int index = _channelIndex;
            if (index == -1)
            {
                _channelIndex = index = 0;
            }

            // If we're past the end, enumeration is done.
            if (index == _channels.Length)
            {
                return false;
            }

            // Else try the fast path.
            if (!_done[index] && _channels[index].TryDequeue(ref _currentElement))
            {
                _channelIndex = (index + 1) % _channels.Length;
                return true;
            }

            return MoveNextSlowPath();
        }

        //-----------------------------------------------------------------------------------
        // The slow path used when a quick loop through the channels didn't come up
        // with anything. We may need to block and/or mark channels as done.
        //

        private bool MoveNextSlowPath()
        {
            int doneChannels = 0;

            // Remember the first channel we are looking at. If we pass through all of the
            // channels without finding an element, we will go to sleep.
            int firstChannelIndex = _channelIndex;

            int currChannelIndex;
            while ((currChannelIndex = _channelIndex) != _channels.Length)
            {
                AsynchronousChannel<T> current = _channels[currChannelIndex];

                bool isDone = _done[currChannelIndex];
                if (!isDone && current.TryDequeue(ref _currentElement))
                {
                    // The channel has an item to be processed. We already remembered the current
                    // element (Dequeue stores it as an out-parameter), so we just return true
                    // after advancing to the next channel.
                    _channelIndex = (currChannelIndex + 1) % _channels.Length;
                    return true;
                }
                else
                {
                    // There isn't an element in the current channel. Check whether the channel
                    // is done before possibly waiting for an element to arrive.
                    if (!isDone && current.IsDone)
                    {
                        // We must check to ensure an item didn't get enqueued after originally
                        // trying to dequeue above and reading the IsDone flag. If there are still
                        // elements, the producer may have marked the channel as done but of course
                        // we still need to continue processing them.
                        if (!current.IsChunkBufferEmpty)
                        {
                            bool dequeueResult = current.TryDequeue(ref _currentElement);
                            Debug.Assert(dequeueResult, "channel isn't empty, yet the dequeue failed, hmm");
                            return true;
                        }

                        // Mark this channel as being truly done. We won't consider it any longer.
                        _done[currChannelIndex] = true;
                        isDone = true;
                        current.Dispose();
                    }

                    if (isDone)
                    {
                        Debug.Assert(_channels[currChannelIndex].IsDone, "thought this channel was done");
                        Debug.Assert(_channels[currChannelIndex].IsChunkBufferEmpty, "thought this channel was empty");

                        // Increment the count of done channels that we've seen. If this reaches the
                        // total number of channels, we know we're finally done.
                        if (++doneChannels == _channels.Length)
                        {
                            // Remember that we are done by setting the index past the end.
                            _channelIndex = currChannelIndex = _channels.Length;
                            break;
                        }
                    }

                    // Still no element. Advance to the next channel and continue searching.
                    _channelIndex = currChannelIndex = (currChannelIndex + 1) % _channels.Length;

                    // If the channels aren't done, and we've inspected all of the queues and still
                    // haven't found anything, we will go ahead and wait on all the queues.
                    if (currChannelIndex == firstChannelIndex)
                    {
                        // On our first pass through the queues, we didn't have any side-effects
                        // that would let a producer know we are waiting. Now we go through and
                        // accumulate a set of events to wait on.
                        try
                        {
                            // Reset our done channels counter; we need to tally them again during the
                            // second pass through.
                            doneChannels = 0;

                            for (int i = 0; i < _channels.Length; i++)
                            {
                                bool channelIsDone = false;
                                if (!_done[i] && _channels[i].TryDequeue(ref _currentElement, ref channelIsDone))
                                {
                                    // The channel has received an item since the last time we checked.
                                    // Just return and let the consumer process the element returned.
                                    return true;
                                }
                                else if (channelIsDone)
                                {
                                    if (!_done[i])
                                    {
                                        _done[i] = true;
                                    }

                                    if (++doneChannels == _channels.Length)
                                    {
                                        // No need to wait. All channels are done. Remember this by setting
                                        // the index past the end of the channel list.
                                        _channelIndex = currChannelIndex = _channels.Length;
                                        break;
                                    }
                                }
                            }

                            // If all channels are done, we can break out of the loop entirely.
                            if (currChannelIndex == _channels.Length)
                            {
                                break;
                            }

                            //This Wait() does not require cancellation support as it will wake up when all the producers into the
                            //channel have finished.  Hence, if all the producers wake up on cancellation, so will this.
                            _consumerEvent.Wait();
                            _channelIndex = currChannelIndex = _consumerEvent.Value;
                            _consumerEvent.Reset();

                            //
                            // We have woken up, and the channel that caused this is contained in the
                            // returned index. This could be due to one of two reasons. Either the channel's
                            // producer has notified that it is done, in which case we just have to take it
                            // out of our current wait-list and redo the wait, or a channel actually has an
                            // item which we will go ahead and process.
                            //
                            // We just go back 'round the loop to accomplish this logic. Reset the channel
                            // index and # of done channels. Go back to the beginning, starting with the channel
                            // that caused us to wake up.
                            //

                            firstChannelIndex = currChannelIndex;
                            doneChannels = 0;
                        }
                        finally
                        {
                            // We have to guarantee that any waits we said we would perform are undone.
                            for (int i = 0; i < _channels.Length; i++)
                            {
                                // If we retrieved an event from a channel, we need to reset the wait.
                                if (!_done[i])
                                {
                                    // We may be calling DoneWithDequeueWait() unnecessarily here, since some of these
                                    // are not necessarily set as waiting. Unnecessary calls to DoneWithDequeueWait()
                                    // must be accepted by the channel.
                                    _channels[i].DoneWithDequeueWait();
                                }
                            }
                        }
                    }
                }
            }

            TraceHelpers.TraceInfo("[timing]: {0}: Completed the merge", DateTime.Now.Ticks);

            // If we got this far, it means we've exhausted our channels.
            Debug.Assert(currChannelIndex == _channels.Length);

            // If any tasks failed, propagate the failure now. We must do it here, because the merge
            // executor returns control back to the caller before the query has completed; contrast
            // this with synchronous enumeration where we can wait before returning.
            _taskGroupState.QueryEnd(false);

            return false;
        }

        public override void Dispose()
        {
            if (_consumerEvent != null)
            {
                // MergeEnumerator.Dispose() will wait until all producers complete.
                // So, we can be sure that no producer will attempt to signal the consumer event, and
                // we can dispose it.
                base.Dispose();

                _consumerEvent.Dispose();
                _consumerEvent = null;
            }
        }
    }
}
