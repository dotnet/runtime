// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// DefaultMergeHelper.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel
{
    /// <summary>
    /// The default merge helper uses a set of straightforward algorithms for output
    /// merging. Namely, for synchronous merges, the input data is yielded from the
    /// input data streams in "depth first" left-to-right order. For asynchronous merges,
    /// on the other hand, we use a biased choice algorithm to favor input channels in
    /// a "fair" way. No order preservation is carried out by this helper.
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    /// <typeparam name="TIgnoreKey"></typeparam>
    internal sealed class DefaultMergeHelper<TInputOutput, TIgnoreKey> : IMergeHelper<TInputOutput>
    {
        private readonly QueryTaskGroupState _taskGroupState; // State shared among tasks.
        private readonly PartitionedStream<TInputOutput, TIgnoreKey> _partitions; // Source partitions.
        private readonly AsynchronousChannel<TInputOutput>[]? _asyncChannels; // Destination channels (async).
        private readonly SynchronousChannel<TInputOutput>[]? _syncChannels; // Destination channels (sync).
        private readonly IEnumerator<TInputOutput>? _channelEnumerator; // Output enumerator.
        private readonly TaskScheduler _taskScheduler; // The task manager to execute the query.
        private readonly bool _ignoreOutput; // Whether we're enumerating "for effect".

        //-----------------------------------------------------------------------------------
        // Instantiates a new merge helper.
        //
        // Arguments:
        //     partitions   - the source partitions from which to consume data.
        //     ignoreOutput - whether we're enumerating "for effect" or for output.
        //     pipeline     - whether to use a pipelined merge.
        //

        internal DefaultMergeHelper(PartitionedStream<TInputOutput, TIgnoreKey> partitions, bool ignoreOutput, ParallelMergeOptions options,
            TaskScheduler taskScheduler, CancellationState cancellationState, int queryId)
        {
            Debug.Assert(partitions != null);

            _taskGroupState = new QueryTaskGroupState(cancellationState, queryId);
            _partitions = partitions;
            _taskScheduler = taskScheduler;
            _ignoreOutput = ignoreOutput;
            IntValueEvent consumerEvent = new IntValueEvent();

            TraceHelpers.TraceInfo("DefaultMergeHelper::.ctor(..): creating a default merge helper");

            // If output won't be ignored, we need to manufacture a set of channels for the consumer.
            // Otherwise, when the merge is executed, we'll just invoke the activities themselves.
            if (!ignoreOutput)
            {
                // Create the asynchronous or synchronous channels, based on whether we're pipelining.
                if (options != ParallelMergeOptions.FullyBuffered)
                {
                    if (partitions.PartitionCount > 1)
                    {
                        Debug.Assert(!ParallelEnumerable.SinglePartitionMode);
                        _asyncChannels =
                            MergeExecutor<TInputOutput>.MakeAsynchronousChannels(partitions.PartitionCount, options, consumerEvent, cancellationState.MergedCancellationToken);
                        _channelEnumerator = new AsynchronousChannelMergeEnumerator<TInputOutput>(_taskGroupState, _asyncChannels, consumerEvent);
                    }
                    else
                    {
                        // If there is only one partition, we don't need to create channels. The only producer enumerator
                        // will be used as the result enumerator.
                        _channelEnumerator = ExceptionAggregator.WrapQueryEnumerator(partitions[0], _taskGroupState.CancellationState).GetEnumerator();
                    }
                }
                else
                {
                    _syncChannels =
                        MergeExecutor<TInputOutput>.MakeSynchronousChannels(partitions.PartitionCount);
                    _channelEnumerator = new SynchronousChannelMergeEnumerator<TInputOutput>(_taskGroupState, _syncChannels);
                }

                Debug.Assert(_asyncChannels == null || _asyncChannels.Length == partitions.PartitionCount);
                Debug.Assert(_syncChannels == null || _syncChannels.Length == partitions.PartitionCount);
                Debug.Assert(_channelEnumerator != null, "enumerator can't be null if we're not ignoring output");
            }
        }

        //-----------------------------------------------------------------------------------
        // Schedules execution of the merge itself.
        //
        // Arguments:
        //    ordinalIndexState - the state of the ordinal index of the merged partitions
        //

        void IMergeHelper<TInputOutput>.Execute()
        {
            if (_asyncChannels != null)
            {
                Debug.Assert(!ParallelEnumerable.SinglePartitionMode);
                SpoolingTask.SpoolPipeline<TInputOutput, TIgnoreKey>(_taskGroupState, _partitions, _asyncChannels, _taskScheduler);
            }
            else if (_syncChannels != null)
            {
                SpoolingTask.SpoolStopAndGo<TInputOutput, TIgnoreKey>(_taskGroupState, _partitions, _syncChannels, _taskScheduler);
            }
            else if (_ignoreOutput)
            {
                SpoolingTask.SpoolForAll<TInputOutput, TIgnoreKey>(_taskGroupState, _partitions, _taskScheduler);
            }
            else
            {
                // The last case is a pipelining merge when DOP = 1. In this case, the consumer thread itself will compute the results,
                // so we don't need any tasks to compute the results asynchronously.
                Debug.Assert(_partitions.PartitionCount == 1);
            }
        }

        //-----------------------------------------------------------------------------------
        // Gets the enumerator from which to enumerate output results.
        //

        IEnumerator<TInputOutput> IMergeHelper<TInputOutput>.GetEnumerator()
        {
            Debug.Assert(_ignoreOutput || _channelEnumerator != null);
            return _channelEnumerator!;
        }

        //-----------------------------------------------------------------------------------
        // Returns the results as an array.
        //
        // There isn't much reason to call this method on a DefaultMergeHelper,
        // because DefaultMergeHelper does not have an array to efficiently hand out, and
        // has to build one up. However, in some uncommon circumstances, this method will be called.
        //

        public TInputOutput[] GetResultsAsArray()
        {
            if (_syncChannels != null)
            {
                // Right size an array.
                int totalSize = 0;
                for (int i = 0; i < _syncChannels.Length; i++)
                {
                    totalSize += _syncChannels[i].Count;
                }
                TInputOutput[] array = new TInputOutput[totalSize];

                // And then blit the elements in.
                int current = 0;
                for (int i = 0; i < _syncChannels.Length; i++)
                {
                    _syncChannels[i].CopyTo(array, current);
                    current += _syncChannels[i].Count;
                }
                return array;
            }
            else
            {
                List<TInputOutput> output = new List<TInputOutput>();
                using (IEnumerator<TInputOutput> enumerator = ((IMergeHelper<TInputOutput>)this).GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        output.Add(enumerator.Current);
                    }
                }

                return output.ToArray();
            }
        }
    }
}
