// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Threading.Tasks;

namespace System.Net
{
    // LazyAsyncResult - Base class for all IAsyncResult classes
    // that want to take advantage of lazy allocated event handles.
    internal class LazyAsyncResult : IAsyncResult
    {
        private const int HighBit = unchecked((int)0x80000000);
        private const int ForceAsyncCount = 50;

        // This is to avoid user mistakes when they queue another async op from a callback the completes sync.
        [ThreadStatic]
        private static ThreadContext s_ThreadContext;

        private static ThreadContext CurrentThreadContext
        {
            get
            {
                ThreadContext threadContext = s_ThreadContext;
                if (threadContext == null)
                {
                    threadContext = new ThreadContext();
                    s_ThreadContext = threadContext;
                }

                return threadContext;
            }
        }

        private class ThreadContext
        {
            internal int _nestedIOCount;
        }

#if DEBUG
        internal object _DebugAsyncChain = null;           // Optionally used to track chains of async calls.
        private bool _ProtectState;                 // Used by ContextAwareResult to prevent some calls.
#endif

        //
        // class members
        //
        private object _asyncObject;               // Caller's async object.
        private object _asyncState;                // Caller's state object.
        private AsyncCallback _asyncCallback;      // Caller's callback method.
        private object _result;                    // Final IO result to be returned byt the End*() method.
        private int _errorCode;                    // Win32 error code for Win32 IO async calls (that want to throw).
        private int _intCompleted;                 // Sign bit indicates synchronous completion if set.
                                                   // Remaining bits count the number of InvokeCallbak() calls.

        private bool _endCalled;                   // True if the user called the End*() method.
        private bool _userEvent;                   // True if the event has been (or is about to be) handed to the user

        private object _event;                     // Lazy allocated event to be returned in the IAsyncResult for the client to wait on.


        internal LazyAsyncResult(object myObject, object myState, AsyncCallback myCallBack)
        {
            _asyncObject = myObject;
            _asyncState = myState;
            _asyncCallback = myCallBack;
            _result = DBNull.Value;
            GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::.ctor()");
        }

        // Allows creating a pre-completed result with less interlockeds.  Beware!  Constructor calls the callback.
        // If a derived class ever uses this and overloads Cleanup, this may need to change.
        internal LazyAsyncResult(object myObject, object myState, AsyncCallback myCallBack, object result)
        {
            GlobalLog.Assert(result != DBNull.Value, "LazyAsyncResult#{0}::.ctor()|Result can't be set to DBNull - it's a special internal value.", Logging.HashString(this));
            _asyncObject = myObject;
            _asyncState = myState;
            _asyncCallback = myCallBack;
            _result = result;
            _intCompleted = 1;

            if (_asyncCallback != null)
            {
                GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::Complete() invoking callback");
                _asyncCallback(this);
            }
            else
            {
                GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::Complete() no callback to invoke");
            }

            GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::.ctor() (pre-completed)");
        }

        // Interface method to return the original async object.
        internal object AsyncObject
        {
            get
            {
                return _asyncObject;
            }
        }

        // Interface method to return the caller's state object.
        public object AsyncState
        {
            get
            {
                return _asyncState;
            }
        }

        protected AsyncCallback AsyncCallback
        {
            get
            {
                return _asyncCallback;
            }

            set
            {
                _asyncCallback = value;
            }
        }

        // Interface property to return a WaitHandle that can be waited on for I/O completion.
        // This property implements lazy event creation.
        // If this is used, the event cannot be disposed because it is under the control of the
        // application.  Internal should use InternalWaitForCompletion instead - never AsyncWaitHandle.
        public WaitHandle AsyncWaitHandle
        {
            get
            {
                GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::get_AsyncWaitHandle()");

#if DEBUG
                // Can't be called when state is protected.
                if (_ProtectState)
                {
                    throw new InvalidOperationException("get_AsyncWaitHandle called in protected state");
                }
#endif

                ManualResetEvent asyncEvent;

                // Indicates that the user has seen the event; it can't be disposed.
                _userEvent = true;

                // The user has access to this object.  Lock-in CompletedSynchronously.
                if (_intCompleted == 0)
                {
                    Interlocked.CompareExchange(ref _intCompleted, HighBit, 0);
                }

                // Because InternalWaitForCompletion() tries to dispose this event, it's
                // possible for _event to become null immediately after being set, but only if
                // IsCompleted has become true.  Therefore it's possible for this property
                // to give different (set) events to different callers when IsCompleted is true.
                asyncEvent = (ManualResetEvent)_event;
                while (asyncEvent == null)
                {
                    LazilyCreateEvent(out asyncEvent);
                }

                GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::get_AsyncWaitHandle() _event:" + Logging.HashString(_event));
                return asyncEvent;
            }
        }

