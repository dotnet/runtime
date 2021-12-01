// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Threading;

namespace System.Runtime.InteropServices
{
    // This implementation does not employ critical execution regions and thus cannot
    // reliably guarantee handle release in the face of thread aborts.

    /// <summary>Represents a wrapper class for operating system handles.</summary>
    public abstract partial class SafeHandle : CriticalFinalizerObject, IDisposable
    {
        // IMPORTANT:
        // - Do not add or rearrange fields as the EE depends on this layout,
        //   as well as on the values of the StateBits flags.
        // - The EE may also perform the same operations using equivalent native
        //   code, so this managed code must not assume it is the only code
        //   manipulating _state.

        /// <summary>Specifies the handle to be wrapped.</summary>
        protected IntPtr handle;
        /// <summary>Combined ref count and closed/disposed flags (so we can atomically modify them).</summary>
        private volatile int _state;
        /// <summary>Whether we can release this handle.</summary>
        private readonly bool _ownsHandle;
        /// <summary>Whether constructor completed.</summary>
        private volatile bool _fullyInitialized;

        /// <summary>Bitmasks for the <see cref="_state"/> field.</summary>
        /// <remarks>
        /// The state field ends up looking like this:
        ///
        ///  31                                                        2  1   0
        /// +-----------------------------------------------------------+---+---+
        /// |                           Ref count                       | D | C |
        /// +-----------------------------------------------------------+---+---+
        ///
        /// Where D = 1 means a Dispose has been performed and C = 1 means the
        /// underlying handle has been (or will be shortly) released.
        /// </remarks>
        private static class StateBits
        {
            public const int Closed = 0b01;
            public const int Disposed = 0b10;
            public const int RefCount = unchecked(~0b11); // 2 bits reserved for closed/disposed; ref count gets 30 bits
            public const int RefCountOne = 1 << 2;
        }

        /// <summary>Creates a SafeHandle class.</summary>
        protected SafeHandle(IntPtr invalidHandleValue, bool ownsHandle)
        {
            handle = invalidHandleValue;
            _state = StateBits.RefCountOne; // Ref count 1 and not closed or disposed.
            _ownsHandle = ownsHandle;

            if (!ownsHandle)
            {
                GC.SuppressFinalize(this);
            }

            _fullyInitialized = true;
        }

#if !CORERT // CoreRT doesn't correctly support CriticalFinalizerObject
        ~SafeHandle()
        {
            if (_fullyInitialized)
            {
                Dispose(disposing: false);
            }
        }
#endif

        internal bool OwnsHandle => _ownsHandle;

        protected internal void SetHandle(IntPtr handle) => this.handle = handle;

        public IntPtr DangerousGetHandle() => handle;

        public bool IsClosed => (_state & StateBits.Closed) == StateBits.Closed;

        public abstract bool IsInvalid { get; }

        public void Close() => Dispose();

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            Debug.Assert(_fullyInitialized);
            InternalRelease(disposeOrFinalizeOperation: true);
        }

        public void SetHandleAsInvalid()
        {
            Debug.Assert(_fullyInitialized);

            // Set closed state (low order bit of the _state field).
            Interlocked.Or(ref _state, StateBits.Closed);

            GC.SuppressFinalize(this);
        }

        protected abstract bool ReleaseHandle();

        public void DangerousAddRef(ref bool success)
        {
            // To prevent handle recycling security attacks we must enforce the
            // following invariant: we cannot successfully AddRef a handle on which
            // we've committed to the process of releasing.

            // We ensure this by never AddRef'ing a handle that is marked closed and
            // never marking a handle as closed while the ref count is non-zero. For
            // this to be thread safe we must perform inspection/updates of the two
            // values as a single atomic operation. We achieve this by storing them both
            // in a single aligned int and modifying the entire state via interlocked
            // compare exchange operations.

            // Additionally we have to deal with the problem of the Dispose operation.
            // We must assume that this operation is directly exposed to untrusted
            // callers and that malicious callers will try and use what is basically a
            // Release call to decrement the ref count to zero and free the handle while
            // it's still in use (the other way a handle recycling attack can be
            // mounted). We combat this by allowing only one Dispose to operate against
            // a given safe handle (which balances the creation operation given that
            // Dispose suppresses finalization). We record the fact that a Dispose has
            // been requested in the same state field as the ref count and closed state.

            Debug.Assert(_fullyInitialized);

            // Might have to perform the following steps multiple times due to
            // interference from other AddRef's and Release's.
            int oldState, newState;
            do
            {
                // First step is to read the current handle state. We use this as a
                // basis to decide whether an AddRef is legal and, if so, to propose an
                // update predicated on the initial state (a conditional write).
                // Check for closed state.
                oldState = _state;
                if ((oldState & StateBits.Closed) != 0)
                {
                    throw new ObjectDisposedException(nameof(SafeHandle), SR.ObjectDisposed_SafeHandleClosed);
                }

                // Not closed, let's propose an update (to the ref count, just add
                // StateBits.RefCountOne to the state to effectively add 1 to the ref count).
                // Continue doing this until the update succeeds (because nobody
                // modifies the state field between the read and write operations) or
                // the state moves to closed.
                newState = oldState + StateBits.RefCountOne;
            } while (Interlocked.CompareExchange(ref _state, newState, oldState) != oldState);

            // If we got here we managed to update the ref count while the state
            // remained non closed. So we're done.
            success = true;
        }

