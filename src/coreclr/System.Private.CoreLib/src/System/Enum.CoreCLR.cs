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
            object obj = RuntimeHelpers.AllocateUninitializedObject(pMethodTable);
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

        private static EnumInfo GetEnumInfo(RuntimeType enumType, bool getNames = true)
        {
            EnumInfo? entry = enumType.GenericCache as EnumInfo;

            if (entry == null || (getNames && entry.Names == null))
            {
                ulong[]? values = null;
                string[]? names = null;
                RuntimeTypeHandle enumTypeHandle = enumType.TypeHandle;
                GetEnumValuesAndNames(
                    new QCallTypeHandle(ref enumTypeHandle),
                    ObjectHandleOnStack.Create(ref values),
                    ObjectHandleOnStack.Create(ref names),
                    getNames ? Interop.BOOL.TRUE : Interop.BOOL.FALSE);
                bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);

                entry = new EnumInfo(hasFlagsAttribute, values!, names!);
                enumType.GenericCache = entry;
            }

            return entry;
        }
    }
}