        // Returns true if this call created the event.
        // May return with a null handle.  That means it thought it got one, but it was disposed in the mean time.
        private bool LazilyCreateEvent(out ManualResetEvent waitHandle)
        {
            waitHandle = new ManualResetEvent(false);
            try
            {
                if (Interlocked.CompareExchange(ref _event, waitHandle, null) == null)
                {
                    if (InternalPeekCompleted)
                    {
                        waitHandle.Set();
                    }
                    return true;
                }
                else
                {
                    waitHandle.Dispose();
                    waitHandle = (ManualResetEvent)_event;
                    // There's a chance here that _event became null.  But the only way is if another thread completed
                    // in InternalWaitForCompletion and disposed it.  If we're in InternalWaitForCompletion, we now know
                    // IsCompleted is set, so we can avoid the wait when waitHandle comes back null.  AsyncWaitHandle
                    // will try again in this case.
                    return false;
                }
            }
            catch
            {
                // This should be very rare, but doing this will reduce the chance of deadlock.
                _event = null;
                if (waitHandle != null)
                {
                    waitHandle.Dispose();
                }
                    
                throw;
            }
        }

        // This allows ContextAwareResult to not let anyone trigger the CompletedSynchronously tripwire while the context is being captured.
        [Conditional("DEBUG")]
        protected void DebugProtectState(bool protect)
        {
#if DEBUG
            _ProtectState = protect;
#endif
        }

        // Interface property, returning synchronous completion status.
        public bool CompletedSynchronously
        {
            get
            {
                GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::get_CompletedSynchronously()");

#if DEBUG
                // Can't be called when state is protected.
                if (_ProtectState)
                {
                    throw new InvalidOperationException("get_CompletedSynchronously called in protected state");
                }
#endif

                // If this returns greater than zero, it means it was incremented by InvokeCallback before anyone ever saw it.
                int result = _intCompleted;
                if (result == 0)
                {
                    result = Interlocked.CompareExchange(ref _intCompleted, HighBit, 0);
                }

                GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::get_CompletedSynchronously() returns: " + ((result > 0) ? "true" : "false"));
                return result > 0;
            }
        }

        // Interface property, returning completion status.
        public bool IsCompleted
        {
            get
            {
                GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::get_IsCompleted()");

#if DEBUG
                // Can't be called when state is protected.
                if (_ProtectState)
                {
                    throw new InvalidOperationException("get_IsCompleted called in protected state");
                }
#endif

                // Verify low bits to see if it's been incremented.  If it hasn't, set the high bit
                // to show that it's been looked at.
                int result = _intCompleted;
                if (result == 0)
                {
                    result = Interlocked.CompareExchange(ref _intCompleted, HighBit, 0);
                }

                return (result & ~HighBit) != 0;
            }
        }

        // Use to see if something's completed without fixing CompletedSynchronously.
        internal bool InternalPeekCompleted
        {
            get
            {
                return (_intCompleted & ~HighBit) != 0;
            }
        }

        // Internal property for setting the IO result.
        internal object Result
        {
            get
            {
                return _result == DBNull.Value ? null : _result;
            }
            set
            {
                // Ideally this should never be called, since setting
                // the result object really makes sense when the IO completes.
                //
                // But if the result was set here (as a preemptive error or for some other reason),
                // then the "result" parameter passed to InvokeCallback() will be ignored.

                // It's an error to call after the result has been completed or with DBNull.
                GlobalLog.Assert(value != DBNull.Value, "LazyAsyncResult#{0}::set_Result()|Result can't be set to DBNull - it's a special internal value.", Logging.HashString(this));
                GlobalLog.Assert(!InternalPeekCompleted, "LazyAsyncResult#{0}::set_Result()|Called on completed result.", Logging.HashString(this));
                _result = value;
            }
        }

        internal bool EndCalled
        {
            get
            {
                return _endCalled;
            }
            set
            {
                _endCalled = value;
            }
        }

        // Internal property for setting the Win32 IO async error code.
        internal int ErrorCode
        {
            get
            {
                return _errorCode;
            }
            set
            {
                _errorCode = value;
            }
        }

        // A method for completing the IO with a result and invoking the user's callback.
        // Used by derived classes to pass context into an overridden Complete().  Useful
        // for determining the 'winning' thread in case several may simultaneously call
        // the equivalent of InvokeCallback().
        protected void ProtectedInvokeCallback(object result, IntPtr userToken)
        {
            GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::ProtectedInvokeCallback() result = " +
                            (result is Exception ? ((Exception)result).Message : result == null ? "<null>" : result.ToString()) +
                            ", userToken:" + userToken.ToString());

            // Critical to disallow DBNull here - it could result in a stuck spinlock in WaitForCompletion.
            if (result == DBNull.Value)
            {
                throw new ArgumentNullException("result");
            }

#if DEBUG
            // Always safe to ask for the state now.
            _ProtectState = false;
#endif

