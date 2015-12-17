// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace System.Threading.Tasks
{
    internal class TaskToAsyncActionAdapter
                        : TaskToAsyncInfoAdapter<AsyncActionCompletedHandler, VoidReferenceTypeParameter, VoidValueTypeParameter, VoidValueTypeParameter>,
                          IAsyncAction
    {
        internal TaskToAsyncActionAdapter(Delegate taskGenerator)

             : base(taskGenerator)
        {
        }


        internal TaskToAsyncActionAdapter(Task underlyingTask, CancellationTokenSource underlyingCancelTokenSource)

            : base(underlyingTask, underlyingCancelTokenSource, underlyingProgressDispatcher: null)
        {
        }


        internal TaskToAsyncActionAdapter(bool isCanceled)

            : base(default(VoidValueTypeParameter))
        {
            if (isCanceled)
                DangerousSetCanceled();
        }


        public virtual void GetResults()
        {
            GetResultsInternal();
        }


        internal override void OnCompleted(AsyncActionCompletedHandler userCompletionHandler, AsyncStatus asyncStatus)
        {
            Debug.Assert(userCompletionHandler != null);
            userCompletionHandler(this, asyncStatus);
        }
    }  // class TaskToAsyncActionAdapter
}  // namespace

// TaskToAsyncActionAdapter.cs