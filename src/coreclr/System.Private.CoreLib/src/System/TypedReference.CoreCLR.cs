// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TypedReference is basically only ever seen on the call stack, and in param arrays.
// These are blob that must be dealt with by the compiler.

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

            MethodTable* pMethodTable = typeHandle.GetMethodTable();

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
