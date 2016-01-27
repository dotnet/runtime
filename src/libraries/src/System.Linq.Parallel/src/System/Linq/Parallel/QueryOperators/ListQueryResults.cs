// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ListQueryResults.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Class to represent an IList{T} as QueryResults{T} 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ListQueryResults<T> : QueryResults<T>
    {
        private IList<T> _source;
        private int _partitionCount;
        private bool _useStriping;

        internal ListQueryResults(IList<T> source, int partitionCount, bool useStriping)
        {
            _source = source;
            _partitionCount = partitionCount;
            _useStriping = useStriping;
        }

        internal override void GivePartitionedStream(IPartitionedStreamRecipient<T> recipient)
        {
            PartitionedStream<T, int> partitionedStream = GetPartitionedStream();
            recipient.Receive<int>(partitionedStream);
        }

        internal override bool IsIndexible
        {
            get { return true; }
        }

        internal override int ElementsCount
        {
            get { return _source.Count; }
        }

        internal override T GetElement(int index)
        {
            return _source[index];
        }

        internal PartitionedStream<T, int> GetPartitionedStream()
        {
            return ExchangeUtilities.PartitionDataSource(_source, _partitionCount, _useStriping);
        }
    }
}
