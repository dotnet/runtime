// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.ExceptionServices;

namespace System.ComponentModel
{
    public delegate void AsyncCompletedEventHandler(object sender, AsyncCompletedEventArgs e);

    public class AsyncCompletedEventArgs : EventArgs
    {
        public AsyncCompletedEventArgs(Exception error, bool cancelled, Object userState)
        {
            _cancelled = cancelled;
            _error = error;
            _state = userState;

            if (error != null)
            {
                _edi = ExceptionDispatchInfo.Capture(error);
            }
        }

        protected void RaiseExceptionIfNecessary()
        {
            if (Cancelled)
            {
                throw new OperationCanceledException(SR.Async_OperationCancelled);
            }

            if (_edi != null)
            {
                _edi.Throw();
            }
        }

        public bool Cancelled { get { return _cancelled; } }
        public Exception Error { get { return _error; } }
        public Object UserState { get { return _state; } }

        private readonly bool _cancelled;
        private readonly Exception _error;
        private readonly object _state;
        private readonly ExceptionDispatchInfo _edi;

    }
}
