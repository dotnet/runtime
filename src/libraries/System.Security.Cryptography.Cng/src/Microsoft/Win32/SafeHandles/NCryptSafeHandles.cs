// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using ErrorCode = Interop.NCrypt.ErrorCode;

namespace Microsoft.Win32.SafeHandles
{

    /// <summary>
    ///     Base class for NCrypt handles which need to support being pseudo-duplicated. This class is not for
    ///     external use (instead applications should consume the concrete subclasses of this class).
    /// </summary>
    /// <remarks>
    ///     Since NCrypt handles do not have a native DuplicateHandle type call, we need to do manual 
    ///     reference counting in managed code whenever we hand out an extra reference to one of these handles.
    ///     This class wraps up the logic to correctly duplicate and free these handles to simulate a native
    ///     duplication.
    /// 
    ///     Each open handle object can be thought of as being in one of three states:
    ///        1. Owner     - created via the marshaler, traditional style safe handle. Notably, only one owner
    ///                       handle exists for a given native handle.
    ///        2. Duplicate - points at a handle in the Holder state. Releasing a handle in the duplicate state
    ///                       results only in decrementing the reference count of the holder, not in a release
    ///                       of the native handle.
    ///        3. Holder    - holds onto a native handle and is referenced by handles in the duplicate state.
    ///                       When all duplicate handles are closed, the holder handle releases the native
    ///                       handle. A holder handle will never be finalized, since this results in a race
    ///                       between the finalizers of the duplicate handles and the holder handle. Instead,
    ///                       it relies upon all of the duplicate handles to be finalized and decrement the
    ///                       ref count to zero.  Instances of a holder handle should never be referenced by
    ///                       anything but a duplicate handle.
    /// </remarks>
    public abstract class SafeNCryptHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private enum OwnershipState
        {
            /// <summary>
            ///     The safe handle owns the native handle outright. This must be value 0, as this is the
            ///     state the marshaler will place the handle in when marshaling back a SafeHandle
            /// </summary>
            Owner = 0,

            /// <summary>
            ///     The safe handle does not own the native handle, but points to a Holder which does
            /// </summary>
            Duplicate,

            /// <summary>
            ///     The safe handle owns the native handle, but shares it with other Duplicate handles
            /// </summary>
            Holder
        }

        private OwnershipState _ownershipState;

        /// <summary>
        ///     If the handle is a Duplicate, this points at the safe handle which actually owns the native handle.
        /// </summary>
        private SafeNCryptHandle _holder;

        private SafeHandle _parentHandle;

        protected SafeNCryptHandle() : base(true)
        {
            return;
        }

        protected SafeNCryptHandle(IntPtr handle, SafeHandle parentHandle)
            : base(true)
        {
            if (parentHandle == null)
                throw new ArgumentNullException(nameof(parentHandle));
            if (parentHandle.IsClosed || parentHandle.IsInvalid)
                throw new ArgumentException(SR.Argument_Invalid_SafeHandleInvalidOrClosed, nameof(parentHandle));

            bool success = false;
            parentHandle.DangerousAddRef(ref success);
            _parentHandle = parentHandle;

            // Don't set the handle value until after parentHandle has been validated and persisted to a field,
            // otherwise Dispose will try to call the underlying Free function.
            SetHandle(handle);

            // But if this handle value IsInvalid then we'll never call ReleaseHandle, which leaves the parent open
            // forever.  Instead, release such a parent now.
            if (IsInvalid)
            {
                _parentHandle.DangerousRelease();
                _parentHandle = null;
            }
        }

