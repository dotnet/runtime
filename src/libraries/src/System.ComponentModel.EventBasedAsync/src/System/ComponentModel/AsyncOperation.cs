// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public SynchronizationContext SynchronizationContext
        {
            get
            {
                return _syncContext;
            }
        }

        public void Post(SendOrPostCallback d, object arg)
        {
            PostCore(d, arg, markCompleted: false);
        }

        public void PostOperationCompleted(SendOrPostCallback d, object arg)
        {
            PostCore(d, arg, markCompleted: true);
            OperationCompletedCore();
        }

        public void OperationCompleted()
        {
            VerifyNotCompleted();
            _alreadyCompleted = true;
            OperationCompletedCore();
        }

        private void PostCore(SendOrPostCallback d, object arg, bool markCompleted)
        {
            VerifyNotCompleted();
            VerifyDelegateNotNull(d);
            if (markCompleted)
            {
                // This call is in response to a PostOperationCompleted.  As such, we need to mark
                // _alreadyCompleted as true so that subsequent attempts to use this instance will
                // fail appropriately.  And we need to do that before we queue the callback, as
                // that callback could itself trigger additional attempts to use this instance.
                _alreadyCompleted = true;
            }
            _syncContext.Post(d, arg);
        }

        private void OperationCompletedCore()
        {
            try
            {
                _syncContext.OperationCompleted();
            }
            finally
            {
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
                throw new ArgumentNullException(nameof(d), SR.Async_NullDelegate);
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
