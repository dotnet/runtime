
////-----------------------------------------------------------------------------
//// Copyright (c) Microsoft Corporation.  All rights reserved.
////-----------------------------------------------------------------------------
 
//namespace System.Runtime
//{
//    using System;
//    using System.Diagnostics;
//    using System.Diagnostics.CodeAnalysis;
//    using System.Threading;

//    // AsyncResult starts acquired; Complete releases.
//    //[Fx.Tag.SynchronizationPrimitive(Fx.Tag.BlocksUsing.ManualResetEvent, SupportsAsync = true, ReleaseMethod = "Complete")]
//    abstract class AsyncResult : IAsyncResult
//    {
//        static AsyncCallback asyncCompletionWrapperCallback;
//        AsyncCallback callback;
//        bool completedSynchronously;
//        bool endCalled;
//        Exception exception;
//        bool isCompleted;
//        AsyncCompletion nextAsyncCompletion;
//        object state;
//        Action beforePrepareAsyncCompletionAction;
//        Func<IAsyncResult, bool> checkSyncValidationFunc;

//        //[Fx.Tag.SynchronizationObject]
//        ManualResetEvent manualResetEvent;

//        //[Fx.Tag.SynchronizationObject(Blocking = false)]
//        object thisLock;

//#if DEBUG
//        StackTrace endStack;
//        StackTrace completeStack;
//        UncompletedAsyncResultMarker marker;
//#endif

//        protected AsyncResult(AsyncCallback callback, object state)
//        {
//            this.callback = callback;
//            this.state = state;
//            this.thisLock = new object();

//#if DEBUG
//            this.marker = new UncompletedAsyncResultMarker(this);
//#endif
//        }

//        public object AsyncState
//        {
//            get
//            {
//                return state;
//            }
//        }

//        public WaitHandle AsyncWaitHandle
//        {
//            get
//            {
//                if (manualResetEvent != null)
//                {
//                    return manualResetEvent;
//                }

//                lock (ThisLock)
//                {
//                    if (manualResetEvent == null)
//                    {
//                        manualResetEvent = new ManualResetEvent(isCompleted);
//                    }
//                }

//                return manualResetEvent;
//            }
//        }

//        public bool CompletedSynchronously
//        {
//            get
//            {
//                return completedSynchronously;
//            }
//        }

//        public bool HasCallback
//        {
//            get
//            {
//                return this.callback != null;
//            }
//        }

//        public bool IsCompleted
//        {
//            get
//            {
//                return isCompleted;
//            }
//        }

//        // used in conjunction with PrepareAsyncCompletion to allow for finally blocks
//        protected Action<AsyncResult, Exception> OnCompleting { get; set; }

//        object ThisLock
//        {
//            get
//            {
//                return this.thisLock;
//            }
//        }

//        // subclasses like TraceAsyncResult can use this to wrap the callback functionality in a scope
//        protected Action<AsyncCallback, IAsyncResult> VirtualCallback
//        {
//            get;
//            set;
//        }

//        protected void Complete(bool completedSynchronously)
//        {
//            if (this.isCompleted)
//            {
//                throw new InvalidOperationException();// Fx.Exception.AsError(new InvalidOperationException(InternalSR.AsyncResultCompletedTwice(GetType())));
//            }

//#if DEBUG
//            this.marker.AsyncResult = null;
//            this.marker = null;
//            //if (!Fx.FastDebug && completeStack == null)
//            //{
//            //    completeStack = new StackTrace();
//            //}
//#endif

//            this.completedSynchronously = completedSynchronously;
//            if (OnCompleting != null)
//            {
//                // Allow exception replacement, like a catch/throw pattern.
//                try
//                {
//                    OnCompleting(this, this.exception);
//                }
//                catch (Exception exception)
//                {
//                    //if (Fx.IsFatal(exception))
//                    //{
//                    //    throw;
//                    //}
//                    this.exception = exception;
//                }
//            }

