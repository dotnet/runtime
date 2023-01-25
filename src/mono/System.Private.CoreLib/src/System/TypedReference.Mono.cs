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

        public static unsafe object? ToObject(TypedReference value)
        {
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('TypedReference')
            return InternalToObject(&value);
#pragma warning restore CS8500
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern unsafe object InternalToObject(void* value);
    }
}
