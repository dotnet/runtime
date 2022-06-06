// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public abstract partial class Enum
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Enum_GetValuesAndNames")]
        private static partial void GetEnumValuesAndNames(QCallTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, Interop.BOOL getNames);

        private static unsafe object InternalBoxEnum(RuntimeType enumType, long value)
        {
            MethodTable* pMethodTable = (MethodTable*)enumType.m_handle;
            object obj = GetEnumInfoForActivation(enumType).CreateUninitializedInstance(enumType);
            ref byte dataRef = ref obj.GetRawData();

            switch (pMethodTable->GetNumInstanceFieldBytes())
            {
                case 1:
                    dataRef = (byte)value;
                    break;
                case 2:
                    Unsafe.As<byte, ushort>(ref dataRef) = (ushort)value;
                    break;
                case 4:
                    Unsafe.As<byte, uint>(ref dataRef) = (uint)value;
                    break;
                case 8:
                    Unsafe.As<byte, ulong>(ref dataRef) = (ulong)value;
                    break;
                default:
                    Debug.Fail("Unexpected enum size");
                    break;
            }

            GC.KeepAlive(enumType);

            return obj;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern CorElementType InternalGetCorElementType();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType InternalGetUnderlyingType(RuntimeType enumType);

        /// <summary>
        /// Gets an <see cref="EnumInfo"/> instance to use to create new boxed instances.
        /// </summary>
        /// <param name="enumType">The enum type to use.</param>
        /// <remarks>
        /// This is like <see cref="GetEnumInfo(RuntimeType, bool)"/>, but skips unnecessary initialization.
        /// </remarks>
        private static EnumInfo GetEnumInfoForActivation(RuntimeType enumType)
        {
            if (enumType.GenericCache is not EnumInfo entry)
            {
                enumType.GenericCache = entry = new EnumInfo();
            }

            return entry;
        }

        /// <summary>
        /// Gets an <see cref="EnumInfo"/> instance to use to retrieve values, names and reflection info.
        /// </summary>
        /// <param name="enumType">The enum type to use.</param>
        /// <param name="getNames">Whether or not to also retrieve the enum names.</param>
        private static EnumInfo GetEnumInfo(RuntimeType enumType, bool getNames = true)
        {
            if (enumType.GenericCache is not EnumInfo entry)
            {
                enumType.GenericCache = entry = new EnumInfo();
            }

            entry.EnsureReflectionInfoIsInitialized(enumType, getNames);

            return entry;
        }
    }
}
