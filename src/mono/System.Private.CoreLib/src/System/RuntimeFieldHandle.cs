// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    public struct RuntimeFieldHandle : IEquatable<RuntimeFieldHandle>, ISerializable
    {
        private readonly IntPtr value;

        internal RuntimeFieldHandle(IntPtr v)
        {
            value = v;
        }

        private RuntimeFieldHandle(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public IntPtr Value
        {
            get
            {
                return value;
            }
        }

        internal bool IsNullHandle()
        {
            return value == IntPtr.Zero;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return value == ((RuntimeFieldHandle)obj).Value;
        }

        public bool Equals(RuntimeFieldHandle handle)
        {
            return value == handle.Value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static bool operator ==(RuntimeFieldHandle left, RuntimeFieldHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeFieldHandle left, RuntimeFieldHandle right)
        {
            return !left.Equals(right);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void SetValueInternal(FieldInfo fi, object? obj, object? value);

        internal static void SetValue(RuntimeFieldInfo field, object? obj, object? value, RuntimeType? fieldType, FieldAttributes fieldAttr, RuntimeType? declaringType, ref bool domainInitialized)
        {
            SetValueInternal(field, obj, value);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe object GetValueDirect(RuntimeFieldInfo field, RuntimeType fieldType, void* pTypedRef, RuntimeType? contextType);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void SetValueDirect(RuntimeFieldInfo field, RuntimeType fieldType, void* pTypedRef, object value, RuntimeType? contextType);
    }

}
