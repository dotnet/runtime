// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// ImergeHelper.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Used as a stand-in for replaceable merge algorithms. Alternative implementations
    /// are chosen based on the style of merge required. 
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    internal interface IMergeHelper<TInputOutput>
    {
        // Begins execution of the merge.
        void Execute();

        // Return an enumerator that yields the merged output.
        IEnumerator<TInputOutput> GetEnumerator();

        // Returns the merged output as an array.
        TInputOutput[] GetResultsAsArray();
    }
}
