// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace System.Net
{
    // This is used by ContextAwareResult to cache callback closures between similar calls.  Create one of these and
    // pass it in to FinishPostingAsyncOp() to prevent the context from being captured in every iteration of a looped async call.
    //
    // It was decided not to make the delegate and state into weak references because:
    //    - The delegate is very likely to be abandoned by the user right after calling BeginXxx, making caching it useless. There's
    //         no easy way to weakly reference just the target.
    //    - We want to support identifying state via object.Equals() (especially value types), which means we need to keep a
    //         reference to the original.  Plus, if we're holding the target, might as well hold the state too.
    // The user will need to disable caching if they want their target/state to be instantly collected.
    //
    // For now the state is not included as part of the closure.  It is too common a pattern (for example with socket receive)
    // to have several pending IOs differentiated by their state object.  We don't want that pattern to break the cache.
    internal sealed class CallbackClosure
    {
        private readonly AsyncCallback? _savedCallback;
        private readonly ExecutionContext? _savedContext;

        internal CallbackClosure(ExecutionContext context, AsyncCallback? callback)
        {
            if (callback != null)
            {
                _savedCallback = callback;
                _savedContext = context;
            }
        }

        internal bool IsCompatible(AsyncCallback? callback)
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

        internal AsyncCallback? AsyncCallback
        {
            get
            {
                return _savedCallback;
            }
        }

        internal ExecutionContext? Context
        {
            get
            {
                return _savedContext;
            }
        }
    }

    // This class will ensure that the correct context is restored on the thread before invoking
    // a user callback.
    internal partial class ContextAwareResult : LazyAsyncResult
    {
        [Flags]
        private enum StateFlags : byte
        {
            None = 0x00,
            CaptureIdentity = 0x01,
            CaptureContext = 0x02,
            ThreadSafeContextCopy = 0x04,
            PostBlockStarted = 0x08,
            PostBlockFinished = 0x10,
        }

        // This needs to be volatile so it's sure to make it over to the completion thread in time.
        private volatile ExecutionContext? _context;
        private object? _lock;
        private StateFlags _flags;


        internal ContextAwareResult(object myObject, object? myState, AsyncCallback? myCallBack) :
            this(false, false, myObject, myState, myCallBack)
        { }

        // Setting captureIdentity enables the Identity property.  This will be available even if ContextCopy isn't, either because
        // flow is suppressed or it wasn't needed.  (If ContextCopy isn't available, Identity may or may not be.  But if it is, it
        // should be used instead of ContextCopy for impersonation - ContextCopy might not include the identity.)
        //
        // Setting forceCaptureContext enables the ContextCopy property even when a null callback is specified.  (The context is
        // always captured if a callback is given.)
        internal ContextAwareResult(bool captureIdentity, bool forceCaptureContext, object? myObject, object? myState, AsyncCallback? myCallBack) :
            this(captureIdentity, forceCaptureContext, false, myObject, myState, myCallBack)
        { }

        internal ContextAwareResult(bool captureIdentity, bool forceCaptureContext, bool threadSafeContextCopy, object? myObject, object? myState, AsyncCallback? myCallBack) :
            base(myObject, myState, myCallBack)
        {
            if (forceCaptureContext)
            {
                _flags = StateFlags.CaptureContext;
            }

            if (captureIdentity)
            {
                _flags |= StateFlags.CaptureIdentity;
            }

            if (threadSafeContextCopy)
            {
                _flags |= StateFlags.ThreadSafeContextCopy;
            }
        }

        // This can be used to establish a context during an async op for something like calling a delegate or demanding a permission.
        // May block briefly if the context is still being produced.
        //
        // Returns null if called from the posting thread.
        internal ExecutionContext? ContextCopy
        {
            get
            {
                if (InternalPeekCompleted)
                {
                    Debug.Assert((_flags & StateFlags.ThreadSafeContextCopy) != 0, "Called on completed result.");
                    throw new InvalidOperationException(SR.net_completed_result);
                }

                ExecutionContext? context = _context;
                if (context != null)
                {
                    return context; // No need to copy on CoreCLR; ExecutionContext is immutable
                }

                // Make sure the context was requested.
                Debug.Assert(AsyncCallback != null || (_flags & StateFlags.CaptureContext) != 0, "No context captured - specify a callback or forceCaptureContext.");

                // Just use the lock to block.  We might be on the thread that owns the lock which is great, it means we
                // don't need a context anyway.
                if ((_flags & StateFlags.PostBlockFinished) == 0)
                {
                    Debug.Assert(_lock != null, "Must lock (StartPostingAsyncOp()) { ... FinishPostingAsyncOp(); } when calling ContextCopy (unless it's only called after FinishPostingAsyncOp).");
                    lock (_lock) { }
                }

                if (InternalPeekCompleted)
                {
                    Debug.Assert((_flags & StateFlags.ThreadSafeContextCopy) != 0, "Result became completed during call.");
                    throw new InvalidOperationException(SR.net_completed_result);
                }

                return _context; // No need to copy on CoreCLR; ExecutionContext is immutable
            }
        }

#if DEBUG
        // Want to be able to verify that the Identity was requested.  If it was requested but isn't available
        // on the Identity property, it's either available via ContextCopy or wasn't needed (synchronous).
        internal bool IdentityRequested
        {
            get
            {
                return (_flags & StateFlags.CaptureIdentity) != 0;
            }
        }
#endif

        internal object StartPostingAsyncOp()
        {
            return StartPostingAsyncOp(true)!;
        }

        // If ContextCopy or Identity will be used, the return value should be locked until FinishPostingAsyncOp() is called
        // or the operation has been aborted (e.g. by BeginXxx throwing).  Otherwise, this can be called with false to prevent the lock
        // object from being created.
        internal object? StartPostingAsyncOp(bool lockCapture)
        {
            Debug.Assert(!InternalPeekCompleted, "Called on completed result.");
            DebugProtectState(true);

            _lock = lockCapture ? new object() : null;
            _flags |= StateFlags.PostBlockStarted;
            return _lock;
        }

        // Call this when returning control to the user.
        internal bool FinishPostingAsyncOp()
        {
            // Ignore this call if StartPostingAsyncOp() failed or wasn't called, or this has already been called.
            if ((_flags & (StateFlags.PostBlockStarted | StateFlags.PostBlockFinished)) != StateFlags.PostBlockStarted)
            {
                return false;
            }

            _flags |= StateFlags.PostBlockFinished;

            ExecutionContext? cachedContext = null;
            return CaptureOrComplete(ref cachedContext, false);
        }

        // Call this when returning control to the user.  Allows a cached Callback Closure to be supplied and used
        // as appropriate, and replaced with a new one.
        internal bool FinishPostingAsyncOp(ref CallbackClosure? closure)
        {
            // Ignore this call if StartPostingAsyncOp() failed or wasn't called, or this has already been called.
            if ((_flags & (StateFlags.PostBlockStarted | StateFlags.PostBlockFinished)) != StateFlags.PostBlockStarted)
            {
                return false;
            }

            _flags |= StateFlags.PostBlockFinished;

            // Need a copy of this ref argument since it can be used in many of these calls simultaneously.
            CallbackClosure? closureCopy = closure;
            ExecutionContext? cachedContext;
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
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this);
            CleanupInternal();
        }

        // This must be called right before returning the result to the user.  It might call the callback itself,
        // to avoid flowing context.  Even if the operation completes before this call, the callback won't have been
        // called.
        //
        // Returns whether the operation completed sync or not.
        private bool CaptureOrComplete(ref ExecutionContext? cachedContext, bool returnContext)
        {
            Debug.Assert((_flags & StateFlags.PostBlockStarted) != 0, "Called without calling StartPostingAsyncOp.");

            // See if we're going to need to capture the context.
            bool capturingContext = AsyncCallback != null || (_flags & StateFlags.CaptureContext) != 0;

            // Peek if we've already completed, but don't fix CompletedSynchronously yet
            // Capture the identity if requested, unless we're going to capture the context anyway, unless
            // capturing the context won't be sufficient.
            if ((_flags & StateFlags.CaptureIdentity) != 0 && !InternalPeekCompleted && (!capturingContext))
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "starting identity capture");
                SafeCaptureIdentity();
            }

            // No need to flow if there's no callback, unless it's been specifically requested.
            // Note that Capture() can return null, for example if SuppressFlow() is in effect.
            if (capturingContext && !InternalPeekCompleted)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "starting capture");

                if (cachedContext == null)
                {
                    cachedContext = ExecutionContext.Capture();
                }

                if (cachedContext != null)
                {
                    if (!returnContext)
                    {
                        _context = cachedContext;
                        cachedContext = null;
                    }
                    else
                    {
                        _context = cachedContext;
                    }
                }

                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"_context:{_context}");
            }
            else
            {
                // Otherwise we have to have completed synchronously, or not needed the context.
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Skipping capture");

                cachedContext = null;
                Debug.Assert(AsyncCallback == null || CompletedSynchronously, "Didn't capture context, but didn't complete synchronously!");
            }

            // Now we want to see for sure what to do.  We might have just captured the context for no reason.
            // This has to be the first time the state has been queried "for real" (apart from InvokeCallback)
            // to guarantee synchronization with Complete() (otherwise, Complete() could try to call the
            // callback without the context having been gotten).
            DebugProtectState(false);
            if (CompletedSynchronously)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Completing synchronously");
                base.Complete(IntPtr.Zero);
                return true;
            }

            return false;
        }

        // This method is guaranteed to be called only once.  If called with a non-zero userToken, the context is not flowed.
        protected override void Complete(IntPtr userToken)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"_context(set):{_context != null} userToken:{userToken}");

            // If no flowing, just complete regularly.
            if ((_flags & StateFlags.PostBlockStarted) == 0)
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

            ExecutionContext? context = _context;

            // If the context is being abandoned or wasn't captured (SuppressFlow, null AsyncCallback), just
            // complete regularly, as long as CaptureOrComplete() has finished.
            //
            if (userToken != IntPtr.Zero || context == null)
            {
                base.Complete(userToken);
                return;
            }

            ExecutionContext.Run(context, s => ((ContextAwareResult)s!).CompleteCallback(), this);
        }

        private void CompleteCallback()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, "Context set, calling callback.");
            base.Complete(IntPtr.Zero);
        }

        internal virtual EndPoint? RemoteEndPoint => null;
    }
}