//            if (completedSynchronously)
//            {
//                // If we completedSynchronously, then there's no chance that the manualResetEvent was created so
//                // we don't need to worry about a ----
//                //Fx.Assert(this.manualResetEvent == null, "No ManualResetEvent should be created for a synchronous AsyncResult.");
//                this.isCompleted = true;
//            }
//            else
//            {
//                lock (ThisLock)
//                {
//                    this.isCompleted = true;
//                    if (this.manualResetEvent != null)
//                    {
//                        this.manualResetEvent.Set();
//                    }
//                }
//            }

//            if (this.callback != null)
//            {
//                try
//                {
//                    if (VirtualCallback != null)
//                    {
//                        VirtualCallback(this.callback, this);
//                    }
//                    else
//                    {
//                        this.callback(this);
//                    }
//                }
//#pragma warning disable 1634
//#pragma warning suppress 56500 // transferring exception to another thread
//                catch (Exception e)
//                {
//                    //if (Fx.IsFatal(e))
//                    //{
//                    //    throw;
//                    //}

//                    throw new InvalidOperationException();// Fx.Exception.AsError(new CallbackException(InternalSR.AsyncCallbackThrewException, e));
//                }
//#pragma warning restore 1634
//            }
//        }

//        protected void Complete(bool completedSynchronously, Exception exception)
//        {
//            this.exception = exception;
//            Complete(completedSynchronously);
//        }

//        static void AsyncCompletionWrapperCallback(IAsyncResult result)
//        {
//            if (result == null)
//            {
//                throw new InvalidOperationException();//Fx.Exception.AsError(new InvalidOperationException(InternalSR.InvalidNullAsyncResult));
//            }
//            if (result.CompletedSynchronously)
//            {
//                return;
//            }

//            AsyncResult thisPtr = (AsyncResult)result.AsyncState;
//            if (!thisPtr.OnContinueAsyncCompletion(result))
//            {
//                return;
//            }

//            AsyncCompletion callback = thisPtr.GetNextCompletion();
//            if (callback == null)
//            {
//                ThrowInvalidAsyncResult(result);
//            }

//            bool completeSelf = false;
//            Exception completionException = null;
//            try
//            {
//                completeSelf = callback(result);
//            }
//            catch (Exception e)
//            {
//                //if (Fx.IsFatal(e))
//                //{
//                //    throw;
//                //}
//                completeSelf = true;
//                completionException = e;
//            }

//            if (completeSelf)
//            {
//                thisPtr.Complete(false, completionException);
//            }
//        }

//        // Note: this should be only derived by the TransactedAsyncResult
//        protected virtual bool OnContinueAsyncCompletion(IAsyncResult result)
//        {
//            return true;
//        }

//        // Note: this should be used only by the TransactedAsyncResult
//        protected void SetBeforePrepareAsyncCompletionAction(Action beforePrepareAsyncCompletionAction)
//        {
//            this.beforePrepareAsyncCompletionAction = beforePrepareAsyncCompletionAction;
//        }

//        // Note: this should be used only by the TransactedAsyncResult
//        protected void SetCheckSyncValidationFunc(Func<IAsyncResult, bool> checkSyncValidationFunc)
//        {
//            this.checkSyncValidationFunc = checkSyncValidationFunc;
//        }

//        protected AsyncCallback PrepareAsyncCompletion(AsyncCompletion callback)
//        {
//            if (this.beforePrepareAsyncCompletionAction != null)
//            {
//                this.beforePrepareAsyncCompletionAction();
//            }

//            this.nextAsyncCompletion = callback;
//            if (AsyncResult.asyncCompletionWrapperCallback == null)
//            {
//                //AsyncResult.asyncCompletionWrapperCallback = Fx.ThunkCallback(new AsyncCallback(AsyncCompletionWrapperCallback));
//            }
//            return AsyncResult.asyncCompletionWrapperCallback;
//        }

//        protected bool CheckSyncContinue(IAsyncResult result)
//        {
//            AsyncCompletion dummy;
//            return TryContinueHelper(result, out dummy);
//        }

//        protected bool SyncContinue(IAsyncResult result)
//        {
//            AsyncCompletion callback;
//            if (TryContinueHelper(result, out callback))
//            {
//                return callback(result);
//            }
//            else
//            {
//                return false;
//            }
//        }

