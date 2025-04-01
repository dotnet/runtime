// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Augments;
using Internal.Runtime;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RuntimeMethodHandle : IEquatable<RuntimeMethodHandle>, ISerializable
    {
        private IntPtr _value;

        private RuntimeMethodHandle(IntPtr value)
        {
            _value = value;
        }

        public IntPtr Value => _value;

        public override bool Equals(object? obj)
        {
            if (!(obj is RuntimeMethodHandle))
                return false;

            return Equals((RuntimeMethodHandle)obj);
        }

        public unsafe bool Equals(RuntimeMethodHandle handle)
        {
            if (_value == handle._value)
                return true;

            if (_value == IntPtr.Zero || handle._value == IntPtr.Zero)
                return false;

            MethodHandleInfo* thisInfo = ToMethodHandleInfo();
            MethodHandleInfo* thatInfo = handle.ToMethodHandleInfo();

            if (!thisInfo->DeclaringType.Equals(thatInfo->DeclaringType))
                return false;
            if (!thisInfo->Handle.Equals(thatInfo->Handle))
                return false;
            if (thisInfo->NumGenericArgs != thatInfo->NumGenericArgs)
                return false;

            RuntimeTypeHandle* thisFirstArg = &thisInfo->FirstArgument;
            RuntimeTypeHandle* thatFirstArg = &thatInfo->FirstArgument;
            for (int i = 0; i < thisInfo->NumGenericArgs; i++)
            {
                if (!thisFirstArg[i].Equals(thatFirstArg[i]))
                    return false;
            }

            return true;
        }

        public override unsafe int GetHashCode()
        {
            if (_value == IntPtr.Zero)
                return 0;

            MethodHandleInfo* info = ToMethodHandleInfo();

            int hashcode = info->DeclaringType.GetHashCode();
            hashcode = (hashcode + int.RotateLeft(hashcode, 13)) ^ info->Handle.GetHashCode();
            for (int i = 0; i < info->NumGenericArgs; i++)
            {
                int argumentHashCode = (&info->FirstArgument)[i].GetHashCode();
                hashcode = (hashcode + int.RotateLeft(hashcode, 13)) ^ argumentHashCode;
            }

            return hashcode;
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

        public unsafe IntPtr GetFunctionPointer()
        {
            if (_value == IntPtr.Zero)
                throw new ArgumentNullException(null, SR.Arg_InvalidHandle);

            return ReflectionAugments.GetFunctionPointer(this, ToMethodHandleInfo()->DeclaringType);
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly unsafe MethodHandleInfo* ToMethodHandleInfo()
        {
            return (MethodHandleInfo*)_value;
        }
    }

    [CLSCompliant(false)]
    [StructLayout(LayoutKind.Sequential)]
    public struct MethodHandleInfo
    {
        public RuntimeTypeHandle DeclaringType;
        public MethodHandle Handle;
        public int NumGenericArgs;
        public RuntimeTypeHandle FirstArgument;
    }
}
