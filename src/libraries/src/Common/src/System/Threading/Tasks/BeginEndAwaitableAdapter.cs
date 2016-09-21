// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    /// <summary>Enables awaiting a Begin/End method pair.</summary>
    internal sealed class BeginEndAwaitableAdapter : ICriticalNotifyCompletion
    {
        private readonly static Action CALLBACK_RAN = () => { };
        private IAsyncResult _asyncResult;
        private Action _continuation;

        public readonly static AsyncCallback Callback = asyncResult =>
        {
            Debug.Assert(asyncResult != null);
            Debug.Assert(asyncResult.IsCompleted);
            Debug.Assert(asyncResult.AsyncState is BeginEndAwaitableAdapter);

            BeginEndAwaitableAdapter adapter = (BeginEndAwaitableAdapter)asyncResult.AsyncState;
            adapter._asyncResult = asyncResult;

            Action continuation = Interlocked.Exchange(ref adapter._continuation, CALLBACK_RAN);
            if (continuation != null)
            {
                Debug.Assert(continuation != CALLBACK_RAN);
                continuation();
            }
        };

        public BeginEndAwaitableAdapter GetAwaiter() => this;

        public bool IsCompleted => _continuation == CALLBACK_RAN;

        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

        public void OnCompleted(Action continuation)
        {
            if (_continuation == CALLBACK_RAN || Interlocked.CompareExchange(ref _continuation, continuation, null) == CALLBACK_RAN)
            {
                Task.Run(continuation);
            }
        }

        public IAsyncResult GetResult()
        {
            IAsyncResult result = _asyncResult;
            Debug.Assert(result != null && result.IsCompleted);

            _asyncResult = null;
            _continuation = null;

            return result;
        }

    }
}
