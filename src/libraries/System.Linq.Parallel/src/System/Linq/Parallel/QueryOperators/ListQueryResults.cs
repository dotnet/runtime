// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal sealed class ListQueryResults<T> : QueryResults<T>
    {
#if NET11_0_OR_GREATER // IList<T> : IReadOnlyList<T> on .NET 11+
        private readonly IReadOnlyList<T> _source;
#else
        private readonly IList<T> _source;
#endif
        private readonly int _partitionCount;
        private readonly bool _useStriping;

        internal ListQueryResults(
#if NET11_0_OR_GREATER // IList<T> : IReadOnlyList<T> on .NET 11+
            IReadOnlyList<T> source,
#else
            IList<T> source,
#endif
            int partitionCount,
            bool useStriping)
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
