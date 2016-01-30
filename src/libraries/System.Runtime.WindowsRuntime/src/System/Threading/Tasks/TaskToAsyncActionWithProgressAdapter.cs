// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
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
            Debug.Assert(userCompletionHandler != null);
            userCompletionHandler(this, asyncStatus);
        }


        internal override void OnProgress(AsyncActionProgressHandler<TProgress> userProgressHandler, TProgress progressInfo)
        {
            Debug.Assert(userProgressHandler != null);
            userProgressHandler(this, progressInfo);
        }
    }  // class TaskToAsyncActionWithProgressAdapter<TProgress>
}  // namespace

// TaskToAsyncActionWithProgressAdapter.cs
