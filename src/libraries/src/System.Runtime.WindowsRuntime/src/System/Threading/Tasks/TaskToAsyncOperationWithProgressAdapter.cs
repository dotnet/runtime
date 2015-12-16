// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace System.Threading.Tasks
{
    internal class TaskToAsyncOperationWithProgressAdapter<TResult, TProgress>
                            : TaskToAsyncInfoAdapter<AsyncOperationWithProgressCompletedHandler<TResult, TProgress>,
                                                     AsyncOperationProgressHandler<TResult, TProgress>,
                                                     TResult,
                                                     TProgress>,
                              IAsyncOperationWithProgress<TResult, TProgress>
    {
        internal TaskToAsyncOperationWithProgressAdapter(Delegate taskGenerator)

             : base(taskGenerator)
        {
        }

        // This is currently not used, so commented out to save code.
        // Leaving this is the source to be uncommented in case we decide to support IAsyncOperationWithProgress-consturction from a Task.
        //
        //internal TaskToAsyncOperationWithProgressAdapter(Task underlyingTask, CancellationTokenSource underlyingCancelTokenSource,
        //                                                 Progress<TProgress> underlyingProgressDispatcher)
        //
        //    : base(underlyingTask, underlyingCancelTokenSource, underlyingProgressDispatcher) {
        //}


        internal TaskToAsyncOperationWithProgressAdapter(TResult synchronousResult)

            : base(synchronousResult)
        {
        }


        public virtual TResult GetResults()
        {
            return GetResultsInternal();
        }


        internal override void OnCompleted(AsyncOperationWithProgressCompletedHandler<TResult, TProgress> userCompletionHandler,
                                           AsyncStatus asyncStatus)
        {
            Contract.Assert(userCompletionHandler != null);
            userCompletionHandler(this, asyncStatus);
        }


        internal override void OnProgress(AsyncOperationProgressHandler<TResult, TProgress> userProgressHandler, TProgress progressInfo)
        {
            Contract.Assert(userProgressHandler != null);
            userProgressHandler(this, progressInfo);
        }
    }  // class TaskToAsyncOperationWithProgressAdapter<TResult, TProgress>
}  // namespace

// TaskToAsyncOperationWithProgressAdapter.cs