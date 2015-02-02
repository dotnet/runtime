// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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
        }

        protected void RaiseExceptionIfNecessary()
        {
            if (this.Error != null)
            {
                // Project N Port Note: Deliberate breaking change from desktop to avoid a dependency on System.Reflection.
                throw this.Error; //System.Reflection.TargetInvocationException(SR.Async_ExceptionOccurred, this.Error);
            }
            if (this.Cancelled)
            {
                throw new InvalidOperationException(SR.Async_OperationCancelled);
            }
        }

        public bool Cancelled { get { return _cancelled; } }
        public Exception Error { get { return _error; } }
        public Object UserState { get { return _state; } }

        private bool _cancelled;
        private Exception _error;
        private object _state;
    }
}