        // Used by internal callers to avoid declaring a bool to pass by ref
        internal void DangerousAddRef()
        {
            bool success = false;
            DangerousAddRef(ref success);
        }

        public void DangerousRelease() => InternalRelease(disposeOrFinalizeOperation: false);

        private void InternalRelease(bool disposeOrFinalizeOperation)
        {
            Debug.Assert(_fullyInitialized || disposeOrFinalizeOperation);

            // See AddRef above for the design of the synchronization here. Basically we
            // will try to decrement the current ref count and, if that would take us to
            // zero refs, set the closed state on the handle as well.
            bool performRelease = false;

            // Might have to perform the following steps multiple times due to
            // interference from other AddRef's and Release's.
            int oldState, newState;
            do
            {
                // First step is to read the current handle state. We use this cached
                // value to predicate any modification we might decide to make to the
                // state).
                oldState = _state;

                // If this is a Dispose operation we have additional requirements (to
                // ensure that Dispose happens at most once as the comments in AddRef
                // detail). We must check that the dispose bit is not set in the old
                // state and, in the case of successful state update, leave the disposed
                // bit set. Silently do nothing if Dispose has already been called.
                if (disposeOrFinalizeOperation && ((oldState & StateBits.Disposed) != 0))
                {
                    return;
                }

                // We should never see a ref count of zero (that would imply we have
                // unbalanced AddRef and Releases). (We might see a closed state before
                // hitting zero though -- that can happen if SetHandleAsInvalid is
                // used).
                if ((oldState & StateBits.RefCount) == 0)
                {
                    throw new ObjectDisposedException(nameof(SafeHandle), SR.ObjectDisposed_SafeHandleClosed);
                }

                // If we're proposing a decrement to zero and the handle is not closed
                // and we own the handle then we need to release the handle upon a
                // successful state update. If so we need to check whether the handle is
                // currently invalid by asking the SafeHandle subclass. We must do this before
                // transitioning the handle to closed, however, since setting the closed
                // state will cause IsInvalid to always return true.
                performRelease = ((oldState & (StateBits.RefCount | StateBits.Closed)) == StateBits.RefCountOne) &&
                                 _ownsHandle &&
                                 !IsInvalid;

                // Attempt the update to the new state, fail and retry if the initial
                // state has been modified in the meantime. Decrement the ref count by
                // substracting StateBits.RefCountOne from the state then OR in the bits for
                // Dispose (if that's the reason for the Release) and closed (if the
                // initial ref count was 1).
                newState = oldState - StateBits.RefCountOne;
                if ((oldState & StateBits.RefCount) == StateBits.RefCountOne)
                {
                    newState |= StateBits.Closed;
                }
                if (disposeOrFinalizeOperation)
                {
                    newState |= StateBits.Disposed;
                }
            } while (Interlocked.CompareExchange(ref _state, newState, oldState) != oldState);

            // If we get here we successfully decremented the ref count. Additionally we
            // may have decremented it to zero and set the handle state as closed. In
            // this case (providng we own the handle) we will call the ReleaseHandle
            // method on the SafeHandle subclass.
            if (performRelease)
            {
                // Save last error from P/Invoke in case the implementation of ReleaseHandle
                // trashes it (important because this ReleaseHandle could occur implicitly
                // as part of unmarshaling another P/Invoke).
                int lastError = Marshal.GetLastPInvokeError();
                ReleaseHandle();
                Marshal.SetLastPInvokeError(lastError);
            }
        }
    }
}
