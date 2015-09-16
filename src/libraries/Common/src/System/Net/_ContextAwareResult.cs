// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Security;
using System.Security.Principal;

namespace System.Net
{
    //
    // This is used by ContextAwareResult to cache callback closures between similar calls.  Create one of these and
    // pass it in to FinishPostingAsyncOp() to prevent the context from being captured in every iteration of a looped async call.
    //
    // I thought about making the delegate and state into weak references, but decided against it because:
    //    - The delegate is very likely to be abandoned by the user right after calling BeginXxx, making caching it useless. There's
    //         no easy way to weakly reference just the target.
    //    - We want to support identifying state via object.Equals() (especially value types), which means we need to keep a
    //         reference to the original.  Plus, if we're holding the target, might as well hold the state too.
    // The user will need to disable caching if they want their target/state to be instantly collected.
    //
    // For now the state is not included as part of the closure.  It is too common a pattern (for example with socket receive)
    // to have several pending IOs differentiated by their state object.  We don't want that pattern to break the cache.
    //
    internal class CallbackClosure
    {
        private AsyncCallback _savedCallback;
        private ExecutionContext _savedContext;

        internal CallbackClosure(ExecutionContext context, AsyncCallback callback)
        {
            if (callback != null)
            {
                _savedCallback = callback;
                _savedContext = context;
            }
        }

        internal bool IsCompatible(AsyncCallback callback)
        {
            if (callback == null || _savedCallback == null)
            {
                return false;
            }

            // Delegates handle this ok.  AsyncCallback is sealed and immutable, so if this succeeds, we are safe to use
            // the passed-in instance.
            if (!object.Equals(_savedCallback, callback))
            {
                return false;
            }

            return true;
        }

        internal AsyncCallback AsyncCallback
        {
            get
            {
                return _savedCallback;
            }
        }

        internal ExecutionContext Context
        {
            get
            {
                return _savedContext;
            }
        }
    }

    //
    // This class will ensure that the correct context is restored on the thread before invoking
    // a user callback.
    //
    internal class ContextAwareResult : LazyAsyncResult
    {
        [Flags]
        private enum StateFlags
        {
            None = 0x00,
            CaptureIdentity = 0x01,
            CaptureContext = 0x02,
            ThreadSafeContextCopy = 0x04,
            PostBlockStarted = 0x08,
            PostBlockFinished = 0x10,
        }

        // This needs to be volatile so it's sure to make it over to the completion thread in time.
        private volatile ExecutionContext _Context;
        private object _Lock;
        private StateFlags _Flags;
        private WindowsIdentity _Wi;

        internal ContextAwareResult(object myObject, object myState, AsyncCallback myCallBack) :
            this(false, false, myObject, myState, myCallBack)
        { }

        // Setting captureIdentity enables the Identity property.  This will be available even if ContextCopy isn't, either because
        // flow is suppressed or it wasn't needed.  (If ContextCopy isn't available, Identity may or may not be.  But if it is, it
        // should be used instead of ContextCopy for impersonation - ContextCopy might not include the identity.)
        //
        // Setting forceCaptureContext enables the ContextCopy property even when a null callback is specified.  (The context is
        // always captured if a callback is given.)
        internal ContextAwareResult(bool captureIdentity, bool forceCaptureContext, object myObject, object myState, AsyncCallback myCallBack) :
            this(captureIdentity, forceCaptureContext, false, myObject, myState, myCallBack)
        { }

        internal ContextAwareResult(bool captureIdentity, bool forceCaptureContext, bool threadSafeContextCopy, object myObject, object myState, AsyncCallback myCallBack) :
            base(myObject, myState, myCallBack)
        {
            if (forceCaptureContext)
            {
                _Flags = StateFlags.CaptureContext;
            }

            if (captureIdentity)
            {
                _Flags |= StateFlags.CaptureIdentity;
            }

            if (threadSafeContextCopy)
            {
                _Flags |= StateFlags.ThreadSafeContextCopy;
            }
        }

