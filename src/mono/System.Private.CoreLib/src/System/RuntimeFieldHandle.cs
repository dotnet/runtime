// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    public struct RuntimeFieldHandle : IEquatable<RuntimeFieldHandle>, ISerializable
    {
        private readonly IntPtr value;

        internal RuntimeFieldHandle(IntPtr v)
        {
            value = v;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
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

        public static RuntimeFieldHandle FromIntPtr(IntPtr value) => new RuntimeFieldHandle(value);

        public static IntPtr ToIntPtr(RuntimeFieldHandle value) => value.Value;

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

        internal static void SetValue(RuntimeFieldInfo field, object? obj, object? value, RuntimeType? _, FieldAttributes _1, RuntimeType? _2, ref bool _3)
        {
            SetValueInternal(field, obj, value);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe object GetValueDirect(RuntimeFieldInfo field, RuntimeType fieldType, void* pTypedRef, RuntimeType? contextType);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe void SetValueDirect(RuntimeFieldInfo field, RuntimeType fieldType, void* pTypedRef, object value, RuntimeType? contextType);
    }

}