        /// <summary>
        ///     Wrapper for the _holder field which ensures that we're in a consistent state
        /// </summary>
        private SafeNCryptHandle Holder
        {
            get
            {
                Debug.Assert((_ownershipState == OwnershipState.Duplicate && _holder != null) ||
                             (_ownershipState != OwnershipState.Duplicate && _holder == null));
                Debug.Assert(_holder == null || _holder._ownershipState == OwnershipState.Holder);

                return _holder;
            }

            set
            {
#if DEBUG
                Debug.Assert(value.IsValidOpenState);
#endif
                Debug.Assert(_ownershipState != OwnershipState.Duplicate);
                Debug.Assert(value._ownershipState == OwnershipState.Holder);


                _holder = value;
                _ownershipState = OwnershipState.Duplicate;
            }
        }

#if DEBUG
        /// <summary>
        ///     Ensure the state of the handle is consistent for an open handle
        /// </summary>
        private bool IsValidOpenState
        {
            get
            {
                switch (_ownershipState)
                {
                    // Owner handles do not have a holder
                    case OwnershipState.Owner:
                        return Holder == null && !IsInvalid && !IsClosed;

                    // Duplicate handles have valid open holders with the same raw handle value,
                    // and should not be tracking a distinct parent.
                    case OwnershipState.Duplicate:
                        bool acquiredHolder = false;

                        try
                        {
                            IntPtr holderRawHandle = IntPtr.Zero;

                            if (Holder != null)
                            {
                                Holder.DangerousAddRef(ref acquiredHolder);
                                holderRawHandle = Holder.DangerousGetHandle();
                            }


                            bool holderValid = Holder != null &&
                                               !Holder.IsInvalid &&
                                               !Holder.IsClosed &&
                                               holderRawHandle != IntPtr.Zero &&
                                               holderRawHandle == handle &&
                                               _parentHandle == null;

                            return holderValid && !IsInvalid && !IsClosed;
                        }
                        finally
                        {
                            if (acquiredHolder)
                            {
                                Holder.DangerousRelease();
                            }
                        }

                    // Holder handles do not have a holder
                    case OwnershipState.Holder:
                        return Holder == null && !IsInvalid && !IsClosed;

                    // Unknown ownership state
                    default:
                        return false;
                }
            }
        }
#endif

        /// <summary>
        ///     Duplicate a handle
        /// </summary>
        /// <remarks>
        ///     #NCryptHandleDuplicationAlgorithm
        /// 
        ///     Duplicating a handle performs different operations depending upon the state of the handle:
        /// 
        ///     * Owner     - Allocate two new handles, a holder and a duplicate.
        ///                 - Suppress finalization on the holder
        ///                 - Transition into the duplicate state
        ///                 - Use the new holder as the holder for both this handle and the duplicate
        ///                 - Increment the reference count on the holder
        /// 
        ///     * Duplicate - Allocate a duplicate handle
        ///                 - Increment the reference count of our holder
        ///                 - Assign the duplicate's holder to be our holder
        /// 
        ///     * Holder    - Specifically disallowed. Holders should only ever be referenced by duplicates,
        ///                   so duplication will occur on the duplicate rather than the holder handle.
        /// </remarks>
        internal T Duplicate<T>() where T : SafeNCryptHandle, new()
        {
#if DEBUG
            Debug.Assert(IsValidOpenState);
#endif
            Debug.Assert(_ownershipState != OwnershipState.Holder);
            Debug.Assert(typeof(T) == this.GetType());

            if (_ownershipState == OwnershipState.Owner)
            {
                return DuplicateOwnerHandle<T>();
            }
            else
            {
                // If we're not an owner handle, and we're being duplicated then we must be a duplicate handle.
                return DuplicateDuplicatedHandle<T>();
            }
        }

        /// <summary>
        ///     Duplicate a safe handle which is already duplicated.
        /// 
        ///     See code:Microsoft.Win32.SafeHandles.SafeNCryptHandle#NCryptHandleDuplicationAlgorithm for
        ///     details about the algorithm.
        /// </summary>
        private T DuplicateDuplicatedHandle<T>() where T : SafeNCryptHandle, new()
        {
#if DEBUG
            Debug.Assert(IsValidOpenState);
#endif
            Debug.Assert(_ownershipState == OwnershipState.Duplicate);
            Debug.Assert(typeof(T) == this.GetType());

            bool addedRef = false;
            T duplicate = new T();

            // We need to do this operation in a CER, since we need to make sure that if the AddRef occurs
            // that the duplicated handle will always point back to the Holder, otherwise the Holder will leak
            // since it will never have its ref count reduced to zero.
            try { }
            finally
            {
                Holder.DangerousAddRef(ref addedRef);
                duplicate.SetHandle(Holder.DangerousGetHandle());
                duplicate.Holder = Holder;              // Transitions to OwnershipState.Duplicate
            }

            return duplicate;
        }