        // Security: We need an assert for a call into WindowsIdentity.GetCurrent.
        private void SafeCaptureIdentity()
        {
            _Wi = WindowsIdentity.GetCurrent();
        }

        //
        // This can be used to establish a context during an async op for something like calling a delegate or demanding a permission.
        // May block briefly if the context is still being produced.
        //
        // Returns null if called from the posting thread.
        //
        internal ExecutionContext ContextCopy
        {
            get
            {
                GlobalLog.Assert(!InternalPeekCompleted || (_Flags & StateFlags.ThreadSafeContextCopy) != 0, "ContextAwareResult#{0}::ContextCopy|Called on completed result.", Logging.HashString(this));
                if (InternalPeekCompleted)
                {
                    throw new InvalidOperationException(SR.net_completed_result);
                }

                ExecutionContext context = _Context;
                if (context != null)
                {
                    return context.CreateCopy();
                }

                // Make sure the context was requested.
                GlobalLog.Assert(AsyncCallback != null || (_Flags & StateFlags.CaptureContext) != 0, "ContextAwareResult#{0}::ContextCopy|No context captured - specify a callback or forceCaptureContext.", Logging.HashString(this));

                // Just use the lock to block.  We might be on the thread that owns the lock which is great, it means we
                // don't need a context anyway.
                if ((_Flags & StateFlags.PostBlockFinished) == 0)
                {
                    GlobalLog.Assert(_Lock != null, "ContextAwareResult#{0}::ContextCopy|Must lock (StartPostingAsyncOp()) { ... FinishPostingAsyncOp(); } when calling ContextCopy (unless it's only called after FinishPostingAsyncOp).", Logging.HashString(this));
                    lock (_Lock) { }
                }

                GlobalLog.Assert(!InternalPeekCompleted || (_Flags & StateFlags.ThreadSafeContextCopy) != 0, "ContextAwareResult#{0}::ContextCopy|Result became completed during call.", Logging.HashString(this));
                if (InternalPeekCompleted)
                {
                    throw new InvalidOperationException(SR.net_completed_result);
                }

                context = _Context;
                return context == null ? null : context.CreateCopy();
            }
        }

        //
        // Just like ContextCopy.
        //
        internal WindowsIdentity Identity
        {
            get
            {
                GlobalLog.Assert(!InternalPeekCompleted || (_Flags & StateFlags.ThreadSafeContextCopy) != 0, "ContextAwareResult#{0}::Identity|Called on completed result.", Logging.HashString(this));
                if (InternalPeekCompleted)
                {
                    throw new InvalidOperationException(SR.net_completed_result);
                }

                if (_Wi != null)
                {
                    return _Wi;
                }

                // Make sure the identity was requested.
                GlobalLog.Assert((_Flags & StateFlags.CaptureIdentity) != 0, "ContextAwareResult#{0}::Identity|No identity captured - specify captureIdentity.", Logging.HashString(this));

                // Just use the lock to block.  We might be on the thread that owns the lock which is great, it means we
                // don't need an identity anyway.
                if ((_Flags & StateFlags.PostBlockFinished) == 0)
                {
                    GlobalLog.Assert(_Lock != null, "ContextAwareResult#{0}::Identity|Must lock (StartPostingAsyncOp()) { ... FinishPostingAsyncOp(); } when calling Identity (unless it's only called after FinishPostingAsyncOp).", Logging.HashString(this));
                    lock (_Lock) { }
                }

                GlobalLog.Assert(!InternalPeekCompleted || (_Flags & StateFlags.ThreadSafeContextCopy) != 0, "ContextAwareResult#{0}::Identity|Result became completed during call.", Logging.HashString(this));
                if (InternalPeekCompleted)
                {
                    throw new InvalidOperationException(SR.net_completed_result);
                }

                return _Wi;
            }
        }

#if DEBUG
        // Want to be able to verify that the Identity was requested.  If it was requested but isn't available
        // on the Identity property, it's either available via ContextCopy or wasn't needed (synchronous).
        internal bool IdentityRequested
        {
            get
            {
                return (_Flags & StateFlags.CaptureIdentity) != 0;
            }
        }
#endif

