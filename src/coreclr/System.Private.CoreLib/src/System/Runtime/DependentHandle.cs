// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
#if !DEBUG
using Internal.Runtime.CompilerServices;
#endif

namespace System.Runtime
{
    /// <summary>
    /// Represents a dependent GC handle, which will conditionally keep a dependent object instance alive as long as
    /// a target object instance is alive as well, without representing a strong reference to the target instance.
    /// </summary>
    /// <remarks>
    /// A <see cref="DependentHandle"/> value with a given object instance as target will not cause the target
    /// to be kept alive if there are no other strong references to it, but it will do so for the dependent
    /// object instance as long as the target is alive.
    /// <para>
    /// Using this type is conceptually equivalent to having a weak reference to a given target object instance A,
    /// with that object having a field or property (or some other strong reference) to a dependent object instance B.
    /// </para>
    /// <para>
    /// The <see cref="DependentHandle"/> type is not thread-safe, and consumers are responsible for ensuring that
    /// <see cref="Dispose"/> is not called concurrently with other APIs. Not doing so results in undefined behavior.
    /// </para>
    /// <para>
    /// The <see cref="IsAllocated"/>, <see cref="Target"/>, <see cref="Dependent"/> and <see cref="TargetAndDependent"/>
    /// properties are instead thread-safe, and safe to use if <see cref="Dispose"/> is not concurrently invoked as well.
    /// </para>
    /// </remarks>
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
            // no need to check for null result: InternalInitialize expected to throw OOM.
            _handle = InternalInitialize(target, dependent);
        }

        /// <summary>
        /// Gets a value indicating whether this instance was constructed with
        /// <see cref="DependentHandle(object?, object?)"/> and has not yet been disposed.
        /// </summary>
        /// <remarks>This property is thread-safe.</remarks>
        public bool IsAllocated => (nint)_handle != 0;

        /// <summary>
        /// Gets or sets the target object instance for the current handle. The target can only be set to a <see langword="null"/> value
        /// once the <see cref="DependentHandle"/> instance has been created. Doing so will cause <see cref="Dependent"/> to start
        /// returning <see langword="null"/> as well, and to become eligible for collection even if the previous target is still alive.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if <see cref="IsAllocated"/> is <see langword="false"/> or if the input value is not <see langword="null"/>.</exception>
        /// <remarks>This property is thread-safe.</remarks>
        public object? Target
        {
            get
            {
                IntPtr handle = _handle;

                if ((nint)handle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                return InternalGetTarget(handle);
            }
            set
            {
                IntPtr handle = _handle;

                if ((nint)handle == 0 || value is not null)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                InternalSetTargetToNull(handle);
            }
        }

        /// <summary>
        /// Gets or sets the dependent object instance for the current handle.
        /// </summary>
        /// <remarks>
        /// If it is needed to retrieve both <see cref="Target"/> and <see cref="Dependent"/>, it is necessary
        /// to ensure that the returned instance from <see cref="Target"/> will be kept alive until <see cref="Dependent"/>
        /// is retrieved as well, or it might be collected and result in unexpected behavior. This can be done by storing the
        /// target in a local and calling <see cref="GC.KeepAlive(object)"/> on it after <see cref="Dependent"/> is accessed.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsAllocated"/> is <see langword="false"/>.</exception>
        /// <remarks>This property is thread-safe.</remarks>
        public object? Dependent
        {
            get
            {
                IntPtr handle = _handle;

                if ((nint)handle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                return InternalGetDependent(handle);
            }
            set
            {
                IntPtr handle = _handle;

                if ((nint)handle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                InternalSetDependent(handle, value);
            }
        }

        /// <summary>
        /// Gets the values of both <see cref="Target"/> and <see cref="Dependent"/> (if available) as an atomic operation.
        /// That is, even if <see cref="Target"/> is concurrently set to <see langword="null"/>, calling this method
        /// will either return <see langword="null"/> for both target and dependent, or return both previous values.
        /// If <see cref="Target"/> and <see cref="Dependent"/> were used sequentially in this scenario instead, it
        /// would be possible to sometimes successfully retrieve the previous target, but then fail to get the dependent.
        /// </summary>
        /// <returns>The values of <see cref="Target"/> and <see cref="Dependent"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsAllocated"/> is <see langword="false"/>.</exception>
        /// <remarks>This property is thread-safe.</remarks>
        public (object? Target, object? Dependent) TargetAndDependent
        {
            get
            {
                IntPtr handle = _handle;

                if ((nint)handle == 0)
                {
                    ThrowHelper.ThrowInvalidOperationException();
                }

                object? target = InternalGetTargetAndDependent(handle, out object? dependent);

                return (target, dependent);
            }
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
        /// Atomically retrieves the values of both <see cref="Target"/> and <see cref="Dependent"/>, if available.
        /// </summary>
        /// <param name="dependent">The dependent instance, if available.</param>
        /// <returns>The values of <see cref="Target"/> and <see cref="Dependent"/>.</returns>
        /// <remarks>
        /// This method mirrors the <see cref="TargetAndDependent"/> property, but without the allocation check.
        /// The signature is also kept the same as the one for the internal call, to improve the codegen.
        /// Note that <paramref name="dependent"/> is required to be on the stack (or it might not be tracked).
        /// </remarks>
        internal object? UnsafeGetTargetAndDependent(out object? dependent)
        {
            return InternalGetTargetAndDependent(_handle, out dependent);
        }

        /// <summary>
        /// Sets the dependent object instance for the current handle to <see langword="null"/>.
        /// </summary>
        /// <remarks>This method mirrors the <see cref="Target"/> setter, but without allocation and input checks.</remarks>
        internal void UnsafeSetTargetToNull()
        {
            InternalSetTargetToNull(_handle);
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
        /// <remarks>This method is not thread-safe.</remarks>
        public void Dispose()
        {
            // Forces the DependentHandle back to non-allocated state
            // (if not already there) and frees the handle if needed.
            IntPtr handle = _handle;

            if ((nint)handle != 0)
            {
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
        private static extern object? InternalGetTargetAndDependent(IntPtr dependentHandle, out object? dependent);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalSetDependent(IntPtr dependentHandle, object? dependent);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalSetTargetToNull(IntPtr dependentHandle);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalFree(IntPtr dependentHandle);
    }
}
