// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

using Internal.Metadata.NativeFormat;
using Internal.Runtime.Augments;

namespace System
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct RuntimeFieldHandle : IEquatable<RuntimeFieldHandle>, ISerializable
    {
        private IntPtr _value;

        private RuntimeFieldHandle(IntPtr value)
        {
            _value = value;
        }

        public IntPtr Value => _value;

        public override bool Equals(object? obj)
        {
            if (!(obj is RuntimeFieldHandle))
                return false;

            return Equals((RuntimeFieldHandle)obj);
        }

        public unsafe bool Equals(RuntimeFieldHandle handle)
        {
            if (_value == handle._value)
                return true;

            if (_value == IntPtr.Zero || handle._value == IntPtr.Zero)
                return false;

            FieldHandleInfo* thisInfo = ToFieldHandleInfo();
            FieldHandleInfo* thatInfo = handle.ToFieldHandleInfo();

            return thisInfo->DeclaringType.Equals(thatInfo->DeclaringType) && thisInfo->Handle.Equals(thatInfo->Handle);
        }

        public override unsafe int GetHashCode()
        {
            if (_value == IntPtr.Zero)
                return 0;

            FieldHandleInfo* info = ToFieldHandleInfo();

            int hashcode = info->DeclaringType.GetHashCode();
            return (hashcode + int.RotateLeft(hashcode, 13)) ^ info->Handle.GetHashCode();
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

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly unsafe FieldHandleInfo* ToFieldHandleInfo()
        {
            return (FieldHandleInfo*)_value;
        }
    }

    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct FieldHandleInfo
    {
        public RuntimeTypeHandle DeclaringType;
        public FieldHandle Handle;
    }
}
