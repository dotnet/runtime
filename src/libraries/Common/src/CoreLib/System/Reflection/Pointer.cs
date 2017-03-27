// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.Serialization;

namespace System.Reflection
{
    [Serializable]
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

        private Pointer(SerializationInfo info, StreamingContext context)
        {
            _ptr = ((IntPtr)(info.GetValue("_ptr", typeof(IntPtr)))).ToPointer();
            _ptrType = (Type)info.GetValue("_ptrType", typeof(Type));
            if (!_ptrType.IsRuntimeImplemented())
                throw new SerializationException(SR.Arg_MustBeType);
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
            info.AddValue("_ptr", new IntPtr(_ptr));
            info.AddValue("_ptrType", _ptrType);
        }

        internal Type GetPointerType() => _ptrType;
        internal object GetPointerValue() => (IntPtr)_ptr;
    }
}
