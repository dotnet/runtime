// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// MergeEnumerator.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Convenience class used by enumerators that merge many partitions into one. 
    /// </summary>
    /// <typeparam name="TInputOutput"></typeparam>
    internal abstract class MergeEnumerator<TInputOutput> : IEnumerator<TInputOutput>
    {
        protected QueryTaskGroupState _taskGroupState;

        //-----------------------------------------------------------------------------------
        // Initializes a new enumerator with the specified group state.
        //

        protected MergeEnumerator(QueryTaskGroupState taskGroupState)
        {
            Contract.Assert(taskGroupState != null);
            _taskGroupState = taskGroupState;
        }

        //-----------------------------------------------------------------------------------
        // Abstract members of IEnumerator<T> that must be implemented by concrete subclasses.
        //

        public abstract TInputOutput Current { get; }

        public abstract bool MoveNext();

        //-----------------------------------------------------------------------------------
        // Straightforward IEnumerator<T> methods. So subclasses needn't bother.
        //

        object IEnumerator.Current
        {
            get { return ((IEnumerator<TInputOutput>)this).Current; }
        }

        public virtual void Reset()
        {
            // (intentionally left blank)
        }

        //-----------------------------------------------------------------------------------
        // If the enumerator is disposed of before the query finishes, we need to ensure
        // we properly tear down the query such that exceptions are not lost.
        //

        public virtual void Dispose()
        {
            // If the enumerator is being disposed of before the query has finished,
            // we will wait for the query to finish.  Cancellation should have already
            // been initiated, so we just need to ensure exceptions are propagated.
            if (!_taskGroupState.IsAlreadyEnded)
            {
                Contract.Assert(_taskGroupState.CancellationState.TopLevelDisposedFlag.Value);
                _taskGroupState.QueryEnd(true);
            }
        }
    }
}