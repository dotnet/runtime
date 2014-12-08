// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// OrdinalIndexState.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

namespace System.Linq.Parallel
{
    /// <summary>
    /// Describes the state of order preservation index associated with an enumerator. 
    /// </summary>
    internal enum OrdinalIndexState : byte
    {
        Indexible = 0,   // Indices of elements are 0,1,2,... An element with any index can be accessed in constant time.
        Correct = 1,     // Indices of elements are 0,1,2,... Within each partition, elements are in the correct order.
        Increasing = 2,  // Indices of elements are increasing. Within each partition, elements are in the correct order.
        Shuffled = 3,    // Indices are of arbitrary type. Elements appear in an arbitrary order in each partition.
    }
}