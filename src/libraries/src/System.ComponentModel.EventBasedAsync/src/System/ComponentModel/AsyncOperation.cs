// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;

namespace System.ComponentModel
{
    public sealed class AsyncOperation
    {
        private readonly SynchronizationContext _syncContext;
        private readonly object _userSuppliedState;
        private bool _alreadyCompleted;

        /// <summary>
        ///     Constructor. Protected to avoid unwitting usage - AsyncOperation objects
        ///     are typically created by AsyncOperationManager calling CreateOperation.
        /// </summary>
        private AsyncOperation(object userSuppliedState, SynchronizationContext syncContext)
        {
            _userSuppliedState = userSuppliedState;
            _syncContext = syncContext;
            _alreadyCompleted = false;
            _syncContext.OperationStarted();
        }

        /// <summary>
        ///     Destructor. Guarantees that sync context will always get notified of completion.
        /// </summary>
        ~AsyncOperation()
        {
            if (!_alreadyCompleted && _syncContext != null)
            {
                _syncContext.OperationCompleted();
            }
        }

        public object UserSuppliedState
        {
            get
            {
                return _userSuppliedState; 
            }
        }

        /// <include file='doc\AsyncOperation.uex' path='docs/doc[@for="AsyncOperation.SynchronizationContext"]/*' />
        public SynchronizationContext SynchronizationContext
        {
            get
            {
                return _syncContext;
            }
        }

        public void Post(SendOrPostCallback d, object arg)
        {
            VerifyNotCompleted();
            VerifyDelegateNotNull(d);
            _syncContext.Post(d, arg);
        }

        public void PostOperationCompleted(SendOrPostCallback d, object arg)
        {
            Post(d, arg);
            OperationCompletedCore();
        }

        public void OperationCompleted()
        {
            VerifyNotCompleted();
            OperationCompletedCore();
        }

        private void OperationCompletedCore()
        {
            try
            {
                _syncContext.OperationCompleted();
            }
            finally
            {
                _alreadyCompleted = true;
                GC.SuppressFinalize(this);
            }
        }

        private void VerifyNotCompleted()
        {
            if (_alreadyCompleted)
            {
                throw new InvalidOperationException(SR.Async_OperationAlreadyCompleted);
            }
        }

        private void VerifyDelegateNotNull(SendOrPostCallback d)
        {
            if (d == null)
            {
                throw new ArgumentNullException(SR.Async_NullDelegate, "d");
            }
        }

        /// <summary>
        ///     Only for use by AsyncOperationManager to create new AsyncOperation objects
        /// </summary>
        internal static AsyncOperation CreateOperation(object userSuppliedState, SynchronizationContext syncContext)
        {
            AsyncOperation newOp = new AsyncOperation(userSuppliedState, syncContext);
            return newOp;
        }
    }
}
