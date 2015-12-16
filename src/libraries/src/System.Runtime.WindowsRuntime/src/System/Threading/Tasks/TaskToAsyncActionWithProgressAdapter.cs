// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace System.Threading.Tasks
{
    internal class TaskToAsyncActionWithProgressAdapter<TProgress>
                            : TaskToAsyncInfoAdapter<AsyncActionWithProgressCompletedHandler<TProgress>,
                                                     AsyncActionProgressHandler<TProgress>,
                                                     VoidValueTypeParameter,
                                                     TProgress>,
                              IAsyncActionWithProgress<TProgress>
    {
        internal TaskToAsyncActionWithProgressAdapter(Delegate taskGenerator)

             : base(taskGenerator)
        {
        }


        // This is currently not used, so commented out to save code.
        // Leaving this is the source to be uncommented in case we decide to support IAsyncActionWithProgress-consturction from a Task.
        //
        //internal TaskToAsyncActionWithProgressAdapter(Task underlyingTask, CancellationTokenSource underlyingCancelTokenSource,
        //                                                 Progress<TProgress> underlyingProgressDispatcher)
        //
        //    : base(underlyingTask, underlyingCancelTokenSource, underlyingProgressDispatcher) {
        //}


        internal TaskToAsyncActionWithProgressAdapter(bool isCanceled)

            : base(default(VoidValueTypeParameter))
        {
            if (isCanceled)
                DangerousSetCanceled();
        }


        public virtual void GetResults()
        {
            GetResultsInternal();
        }


        internal override void OnCompleted(AsyncActionWithProgressCompletedHandler<TProgress> userCompletionHandler, AsyncStatus asyncStatus)
        {
            Contract.Assert(userCompletionHandler != null);
            userCompletionHandler(this, asyncStatus);
        }


        internal override void OnProgress(AsyncActionProgressHandler<TProgress> userProgressHandler, TProgress progressInfo)
        {
            Contract.Assert(userProgressHandler != null);
            userProgressHandler(this, progressInfo);
        }
    }  // class TaskToAsyncActionWithProgressAdapter<TProgress>
}  // namespace

// TaskToAsyncActionWithProgressAdapter.cs