//        bool TryContinueHelper(IAsyncResult result, out AsyncCompletion callback)
//        {
//            if (result == null)
//            {
//                throw new InvalidOperationException();//Fx.Exception.AsError(new InvalidOperationException(InternalSR.InvalidNullAsyncResult));
//            }

//            callback = null;
//            if (this.checkSyncValidationFunc != null)
//            {
//                if (!this.checkSyncValidationFunc(result))
//                {
//                    return false;
//                }
//            }
//            else if (!result.CompletedSynchronously)
//            {
//                return false;
//            }

//            callback = GetNextCompletion();
//            if (callback == null)
//            {
//                ThrowInvalidAsyncResult("Only call Check/SyncContinue once per async operation (once per PrepareAsyncCompletion).");
//            }
//            return true;
//        }

//        AsyncCompletion GetNextCompletion()
//        {
//            AsyncCompletion result = this.nextAsyncCompletion;
//            this.nextAsyncCompletion = null;
//            return result;
//        }

//        protected static void ThrowInvalidAsyncResult(IAsyncResult result)
//        {
//            throw new InvalidOperationException();//Fx.Exception.AsError(new InvalidOperationException(InternalSR.InvalidAsyncResultImplementation(result.GetType())));
//        }

//        protected static void ThrowInvalidAsyncResult(string debugText)
//        {

//            string message = "Default"; // testing- original=> InternalSR.InvalidAsyncResultImplementationGeneric;
//            if (debugText != null)
//            {
//#if DEBUG
//                message += " " + debugText;
//#endif
//            }
//            throw new InvalidOperationException();//Fx.Exception.AsError(new InvalidOperationException(message));
//        }

//        //[Fx.Tag.Blocking(Conditional = "!asyncResult.isCompleted")]
//        protected static TAsyncResult End<TAsyncResult>(IAsyncResult result)
//            where TAsyncResult : AsyncResult
//        {
//            if (result == null)
//            {
//                throw new ArgumentException();// Fx.Exception.ArgumentNull("result");
//            }

//            TAsyncResult asyncResult = result as TAsyncResult;

//            if (asyncResult == null)
//            {
//                throw new ArgumentException();//Fx.Exception.Argument("result", InternalSR.InvalidAsyncResult);
//            }

//            if (asyncResult.endCalled)
//            {
//                throw new ArgumentException();//Fx.Exception.AsError(new InvalidOperationException(InternalSR.AsyncResultAlreadyEnded));
//            }

//#if DEBUG
//            //if (!Fx.FastDebug && asyncResult.endStack == null)
//            //{
//            //    asyncResult.endStack = new StackTrace();
//            //}
//#endif

//            asyncResult.endCalled = true;

//            if (!asyncResult.isCompleted)
//            {
//                asyncResult.AsyncWaitHandle.WaitOne();
//            }

//            if (asyncResult.manualResetEvent != null)
//            {
//                asyncResult.manualResetEvent.Close();
//            }

//            if (asyncResult.exception != null)
//            {
//                throw new InvalidOperationException();// Fx.Exception.AsError(asyncResult.exception);
//            }

//            return asyncResult;
//        }

//        // can be utilized by subclasses to write core completion code for both the sync and async paths
//        // in one location, signalling chainable synchronous completion with the boolean result,
//        // and leveraging PrepareAsyncCompletion for conversion to an AsyncCallback.
//        // NOTE: requires that "this" is passed in as the state object to the asynchronous sub-call being used with a completion routine.
//        protected delegate bool AsyncCompletion(IAsyncResult result);

//#if DEBUG
//        class UncompletedAsyncResultMarker
//        {
//            public UncompletedAsyncResultMarker(AsyncResult result)
//            {
//                AsyncResult = result;
//            }

//            //[SuppressMessage(FxCop.Category.Performance, FxCop.Rule.AvoidUncalledPrivateCode,
//            //    Justification = "Debug-only facility")]
//            public AsyncResult AsyncResult { get; set; }
//        }
//#endif
//    }
//}
