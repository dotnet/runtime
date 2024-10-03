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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TypedReference MakeTypedReferenceInternal(object target, IntPtr[] fields, RuntimeType lastFieldType)
        {
            TypedReference result = default;
            unsafe
            {
                InternalMakeTypedReference(&result, target, fields, lastFieldType);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        // reference to TypedReference is banned, so have to pass result as pointer
        private static extern unsafe void InternalMakeTypedReference(void* result, object target, IntPtr[] flds, RuntimeType lastFieldType);

        public static unsafe object? ToObject(TypedReference value)
        {
            return InternalToObject(&value);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object InternalToObject(void* value);
    }
}
