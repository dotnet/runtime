// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Text;

namespace System
{
    public struct RuntimeMethodHandle : IEquatable<RuntimeMethodHandle>, ISerializable
    {
        private readonly IntPtr value;

        internal RuntimeMethodHandle(IntPtr v)
        {
            value = v;
        }

        public IntPtr Value => value;

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr GetFunctionPointer(IntPtr m);

        public IntPtr GetFunctionPointer()
        {
            return GetFunctionPointer(value);
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            return value == ((RuntimeMethodHandle)obj).Value;
        }

        public bool Equals(RuntimeMethodHandle handle)
        {
            return value == handle.Value;
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public static RuntimeMethodHandle FromIntPtr(IntPtr value) => new RuntimeMethodHandle(value);

        public static IntPtr ToIntPtr(RuntimeMethodHandle value) => value.Value;

        public static bool operator ==(RuntimeMethodHandle left, RuntimeMethodHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeMethodHandle left, RuntimeMethodHandle right)
        {
            return !left.Equals(right);
        }

        internal static string ConstructInstantiation(RuntimeMethodInfo method)
        {
            var sb = new StringBuilder();
            Type[]? gen_params = method.GetGenericArguments();
            sb.Append('[');
            for (int j = 0; j < gen_params.Length; j++)
            {
                if (j > 0)
                    sb.Append(',');
                sb.Append(gen_params[j].Name);
            }
            sb.Append(']');
            return sb.ToString();
        }

        internal bool IsNullHandle()
        {
            return value == IntPtr.Zero;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void ReboxFromNullable (object? src, ObjectHandleOnStack res);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void ReboxToNullable (object? src, QCallTypeHandle destNullableType, ObjectHandleOnStack res);

        internal static object ReboxFromNullable(object? src)
        {
            object? res = null;
            ReboxFromNullable(src, ObjectHandleOnStack.Create(ref res));
            return res!;
        }

        internal static object ReboxToNullable(object? src, RuntimeType destNullableType)
        {
            object? res = null;
            ReboxToNullable(src, new QCallTypeHandle(ref destNullableType), ObjectHandleOnStack.Create(ref res));
            return res!;
        }
    }
}
