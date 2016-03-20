// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ScanQueryOperator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A scan is just a simple operator that is positioned directly on top of some
    /// real data source. It's really just a place holder used during execution and
    /// analysis -- it should never actually get opened.
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    internal sealed class ScanQueryOperator<TElement> : QueryOperator<TElement>
    {
        private readonly IEnumerable<TElement> _data; // The actual data source to scan.

        //-----------------------------------------------------------------------------------
        // Constructs a new scan on top of the target data source.
        //

        internal ScanQueryOperator(IEnumerable<TElement> data)
            : base(Scheduling.DefaultPreserveOrder, QuerySettings.Empty)
        {
            Debug.Assert(data != null);

            ParallelEnumerableWrapper<TElement> wrapper = data as ParallelEnumerableWrapper<TElement>;
            if (wrapper != null)
            {
                data = wrapper.WrappedEnumerable;
            }

            _data = data;
        }

        //-----------------------------------------------------------------------------------
        // Accesses the underlying data source.
        //

        public IEnumerable<TElement> Data
        {
            get { return _data; }
        }

        //-----------------------------------------------------------------------------------
        // Override of the query operator base class's Open method. It creates a partitioned
        // stream that reads scans the data source.
        //

        internal override QueryResults<TElement> Open(QuerySettings settings, bool preferStriping)
        {
            Debug.Assert(settings.DegreeOfParallelism.HasValue);

            IList<TElement> dataAsList = _data as IList<TElement>;
            if (dataAsList != null)
            {
                return new ListQueryResults<TElement>(dataAsList, settings.DegreeOfParallelism.GetValueOrDefault(), preferStriping);
            }
            else
            {
                return new ScanEnumerableQueryOperatorResults(_data, settings);
            }
        }


        //-----------------------------------------------------------------------------------
        // IEnumerable<T> data source represented as QueryResults<T>. Typically, we would not
        // use ScanEnumerableQueryOperatorResults if the data source implements IList<T>.
        //

        internal override IEnumerator<TElement> GetEnumerator(ParallelMergeOptions? mergeOptions, bool suppressOrderPreservation)
        {
            return _data.GetEnumerator();
        }


        //---------------------------------------------------------------------------------------
        // Returns an enumerable that represents the query executing sequentially.
        //

        internal override IEnumerable<TElement> AsSequentialQuery(CancellationToken token)
        {
            return _data;
        }

        //---------------------------------------------------------------------------------------
        // The state of the order index of the results returned by this operator.
        //

        internal override OrdinalIndexState OrdinalIndexState
        {
            get
            {
                return _data is IList<TElement>
                    ? OrdinalIndexState.Indexable
                    : OrdinalIndexState.Correct;
            }
        }


        //---------------------------------------------------------------------------------------
        // Whether this operator performs a premature merge that would not be performed in
        // a similar sequential operation (i.e., in LINQ to Objects).
        //

        internal override bool LimitsParallelism
        {
            get { return false; }
        }

        private class ScanEnumerableQueryOperatorResults : QueryResults<TElement>
        {
            private IEnumerable<TElement> _data; // The data source for the query

            private QuerySettings _settings; // Settings collected from the query

            internal ScanEnumerableQueryOperatorResults(IEnumerable<TElement> data, QuerySettings settings)
            {
                _data = data;
                _settings = settings;
            }

            internal override void GivePartitionedStream(IPartitionedStreamRecipient<TElement> recipient)
            {
                // Since we are not using _data as an IList, we can pass useStriping = false.
                PartitionedStream<TElement, int> partitionedStream = ExchangeUtilities.PartitionDataSource(
                    _data, _settings.DegreeOfParallelism.Value, false);
                recipient.Receive<int>(partitionedStream);
            }
        }
    }
}
