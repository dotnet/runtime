// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// A marshaller for <see cref="SafeHandle"/>-derived types that marshals the handle following the lifetime rules for <see cref="SafeHandle"/>s.
    /// </summary>
    /// <typeparam name="T">The <see cref="SafeHandle"/>-derived type.</typeparam>
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.ManagedToUnmanagedIn, typeof(SafeHandleMarshaller<>.ManagedToUnmanagedIn))]
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.ManagedToUnmanagedRef, typeof(SafeHandleMarshaller<>.ManagedToUnmanagedRef))]
    [CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder), MarshalMode.ManagedToUnmanagedOut, typeof(SafeHandleMarshaller<>.ManagedToUnmanagedOut))]
    public static class SafeHandleMarshaller<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> where T : SafeHandle
    {
        /// <summary>
        /// Custom marshaller to marshal a <see cref="SafeHandle"/> as its underlying handle value.
        /// </summary>
        public struct ManagedToUnmanagedIn
        {
            private bool _addRefd;
            private T? _handle;

            /// <summary>
            /// Initializes the marshaller from a managed handle.
            /// </summary>
            /// <param name="handle">The managed handle.</param>
            public void FromManaged(T handle)
            {
                _handle = handle;
                handle.DangerousAddRef(ref _addRefd);
            }

            /// <summary>
            /// Get the unmanaged handle.
            /// </summary>
            /// <returns>The unmanaged handle.</returns>
            public IntPtr ToUnmanaged() => _handle!.DangerousGetHandle();

            /// <summary>
            /// Release any references keeping the managed handle alive.
            /// </summary>
            public void Free()
            {
                if (_addRefd)
                {
                    _handle!.DangerousRelease();
                }
            }
        }

        /// <summary>
        /// Custom marshaller to marshal a <see cref="SafeHandle"/> as its underlying handle value.
        /// </summary>
        public struct ManagedToUnmanagedRef
        {
            private bool _addRefd;
            private bool _callInvoked;
            private T? _handle;
            private IntPtr _originalHandleValue;
            private T _newHandle;
            private T? _handleToReturn;

            /// <summary>
            /// Create the marshaller in a default state.
            /// </summary>
            public ManagedToUnmanagedRef()
            {
                _addRefd = false;
                _callInvoked = false;
                // SafeHandle ref marshalling has always required parameterless constructors,
                // but it has never required them to be public.
                // We construct the handle now to ensure we don't cause an exception
                // before we are able to capture the unmanaged handle after the call.
                _newHandle = Activator.CreateInstance<T>()!;
            }

            /// <summary>
            /// Initialize the marshaller from a managed handle.
            /// </summary>
            /// <param name="handle">The managed handle</param>
            public void FromManaged(T handle)
            {
                _handle = handle;
                handle.DangerousAddRef(ref _addRefd);
                _originalHandleValue = handle.DangerousGetHandle();
            }

            /// <summary>
            /// Retrieve the unmanaged handle.
            /// </summary>
            /// <returns>The unmanaged handle</returns>
            public IntPtr ToUnmanaged() => _originalHandleValue;

            /// <summary>
            /// Initialize the marshaller from an unmanaged handle.
            /// </summary>
            /// <param name="value">The unmanaged handle.</param>
            public void FromUnmanaged(IntPtr value)
            {
                if (value == _originalHandleValue)
                {
                    _handleToReturn = _handle;
                }
                else
                {
                    Marshal.InitHandle(_newHandle, value);
                    _handleToReturn = _newHandle;
                }
            }

            /// <summary>
            /// Notify the marshaller that the native call has been invoked.
            /// </summary>
            public void OnInvoked()
            {
                _callInvoked = true;
            }

            /// <summary>
            /// Retrieve the managed handle from the marshaller.
            /// </summary>
            /// <returns>The managed handle.</returns>
            public T ToManagedFinally() => _handleToReturn!;

            /// <summary>
            /// Free any resources and reference counts owned by the marshaller.
            /// </summary>
            public void Free()
            {
                if (_addRefd)
                {
                    _handle!.DangerousRelease();
                }

                // If we never invoked the call, then we aren't going to use the
                // new handle. Dispose it now to avoid clogging up the finalizer queue
                // unnecessarily.
                if (!_callInvoked)
                {
                    _newHandle.Dispose();
                }
            }
        }

        /// <summary>
        /// Custom marshaller to marshal a <see cref="SafeHandle"/> as its underlying handle value.
        /// </summary>
        public struct ManagedToUnmanagedOut
        {
            private bool _initialized;
            private T _newHandle;

            /// <summary>
            /// Create the marshaller in a default state.
            /// </summary>
            public ManagedToUnmanagedOut()
            {
                _initialized = false;
                // SafeHandle out marshalling has always required parameterless constructors,
                // but it has never required them to be public.
                // We construct the handle now to ensure we don't cause an exception
                // before we are able to capture the unmanaged handle after the call.
                _newHandle = Activator.CreateInstance<T>()!;
            }

            /// <summary>
            /// Initialize the marshaller from an unmanaged handle.
            /// </summary>
            /// <param name="value">The unmanaged handle.</param>
            public void FromUnmanaged(IntPtr value)
            {
                _initialized = true;
                Marshal.InitHandle(_newHandle, value);
            }

            /// <summary>
            /// Retrieve the managed handle from the marshaller.
            /// </summary>
            /// <returns>The managed handle.</returns>
            public T ToManaged() => _newHandle;

            /// <summary>
            /// Free any resources and reference counts owned by the marshaller.
            /// </summary>
            public void Free()
            {
                // If we never captured the handle value, then we aren't going to use the
                // new handle. Dispose it now to avoid clogging up the finalizer queue
                // unnecessarily.
                if (!_initialized)
                {
                    _newHandle!.Dispose();
                }
            }
        }
    }
}
