// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Net
{
    internal partial class ContextAwareResult : IAsyncResult
    {
        private AsyncCallback _callback;

        private static Func<object> _resultFactory;

        public static void FakeSetResultFactory(Func<object> resultFactory)
        {
            _resultFactory = resultFactory;
        }

        public object AsyncState
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal bool EndCalled
        {
            get;
            set;
        }

        internal object Result
        {
            get
            {
                return _resultFactory?.Invoke();
            }
        }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool CompletedSynchronously
        {
            get
            {
                // Simulate sync completion:
                return true;
            }
        }

        public bool IsCompleted
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal ContextAwareResult(object myObject, object myState, AsyncCallback myCallBack)
        {
            _callback = myCallBack;
        }

        internal object StartPostingAsyncOp(bool lockCapture)
        {
            return null;
        }

        internal bool FinishPostingAsyncOp()
        {
            return true;
        }

        internal void InvokeCallback(object result)
        {
            _callback.Invoke(this);
        }

        internal void InternalWaitForCompletion() { }


    }
}
