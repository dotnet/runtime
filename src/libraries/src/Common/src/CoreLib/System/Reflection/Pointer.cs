// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.Reflection
{
    [CLSCompliant(false)]
    public sealed unsafe class Pointer : ISerializable
    {
        // CoreCLR: Do not add or remove fields without updating the ReflectionPointer class in runtimehandles.h
        private readonly void* _ptr;
        private readonly Type _ptrType;

        private Pointer(void* ptr, Type ptrType)
        {
            Debug.Assert(ptrType.IsRuntimeImplemented()); // CoreCLR: For CoreRT's sake, _ptrType has to be declared as "Type", but in fact, it is always a RuntimeType. Code on CoreCLR expects this.
            _ptr = ptr;
            _ptrType = ptrType;
        }

        public static object Box(void* ptr, Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (!type.IsPointer)
                throw new ArgumentException(SR.Arg_MustBePointer, nameof(ptr));
            if (!type.IsRuntimeImplemented())
                throw new ArgumentException(SR.Arg_MustBeType, nameof(ptr));

            return new Pointer(ptr, type);
        }

        public static void* Unbox(object ptr)
        {
            if (!(ptr is Pointer))
                throw new ArgumentException(SR.Arg_MustBePointer, nameof(ptr));
            return ((Pointer)ptr)._ptr;
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        internal Type GetPointerType() => _ptrType;
        internal IntPtr GetPointerValue() => (IntPtr)_ptr;
    }
}
