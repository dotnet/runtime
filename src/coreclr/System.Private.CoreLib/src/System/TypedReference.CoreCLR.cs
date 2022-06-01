// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TypedReference is basically only ever seen on the call stack, and in param arrays.
// These are blob that must be dealt with by the compiler.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    [System.Runtime.Versioning.NonVersionable] // This only applies to field layout
    public ref partial struct TypedReference
    {
        private readonly ByReference<byte> _value;
        private readonly IntPtr _type;

        public static unsafe object? ToObject(TypedReference value)
        {
            TypeHandle typeHandle = new((void*)value._type);

            if (typeHandle.IsNull)
            {
                ThrowHelper.ThrowArgumentException_ArgumentNull_TypedRefType();
            }

            // The only case where a type handle here might be a type desc is when the type is either a
            // pointer or a function pointer. In those cases, just always return the method table pointer
            // for System.UIntPtr without inspecting the type desc any further. Otherwise, the type handle
            // is just wrapping a method table pointer, so return that directly with a reinterpret cast.
            MethodTable* pMethodTable = typeHandle.IsTypeDesc
                ? (MethodTable*)RuntimeTypeHandle.GetValueInternal(typeof(UIntPtr).TypeHandle)
                : typeHandle.AsMethodTable();

            Debug.Assert(pMethodTable is not null);

            object? result;

            if (pMethodTable->IsValueType)
            {
                result = RuntimeHelpers.Box(pMethodTable, ref value._value.Value);
            }
            else
            {
                result = Unsafe.As<byte, object>(ref value._value.Value);
            }

            return result;
        }
    }
}
