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
    /// <remarks>
    /// The <see cref="DependentHandle"/> type is not thread-safe, and consumers are responsible for ensuring that
    /// <see cref="Dispose"/> is not called concurrently with other APIs. Not doing so results in undefined behavior.
    /// <para>The <see cref="Target"/> and <see cref="Dependent"/> properties are instead thread-safe.</para>
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
            // no need to check for null result: nInitialize expected to throw OOM.
            _handle = InternalInitialize(target, dependent);
        }

        /// <summary>
        /// Gets a value indicating whether this handle has been allocated or not.
        /// </summary>
        public bool IsAllocated => (nint)_handle != 0;

        /// <summary>
        /// Gets the target object instance for the current handle.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsAllocated"/> is <see langword="false"/>.</exception>
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
        /// Atomically retrieves the values of both <see cref="Target"/> and <see cref="Dependent"/>, if available.
        /// That is, even if <see cref="Target"/> is concurrently set to <see langword="null"/>, calling this method
        /// will either return <see langword="null"/> for both target and dependent, or return both previous values.
        /// If <see cref="Target"/> and <see cref="Dependent"/> were used sequentially in this scenario instead, it's
        /// could be possible to sometimes successfully retrieve the previous target, but then fail to get the dependent.
        /// </summary>
        /// <returns>The values of <see cref="Target"/> and <see cref="Dependent"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsAllocated"/> is <see langword="false"/>.</exception>
        public (object? Target, object? Dependent) GetTargetAndDependent()
        {
            IntPtr handle = _handle;

            if ((nint)handle == 0)
            {
                ThrowHelper.ThrowInvalidOperationException();
            }

            object? target = InternalGetTargetAndDependent(handle, out object? dependent);

            return (target, dependent);
        }

        /// <summary>
        /// Stops tracking the target and dependent objects in the current <see cref="DependentHandle"/> instance. Once this method
        /// is invoked, calling other APIs is still allowed, but <see cref="Target"/> and <see cref="Dependent"/> will always just
        /// return <see langword="null"/>. Additionally, since the dependent instance will no longer be tracked, it will also
        /// immediately become eligible for collection if there are no other roots for it, even if the original target is still alive.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsAllocated"/> is <see langword="false"/>.</exception>
        /// <remarks>
        /// This method does not dispose the current <see cref="DependentHandle"/> instance, which means that after calling it will not
        /// affect the value of <see cref="IsAllocated"/>, and <see cref="Dispose"/> will still need to be called to release resources.
        /// </remarks>
        public void StopTracking()
        {
            IntPtr handle = _handle;

            if ((nint)handle == 0)
            {
                ThrowHelper.ThrowInvalidOperationException();
            }

            InternalSetTarget(handle, null);
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
        /// This method mirrors <see cref="GetTargetAndDependent"/>, but without the allocation check.
        /// The signature is also kept the same as the one for the internal call, to improve the codegen.
        /// Note that <paramref name="dependent"/> is required to be on the stack (or it might not be tracked).
        /// </remarks>
        internal object? UnsafeGetTargetAndDependent(out object? dependent)
        {
            return InternalGetTargetAndDependent(_handle, out dependent);
        }

        /// <summary>
        /// Sets the dependent object instance for the current handle.
        /// </summary>
        /// <remarks>This method mirrors <see cref="Dependent"/>, but without the allocation check.</remarks>
        internal void UnsafeSetDependent(object? dependent)
        {
            InternalSetDependent(_handle, dependent);
        }

        /// <summary>
        /// Stops tracking the target and dependent objects in the current <see cref="DependentHandle"/> instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="IsAllocated"/> is <see langword="false"/>.</exception>
        /// <remarks>This method mirrors <see cref="StopTracking"/>, but without the allocation check.</remarks>
        internal void UnsafeStopTracking()
        {
            InternalSetTarget(_handle, null);
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
        private static extern void InternalSetTarget(IntPtr dependentHandle, object? target);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalSetDependent(IntPtr dependentHandle, object? dependent);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalFree(IntPtr dependentHandle);
    }
}