        internal object StartPostingAsyncOp()
        {
            return StartPostingAsyncOp(true);
        }

        //
        // If ContextCopy or Identity will be used, the return value should be locked until FinishPostingAsyncOp() is called
        // or the operation has been aborted (e.g. by BeginXxx throwing).  Otherwise, this can be called with false to prevent the lock
        // object from being created.
        //
        internal object StartPostingAsyncOp(bool lockCapture)
        {
            GlobalLog.Assert(!InternalPeekCompleted, "ContextAwareResult#{0}::StartPostingAsyncOp|Called on completed result.", Logging.HashString(this));

            DebugProtectState(true);

            _Lock = lockCapture ? new object() : null;
            _Flags |= StateFlags.PostBlockStarted;
            return _Lock;
        }

        //
        // Call this when returning control to the user.
        //
        internal bool FinishPostingAsyncOp()
        {
            // Ignore this call if StartPostingAsyncOp() failed or wasn't called, or this has already been called.
            if ((_Flags & (StateFlags.PostBlockStarted | StateFlags.PostBlockFinished)) != StateFlags.PostBlockStarted)
            {
                return false;
            }

            _Flags |= StateFlags.PostBlockFinished;

            ExecutionContext cachedContext = null;
            return CaptureOrComplete(ref cachedContext, false);
        }

        //
        // Call this when returning control to the user.  Allows a cached Callback Closure to be supplied and used
        // as appropriate, and replaced with a new one.
        //
        internal bool FinishPostingAsyncOp(ref CallbackClosure closure)
        {
            // Ignore this call if StartPostingAsyncOp() failed or wasn't called, or this has already been called.
            if ((_Flags & (StateFlags.PostBlockStarted | StateFlags.PostBlockFinished)) != StateFlags.PostBlockStarted)
            {
                return false;
            }

            _Flags |= StateFlags.PostBlockFinished;

            // Need a copy of this ref argument since it can be used in many of these calls simultaneously.
            CallbackClosure closureCopy = closure;
            ExecutionContext cachedContext;
            if (closureCopy == null)
            {
                cachedContext = null;
            }
            else
            {
                if (!closureCopy.IsCompatible(AsyncCallback))
                {
                    // Clear the cache as soon as a method is called with incompatible parameters.
                    closure = null;
                    cachedContext = null;
                }
                else
                {
                    // If it succeeded, we want to replace our context/callback with the one from the closure.
                    // Using the closure's instance of the callback is probably overkill, but safer.
                    AsyncCallback = closureCopy.AsyncCallback;
                    cachedContext = closureCopy.Context;
                }
            }

            bool calledCallback = CaptureOrComplete(ref cachedContext, true);

            // Set up new cached context if we didn't use the previous one.
            if (closure == null && AsyncCallback != null && cachedContext != null)
            {
                closure = new CallbackClosure(cachedContext, AsyncCallback);
            }

            return calledCallback;
        }

        protected override void Cleanup()
        {
            base.Cleanup();

            GlobalLog.Print("ContextAwareResult#" + Logging.HashString(this) + "::Cleanup()");
            if (_Wi != null)
            {
                _Wi.Dispose();
                _Wi = null;
            }
        }

