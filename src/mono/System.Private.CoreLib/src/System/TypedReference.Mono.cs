// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
    public ref partial struct TypedReference
    {
        #region sync with object-internals.h
        #pragma warning disable CA1823 // used by runtime
        private readonly RuntimeTypeHandle type;
        private readonly ref byte _value;
        private readonly IntPtr _type;
        #pragma warning restore CA1823
        #endregion

        private TypedReference(ref byte target, RuntimeType lastFieldType)
        {
            type = new RuntimeTypeHandle(lastFieldType);
            _value = ref target;
            _type = lastFieldType.GetUnderlyingNativeHandle();
        }

        private static ref byte GetFieldDataReference(object target, RuntimeFieldInfo field)
            => ref InternalGetFieldDataReference(target, field.FieldHandle.Value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ref byte InternalGetFieldDataReference(object target, IntPtr field);

        private static int GetFieldOffset(RuntimeFieldInfo field)
            => field.GetFieldOffset();

        public static unsafe object? ToObject(TypedReference value)
            => InternalToObject(&value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object InternalToObject(void* value);
    }
}
