// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
#if !DEBUG
using Internal.Runtime.CompilerServices;
#endif

namespace System.Runtime
{
    /// <summary>
    /// Represents a dependent GC handle, which will conditionally keep a dependent object instance alive
    /// as long as a target object instance is alive as well, without representing a strong reference to the
    /// target object instance. That is, a <see cref="DependentHandle"/> value with a given object instance as
    /// target will not cause the target to be kept alive if there are no other strong references to it, but
    /// it will do so for the dependent object instance as long as the target is alive.
    /// <para>
    /// This type is conceptually equivalent to having a weak reference to a given target object instance A, with
    /// that object having a field or property (or some other strong reference) to a dependent object instance B.
    /// </para>
    /// </summary>
    public struct DependentHandle : IDisposable
    {
        // =========================================================================================
        // This struct collects all operations on native DependentHandles. The DependentHandle
        // merely wraps an IntPtr so this struct serves mainly as a "managed typedef."
        //
        // DependentHandles exist in one of two states:
        //
        //    IsAllocated == false
        //        No actual handle is allocated underneath. Illegal to get Target, Dependent
        //        or GetTargetAndDependent(). Ok to call Dispose().
        //
        //        Initializing a DependentHandle using the nullary ctor creates a DependentHandle
        //        that's in the !IsAllocated state.
        //        (! Right now, we get this guarantee for free because (IntPtr)0 == NULL unmanaged handle.
        //         ! If that assertion ever becomes false, we'll have to add an _isAllocated field
        //         ! to compensate.)
        //
        //
        //    IsAllocated == true
        //        There's a handle allocated underneath. You must call Dispose() on this eventually
        //        or you cause a native handle table leak.
        //
        // This struct intentionally does no self-synchronization. It's up to the caller to
        // to use DependentHandles in a thread-safe way.
        // =========================================================================================

        private IntPtr _handle;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependentHandle"/> struct with the specified arguments.
        /// </summary>
        /// <param name="target">The target object instance to track.</param>
        /// <param name="dependent">The dependent object instance to associate with <paramref name="target"/>.</param>
        public DependentHandle(object? target, object? dependent)
        {
            // no need to check for null result: nInitialize expected to throw OOM.
            _handle = InternalInitialize(target, dependent);
        }

        /// <summary>
        /// Gets a value indicating whether this handle has been allocated or not.
        /// </summary>
        public bool IsAllocated => _handle != IntPtr.Zero;

        /// <summary>
        /// Gets or sets the target object instance for the current handle.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsAllocated"/> is <see langword="false"/>.</exception>
        public object? Target
        {
            get
            {
                IntPtr handle = _handle;

                if (handle == IntPtr.Zero)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                return InternalGetTarget(handle);
            }
            set
            {
                IntPtr handle = _handle;

                if (handle == IntPtr.Zero)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                InternalSetTarget(handle, value);
            }
        }

        /// <summary>
        /// Gets or sets the dependent object instance for the current handle.
        /// </summary>
        /// <remarks>
        /// If it is necessary to retrieve both <see cref="Target"/> and <see cref="Dependent"/>, it is
        /// recommended to use <see cref="GetTargetAndDependent"/> instead. This will result in better
        /// performance and it will reduce the chance of unexpected behavior in some cases.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsAllocated"/> is <see langword="false"/>.</exception>
        public object? Dependent
        {
            get
            {
                IntPtr handle = _handle;

                if (handle == IntPtr.Zero)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                return InternalGetDependent(handle);
            }
            set
            {
                IntPtr handle = _handle;

                if (handle == IntPtr.Zero)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                InternalSetDependent(handle, value);
            }
        }

        /// <summary>
        /// Retrieves the values of both <see cref="Target"/> and <see cref="Dependent"/>, if available.
        /// </summary>
        /// <returns>The values of <see cref="Target"/> and <see cref="Dependent"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsAllocated"/> is <see langword="false"/>.</exception>
        public (object? Target, object? Dependent) GetTargetAndDependent()
        {
            IntPtr handle = _handle;

            if (handle == IntPtr.Zero)
            {
                ThrowHelper.ThrowInvalidOperationException();
            }

            object? target = InternalGetTarget(handle);
            object? secondary = InternalGetDependent(handle);

            return (target, secondary);
        }

        /// <summary>
        /// Gets the target object instance for the current handle.
        /// </summary>
        /// <returns>The target object instance, if present.</returns>
        /// <remarks>This method mirrors <see cref="Target"/>, but without the allocation check.</remarks>
        internal object? UnsafeGetTarget()
        {
            return InternalGetTarget(_handle);
        }

        /// <summary>
        /// Gets the dependent object instance for the current handle.
        /// </summary>
        /// <returns>The dependent object instance, if present.</returns>
        /// <remarks>This method mirrors <see cref="Dependent"/>, but without the allocation check.</remarks>
        internal object? UnsafeGetDependent()
        {
            return InternalGetDependent(_handle);
        }

        /// <summary>
        /// Sets the target object instance for the current handle.
        /// </summary>
        /// <remarks>This method mirrors <see cref="Target"/>, but without the allocation check.</remarks>
        internal void UnsafeSetTarget(object? target)
        {
            InternalSetTarget(_handle, target);
        }

        /// <summary>
        /// Sets the dependent object instance for the current handle.
        /// </summary>
        /// <remarks>This method mirrors <see cref="Dependent"/>, but without the allocation check.</remarks>
        internal void UnsafeSetDependent(object? dependent)
        {
            InternalSetDependent(_handle, dependent);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public void Dispose()
        {
            // Forces dependentHandle back to non-allocated state (if not already there)
            // and frees the handle if needed.

            if (_handle != IntPtr.Zero)
            {
                IntPtr handle = _handle;
                _handle = IntPtr.Zero;
                InternalFree(handle);
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr InternalInitialize(object? target, object? dependent);

#if DEBUG
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object? InternalGetTarget(IntPtr dependentHandle);
#else
        private static unsafe object? InternalGetTarget(IntPtr dependentHandle)
        {
            // This optimization is the same that is used in GCHandle in RELEASE mode.
            // This is not used in DEBUG builds as the runtime performs additional checks.
            // The logic below is the inlined copy of ObjectFromHandle in the unmanaged runtime.
            return Unsafe.As<IntPtr, object>(ref *(IntPtr*)(nint)dependentHandle);
        }
#endif

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object? InternalGetDependent(IntPtr dependentHandle);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalSetTarget(IntPtr dependentHandle, object? target);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalSetDependent(IntPtr dependentHandle, object? dependent);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalFree(IntPtr dependentHandle);
    }
}
