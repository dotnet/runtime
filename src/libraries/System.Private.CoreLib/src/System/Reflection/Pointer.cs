// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Reflection
{
    [CLSCompliant(false)]
    public sealed unsafe class Pointer : ISerializable
    {
        // CoreCLR: Do not add or remove fields without updating the ReflectionPointer class in runtimehandles.h
        private readonly void* _ptr;
        private readonly RuntimeType _ptrType;

        private Pointer(void* ptr, RuntimeType ptrType)
        {
            _ptr = ptr;
            _ptrType = ptrType;
        }

        public static object Box(void* ptr, Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (!type.IsPointer)
                throw new ArgumentException(SR.Arg_MustBePointer, nameof(ptr));
            if (type is not RuntimeType rtType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            return new Pointer(ptr, rtType);
        }

        public static void* Unbox(object ptr)
        {
            if (!(ptr is Pointer))
                throw new ArgumentException(SR.Arg_MustBePointer, nameof(ptr));
            return ((Pointer)ptr)._ptr;
        }

        public override unsafe bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is Pointer pointer)
            {
                return _ptr == pointer._ptr;
            }

            return false;
        }

        public override unsafe int GetHashCode() => ((nuint)_ptr).GetHashCode();

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        internal RuntimeType GetPointerType() => _ptrType;
        internal IntPtr GetPointerValue() => (IntPtr)_ptr;
    }
}
