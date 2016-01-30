// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