            if ((_intCompleted & ~HighBit) == 0 && (Interlocked.Increment(ref _intCompleted) & ~HighBit) == 1)
            {
                // DBNull.Value is used to guarantee that the first caller wins,
                // even if the result was set to null.
                if (_result == DBNull.Value)
                {
                    _result = result;
                }

                ManualResetEvent asyncEvent = (ManualResetEvent)_event;
                if (asyncEvent != null)
                {
                    try
                    {
                        asyncEvent.Set();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Simply ignore this exception - There is apparently a rare race condition
                        // where the event is disposed before the completion method is called.
                    }
                }

                Complete(userToken);
            }
        }

        // A method for completing the IO with a result and invoking the user's callback.
        internal void InvokeCallback(object result)
        {
            ProtectedInvokeCallback(result, IntPtr.Zero);
        }

        // A method for completing the IO without a result and invoking the user's callback.
        internal void InvokeCallback()
        {
            ProtectedInvokeCallback(null, IntPtr.Zero);
        }

        //
        //  MUST NOT BE CALLED DIRECTLY
        //  A protected method that does callback job and it is guaranteed to be called exactly once.
        //  A derived overriding method must call the base class somewhere or the completion is lost.
        //
        protected virtual void Complete(IntPtr userToken)
        {
            bool offloaded = false;
            ThreadContext threadContext = CurrentThreadContext;
            try
            {
                ++threadContext._nestedIOCount;
                if (_asyncCallback != null)
                {
                    GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::Complete() invoking callback");

                    if (threadContext._nestedIOCount >= ForceAsyncCount)
                    {
                        GlobalLog.Print("LazyAsyncResult::Complete *** OFFLOADED the user callback ***");
                        Task.Factory.StartNew(
                            s => WorkerThreadComplete(s),
                            this,
                            CancellationToken.None,
                            TaskCreationOptions.DenyChildAttach,
                            TaskScheduler.Default);

                        offloaded = true;
                    }
                    else
                    {
                        _asyncCallback(this);
                    }
                }
                else
                {
                    GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::Complete() no callback to invoke");
                }
            }
            finally
            {
                --threadContext._nestedIOCount;

                // Never call this method unless interlocked _intCompleted check has succeeded (like in this case)
                if (!offloaded)
                {
                    Cleanup();
                }
            }
        }

        // Only called in the above method
        private static void WorkerThreadComplete(object state)
        {
            Debug.Assert(state is LazyAsyncResult);
            LazyAsyncResult thisPtr = (LazyAsyncResult)state;

            try
            {
                thisPtr._asyncCallback(thisPtr);
            }
            finally
            {
                thisPtr.Cleanup();
            }
        }

        // Custom instance cleanup method.
        // Derived types override this method to release unmanaged resources associated with an IO request.
        protected virtual void Cleanup()
        {
        }

        internal object InternalWaitForCompletion()
        {
            return WaitForCompletion(true);
        }

        private object WaitForCompletion(bool snap)
        {
            ManualResetEvent waitHandle = null;
            bool createdByMe = false;
            bool complete = snap ? IsCompleted : InternalPeekCompleted;

            if (!complete)
            {
                // Not done yet, so wait:
                waitHandle = (ManualResetEvent)_event;
                if (waitHandle == null)
                {
                    createdByMe = LazilyCreateEvent(out waitHandle);
                }
            }

            if (waitHandle != null)
            {
                try
                {
                    GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::InternalWaitForCompletion() Waiting for completion _event#" + Logging.HashString(waitHandle));
                    waitHandle.WaitOne(Timeout.Infinite);
                }
                catch (ObjectDisposedException)
                {
                    // This can occur if this method is called from two different threads.
                    // This possibility is the trade-off for not locking.
                }
                finally
                {
                    // We also want to dispose the event although we can't unless we did wait on it here.
                    if (createdByMe && !_userEvent)
                    {
                        // Does _userEvent need to be volatile (or _event set via Interlocked) in order
                        // to avoid giving a user a disposed event?
                        ManualResetEvent oldEvent = (ManualResetEvent)_event;
                        _event = null;
                        if (!_userEvent)
                        {
                            oldEvent.Dispose();
                        }
                    }
                }
            }

            // A race condition exists because InvokeCallback sets _intCompleted before _result (so that _result
            // can benefit from the synchronization of _intCompleted).  That means you can get here before _result got
            // set (although rarely - once every eight hours of stress).  Handle that case with a spin-lock.

            SpinWait sw = new SpinWait();
            while (_result == DBNull.Value)
            {
                sw.SpinOnce();
            }

            GlobalLog.Print("LazyAsyncResult#" + Logging.HashString(this) + "::InternalWaitForCompletion() done: " +
                            (_result is Exception ? ((Exception)_result).Message : _result == null ? "<null>" : _result.ToString()));

            return _result;
        }

        // A general interface that is called to release unmanaged resources associated with the class.
        // It completes the result but doesn't do any of the notifications.
        internal void InternalCleanup()
        {
            if ((_intCompleted & ~HighBit) == 0 && (Interlocked.Increment(ref _intCompleted) & ~HighBit) == 1)
            {
                // Set no result so that just in case there are waiters, they don't hang in the spin lock.
                _result = null;
                Cleanup();
            }
        }
    }
}
