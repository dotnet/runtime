// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

using Internal.Runtime.Augments;

namespace System
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    public struct RuntimeFieldHandle : ISerializable
    {
        private IntPtr _value;

        public IntPtr Value => _value;

        public override bool Equals(object? obj)
        {
            if (!(obj is RuntimeFieldHandle))
                return false;

            return Equals((RuntimeFieldHandle)obj);
        }

        public bool Equals(RuntimeFieldHandle handle)
        {
            if (_value == handle._value)
                return true;

            if (_value == IntPtr.Zero || handle._value == IntPtr.Zero)
                return false;

            string fieldName1, fieldName2;
            RuntimeTypeHandle declaringType1, declaringType2;

            RuntimeAugments.TypeLoaderCallbacks.GetRuntimeFieldHandleComponents(this, out declaringType1, out fieldName1);
            RuntimeAugments.TypeLoaderCallbacks.GetRuntimeFieldHandleComponents(handle, out declaringType2, out fieldName2);

            return declaringType1.Equals(declaringType2) && fieldName1 == fieldName2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int _rotl(int value, int shift)
        {
            return (int)(((uint)value << shift) | ((uint)value >> (32 - shift)));
        }

        public override int GetHashCode()
        {
            if (_value == IntPtr.Zero)
                return 0;

            string fieldName;
            RuntimeTypeHandle declaringType;
            RuntimeAugments.TypeLoaderCallbacks.GetRuntimeFieldHandleComponents(this, out declaringType, out fieldName);

            int hashcode = declaringType.GetHashCode();
            return (hashcode + _rotl(hashcode, 13)) ^ fieldName.GetHashCode();
        }

        public static bool operator ==(RuntimeFieldHandle left, RuntimeFieldHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeFieldHandle left, RuntimeFieldHandle right)
        {
            return !left.Equals(right);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
