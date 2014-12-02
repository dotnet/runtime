// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// IParallelPartitionable.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

namespace System.Linq.Parallel
{
    /// <summary>
    /// 
    /// An interface that allows developers to specify their own partitioning routines.
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IParallelPartitionable<T>
    {
        QueryOperatorEnumerator<T, int>[] GetPartitions(int partitionCount);
    }
}