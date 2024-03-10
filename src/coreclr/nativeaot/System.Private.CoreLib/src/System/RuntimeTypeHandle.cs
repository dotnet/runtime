// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

using Internal.Runtime;
using Internal.Runtime.Augments;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RuntimeTypeHandle : IEquatable<RuntimeTypeHandle>, ISerializable
    {
        private IntPtr _value;

        internal unsafe RuntimeTypeHandle(MethodTable* pEEType)
            => _value = (IntPtr)pEEType;

        private RuntimeTypeHandle(IntPtr value)
            => _value = value;

        public override bool Equals(object? obj)
        {
            if (obj is RuntimeTypeHandle handle)
            {
                return Equals(handle);
            }
            return false;
        }

        public override unsafe int GetHashCode()
        {
            if (IsNull)
                return 0;

            return (int)this.ToMethodTable()->HashCode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RuntimeTypeHandle handle)
        {
            return _value == handle._value;
        }

        public static RuntimeTypeHandle FromIntPtr(IntPtr value) => new RuntimeTypeHandle(value);

        public static IntPtr ToIntPtr(RuntimeTypeHandle value) => value.Value;

        public static bool operator ==(object? left, RuntimeTypeHandle right)
        {
            if (left is RuntimeTypeHandle)
                return right.Equals((RuntimeTypeHandle)left);
            return false;
        }

        public static bool operator ==(RuntimeTypeHandle left, object? right)
        {
            if (right is RuntimeTypeHandle)
                return left.Equals((RuntimeTypeHandle)right);
            return false;
        }

        public static bool operator !=(object? left, RuntimeTypeHandle right)
        {
            if (left is RuntimeTypeHandle)
                return !right.Equals((RuntimeTypeHandle)left);
            return true;
        }

        public static bool operator !=(RuntimeTypeHandle left, object? right)
        {
            if (right is RuntimeTypeHandle)
                return !left.Equals((RuntimeTypeHandle)right);
            return true;
        }

        public IntPtr Value => _value;

        public ModuleHandle GetModuleHandle()
        {
            Type? type = Type.GetTypeFromHandle(this);
            if (type == null)
                return default(ModuleHandle);

            return type.Module.ModuleHandle;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal MethodTable* ToMethodTable()
        {
            return (MethodTable*)_value;
        }

        internal bool IsNull
        {
            get
            {
                return _value == new IntPtr(0);
            }
        }
    }
}