        /// <summary>
        ///     Duplicate a safe handle which is currently the exclusive owner of a native handle
        /// 
        ///     See code:Microsoft.Win32.SafeHandles.SafeNCryptHandle#NCryptHandleDuplicationAlgorithm for
        ///     details about the algorithm.
        /// </summary>
        private T DuplicateOwnerHandle<T>() where T : SafeNCryptHandle, new()
        {
#if DEBUG
            Debug.Assert(IsValidOpenState);
#endif
            Debug.Assert(_ownershipState == OwnershipState.Owner);
            Debug.Assert(typeof(T) == this.GetType());

            bool addRef = false;

            T holder = new T();
            T duplicate = new T();

            // We need to do this operation in a CER in order to ensure that everybody's state stays consistent
            // with the current view of the world.  If the state of the various handles gets out of sync, then
            // we'll end up leaking since reference counts will not be set up properly.
            try { }
            finally
            {
                // Setup a holder safe handle to ref count the native handle
                holder._ownershipState = OwnershipState.Holder;
                holder.SetHandle(DangerousGetHandle());
                GC.SuppressFinalize(holder);


                // Move the parent handle to the Holder
                if (_parentHandle != null)
                {
                    holder._parentHandle = _parentHandle;
                    _parentHandle = null;
                }

                // Transition into the duplicate state, referencing the holder. The initial reference count
                // on the holder will refer to the original handle so there is no need to AddRef here.
                Holder = holder;        // Transitions to OwnershipState.Duplicate

                // The duplicate handle will also reference the holder
                holder.DangerousAddRef(ref addRef);
                duplicate.SetHandle(holder.DangerousGetHandle());
                duplicate.Holder = holder;  // Transitions to OwnershipState.Duplicate
            }

            return duplicate;
        }

        /// <summary>
        ///     Release the handle
        /// </summary>
        /// <remarks>
        ///     Similar to duplication, releasing a handle performs different operations based upon the state
        ///     of the handle.
        /// 
        ///     An instance which was constructed with a parentHandle value will only call DangerousRelease on
        ///     the parentHandle object. Otherwise the behavior is dictated by the ownership state.
        /// 
        ///     * Owner     - Simply call the release P/Invoke method
        ///     * Duplicate - Decrement the reference count of the current holder
        ///     * Holder    - Call the release P/Invoke. Note that ReleaseHandle on a holder implies a reference
        ///                   count of zero.
        /// </remarks>
        protected override bool ReleaseHandle()
        {
            if (_ownershipState == OwnershipState.Duplicate)
            {
                Holder.DangerousRelease();
                return true;
            }
            else if (_parentHandle != null)
            {
                _parentHandle.DangerousRelease();
                return true;
            }
            else
            {
                return ReleaseNativeHandle();
            }
        }

        /// <summary>
        ///     Perform the actual release P/Invoke
        /// </summary>
        protected abstract bool ReleaseNativeHandle();

        /// <summary>
        ///     Since all NCrypt handles are released the same way, no sense in writing the same code three times.
        /// </summary>
        internal bool ReleaseNativeWithNCryptFreeObject()
        {
            ErrorCode errorCode = Interop.NCrypt.NCryptFreeObject(handle);
            bool success = (errorCode == ErrorCode.ERROR_SUCCESS);
            Debug.Assert(success);
            return success;
        }
    }

    /// <summary>
    ///     Safe handle representing an NCRYPT_KEY_HANDLE
    /// </summary>
    public sealed class SafeNCryptKeyHandle : SafeNCryptHandle
    {
        public SafeNCryptKeyHandle()
        {
        }

        public SafeNCryptKeyHandle(IntPtr handle, SafeHandle parentHandle)
            : base(handle, parentHandle)
        {
        }

        internal SafeNCryptKeyHandle Duplicate()
        {
            return Duplicate<SafeNCryptKeyHandle>();
        }

        protected override bool ReleaseNativeHandle()
        {
            return ReleaseNativeWithNCryptFreeObject();
        }
    }

    /// <summary>
    ///     Safe handle representing an NCRYPT_PROV_HANDLE
    /// </summary>
    public sealed class SafeNCryptProviderHandle : SafeNCryptHandle
    {
        internal SafeNCryptProviderHandle Duplicate()
        {
            return Duplicate<SafeNCryptProviderHandle>();
        }

        internal void SetHandleValue(IntPtr newHandleValue)
        {
            Debug.Assert(newHandleValue != IntPtr.Zero);
            Debug.Assert(!IsClosed);
            Debug.Assert(handle == IntPtr.Zero);

            SetHandle(newHandleValue);
        }

        protected override bool ReleaseNativeHandle()
        {
            return ReleaseNativeWithNCryptFreeObject();
        }
    }

    /// <summary>
    ///     Safe handle representing an NCRYPT_SECRET_HANDLE
    /// </summary>
    public sealed class SafeNCryptSecretHandle : SafeNCryptHandle
    {
        protected override bool ReleaseNativeHandle()
        {
            return ReleaseNativeWithNCryptFreeObject();
        }
    }
} 