        //
        // This must be called right before returning the result to the user.  It might call the callback itself,
        // to avoid flowing context.  Even if the operation completes before this call, the callback won't have been
        // called.
        //
        // Returns whether the operation completed sync or not.
        //
        private bool CaptureOrComplete(ref ExecutionContext cachedContext, bool returnContext)
        {
            GlobalLog.Assert((_Flags & StateFlags.PostBlockStarted) != 0, "ContextAwareResult#{0}::CaptureOrComplete|Called without calling StartPostingAsyncOp.", Logging.HashString(this));

            // See if we're going to need to capture the context.
            bool capturingContext = AsyncCallback != null || (_Flags & StateFlags.CaptureContext) != 0;

            // Peek if we've already completed, but don't fix CompletedSynchronously yet
            // Capture the identity if requested, unless we're going to capture the context anyway, unless
            // capturing the context won't be sufficient.
            if ((_Flags & StateFlags.CaptureIdentity) != 0 && !InternalPeekCompleted && (!capturingContext))
            {
                GlobalLog.Print("ContextAwareResult#" + Logging.HashString(this) + "::CaptureOrComplete() starting identity capture");
                SafeCaptureIdentity();
            }

            // No need to flow if there's no callback, unless it's been specifically requested.
            // Note that Capture() can return null, for example if SuppressFlow() is in effect.
            if (capturingContext && !InternalPeekCompleted)
            {
                GlobalLog.Print("ContextAwareResult#" + Logging.HashString(this) + "::CaptureOrComplete() starting capture");
                if (cachedContext == null)
                {
                    cachedContext = ExecutionContext.Capture();
                }

                if (cachedContext != null)
                {
                    if (!returnContext)
                    {
                        _Context = cachedContext;
                        cachedContext = null;
                    }
                    else
                    {
                        _Context = cachedContext.CreateCopy();
                    }
                }
                GlobalLog.Print("ContextAwareResult#" + Logging.HashString(this) + "::CaptureOrComplete() _Context:" + Logging.HashString(_Context));
            }
            else
            {
                // Otherwise we have to have completed synchronously, or not needed the context.
                GlobalLog.Print("ContextAwareResult#" + Logging.HashString(this) + "::CaptureOrComplete() skipping capture");
                cachedContext = null;
                GlobalLog.Assert(AsyncCallback == null || CompletedSynchronously, "ContextAwareResult#{0}::CaptureOrComplete|Didn't capture context, but didn't complete synchronously!", Logging.HashString(this));
            }

            // Now we want to see for sure what to do.  We might have just captured the context for no reason.
            // This has to be the first time the state has been queried "for real" (apart from InvokeCallback)
            // to guarantee synchronization with Complete() (otherwise, Complete() could try to call the
            // callback without the context having been gotten).
            DebugProtectState(false);
            if (CompletedSynchronously)
            {
                GlobalLog.Print("ContextAwareResult#" + Logging.HashString(this) + "::CaptureOrComplete() completing synchronously");
                base.Complete(IntPtr.Zero);
                return true;
            }

            return false;
        }

        //
        // Is guaranteed to be called only once.  If called with a non-zero userToken, the context is not flowed.
        //
        protected override void Complete(IntPtr userToken)
        {
            GlobalLog.Print("ContextAwareResult#" + Logging.HashString(this) + "::Complete() _Context(set):" + (_Context != null).ToString() + " userToken:" + userToken.ToString());

            // If no flowing, just complete regularly.
            if ((_Flags & StateFlags.PostBlockStarted) == 0)
            {
                base.Complete(userToken);
                return;
            }

            // At this point, IsCompleted is set and CompletedSynchronously is fixed.  If it's synchronous, then we want to hold
            // the completion for the CaptureOrComplete() call to avoid the context flow.  If not, we know CaptureOrComplete() has completed.
            if (CompletedSynchronously)
            {
                return;
            }

            ExecutionContext context = _Context;

            // If the context is being abandoned or wasn't captured (SuppressFlow, null AsyncCallback), just
            // complete regularly, as long as CaptureOrComplete() has finished.
            // 
            if (userToken != IntPtr.Zero || context == null)
            {
                base.Complete(userToken);
                return;
            }

            ExecutionContext.Run((_Flags & StateFlags.ThreadSafeContextCopy) != 0 ? context.CreateCopy() : context,
                                 new ContextCallback(CompleteCallback), null);
        }

        private void CompleteCallback(object state)
        {
            GlobalLog.Print("ContextAwareResult#" + Logging.HashString(this) + "::CompleteCallback() Context set, calling callback.");
            base.Complete(IntPtr.Zero);
        }
    }
}
