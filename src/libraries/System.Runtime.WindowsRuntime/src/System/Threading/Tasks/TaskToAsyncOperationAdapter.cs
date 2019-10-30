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
    internal class TaskToAsyncOperationAdapter<TResult>
                    : TaskToAsyncInfoAdapter<AsyncOperationCompletedHandler<TResult>, VoidReferenceTypeParameter, TResult, VoidValueTypeParameter>,
                      IAsyncOperation<TResult>
    {
        internal TaskToAsyncOperationAdapter(Delegate taskGenerator)

             : base(taskGenerator)
        {
        }


        internal TaskToAsyncOperationAdapter(Task underlyingTask, CancellationTokenSource underlyingCancelTokenSource)

            : base(underlyingTask, underlyingCancelTokenSource, underlyingProgressDispatcher: null)
        {
        }


        internal TaskToAsyncOperationAdapter(TResult synchronousResult)

            : base(synchronousResult)
        {
        }


        public virtual TResult GetResults()
        {
            return GetResultsInternal();
        }


        internal override void OnCompleted(AsyncOperationCompletedHandler<TResult> userCompletionHandler, AsyncStatus asyncStatus)
        {
            Debug.Assert(userCompletionHandler != null);
            userCompletionHandler(this, asyncStatus);
        }
    }  // class TaskToAsyncOperationAdapter<TResult>
}  // namespace

// TaskToAsyncOperationAdapter.cs
