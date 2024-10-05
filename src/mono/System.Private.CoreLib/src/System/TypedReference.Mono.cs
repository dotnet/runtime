// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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

        private static unsafe TypedReference MakeTypedReference(ref byte target, RuntimeType lastFieldType)
        {
            TypedReference typedRef = default;
            {
                InternalMakeTypedReference(&typedRef, ref target, lastFieldType._impl.Value);
            }
            return typedRef;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern ref byte GetFieldDataReference(object target, RuntimeFieldInfo field);

        private static int GetFieldOffset(RuntimeFieldInfo field)
            => field.GetFieldOffset();

        [MethodImpl(MethodImplOptions.InternalCall)]
        // reference to TypedReference is banned, so have to pass result as pointer
        private static extern unsafe void InternalMakeTypedReference(void* result, ref byte target, IntPtr lastFieldType);

        public static unsafe object? ToObject(TypedReference value)
        {
            return InternalToObject(&value);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object InternalToObject(void* value);
    }
}
