// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace System.ComponentModel
{
    public delegate void AsyncCompletedEventHandler(object sender, AsyncCompletedEventArgs e);

    public class AsyncCompletedEventArgs : EventArgs
    {
        public AsyncCompletedEventArgs(Exception error, bool cancelled, object userState)
        {
            _cancelled = cancelled;
            _error = error;
            _state = userState;
        }

        protected void RaiseExceptionIfNecessary()
        {
            if (Error != null)
            {
                throw new TargetInvocationException(SR.Async_ExceptionOccurred, Error);
            }
            else if (Cancelled)
            {
                throw new InvalidOperationException(SR.Async_OperationCancelled);
            }
        }

        public bool Cancelled { get { return _cancelled; } }
        public Exception Error { get { return _error; } }
        public object UserState { get { return _state; } }

        private readonly bool _cancelled;
        private readonly Exception _error;
        private readonly object _state;

    }
}
