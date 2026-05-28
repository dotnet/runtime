// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices.Marshalling;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

/// <summary>
/// Represents a COM output parameter (<c>void**</c>) that may be null.
/// </summary>
/// <remarks>
/// Used for all COM interface output parameters in the cDAC legacy layer.
/// When the native caller passes <c>NULL</c> for the <c>void**</c>,
/// <see cref="IsNullRef"/> is <c>true</c> and setting <see cref="Interface"/>
/// throws <see cref="NullReferenceException"/> — mirroring what happens
/// when native code writes through a null pointer.
/// </remarks>
/// <typeparam name="T">The managed COM interface type.</typeparam>
[NativeMarshalling(typeof(DacComNullableByRefMarshaller<>))]
public sealed class DacComNullableByRef<T> where T : class
{
    private T? _interface;

    /// <summary>
    /// <c>true</c> when the native caller passed <c>NULL</c> for the output pointer.
    /// </summary>
    public bool IsNullRef { get; }

    /// <summary>
    /// The managed COM interface to return to the caller.
    /// Throws <see cref="NullReferenceException"/> when <see cref="IsNullRef"/> is <c>true</c>.
    /// </summary>
    public T? Interface
    {
        get => _interface;
        set
        {
            if (IsNullRef)
                throw new NullReferenceException();

            _interface = value;
        }
    }

    public DacComNullableByRef(bool isNullRef)
    {
        IsNullRef = isNullRef;
    }
}

/// <summary>
/// Stateful marshalers for <see cref="DacComNullableByRef{T}"/>.
/// Native type is <c>void**</c>.
/// </summary>
[CustomMarshaller(typeof(DacComNullableByRef<>), MarshalMode.UnmanagedToManagedIn, typeof(DacComNullableByRefMarshaller<>.UnmanagedToManaged))]
[CustomMarshaller(typeof(DacComNullableByRef<>), MarshalMode.ManagedToUnmanagedIn, typeof(DacComNullableByRefMarshaller<>.ManagedToUnmanaged))]
public static unsafe class DacComNullableByRefMarshaller<T> where T : class
{
    /// <summary>
    /// Marshals when native calls into managed: wraps the incoming <c>void**</c>,
    /// then writes back the result during cleanup.
    /// </summary>
    public struct UnmanagedToManaged
    {
        private void** _nativePtr;
        private DacComNullableByRef<T>? _managed;

        public void FromUnmanaged(void** nativePtr) => _nativePtr = nativePtr;

        public DacComNullableByRef<T> ToManaged()
        {
            _managed = new DacComNullableByRef<T>(isNullRef: _nativePtr is null);

            return _managed;
        }

        public void Free()
        {
            try
            {
                if (_nativePtr is not null)
                {
                    *_nativePtr = ComInterfaceMarshaller<T>.ConvertToUnmanaged(_managed?.Interface);
                }
            }
            catch
            {
                // Swallow exceptions to avoid crashing the process during marshalling.
                // The native caller will receive a null pointer if marshalling fails.
                Debug.Fail("Exception occurred while marshalling COM interface output parameter. The native caller will receive a null pointer.");
            }
        }
    }

    /// <summary>
    /// Passes a <c>void**</c> to a legacy native method, then reads back
    /// the COM pointer it wrote and stores it in the wrapper.
    /// </summary>
    public struct ManagedToUnmanaged
    {
        private DacComNullableByRef<T>? _managed;
        private nint _nativeResult;

        public void FromManaged(DacComNullableByRef<T>? managed)
        {
            _managed = managed;
            _nativeResult = 0;
        }

        public void** ToUnmanaged()
        {
            if (_managed is null || _managed.IsNullRef)
                return null;

            return (void**)System.Runtime.CompilerServices.Unsafe.AsPointer(ref _nativeResult);
        }

        public void OnInvoked()
        {
            if (_managed is not null && _nativeResult != 0)
            {
                _managed.Interface = ComInterfaceMarshaller<T>.ConvertToManaged((void*)_nativeResult);
            }
        }

        public void Free()
        {
            if (_nativeResult != 0)
            {
                ComInterfaceMarshaller<T>.Free((void*)_nativeResult);
            }
        }
    }
}
