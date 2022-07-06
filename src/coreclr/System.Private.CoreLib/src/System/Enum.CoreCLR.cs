// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public abstract partial class Enum
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Enum_GetValuesAndNames")]
        private static partial void GetEnumValuesAndNames(QCallTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, Interop.BOOL getNames);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object InternalBoxEnum(RuntimeType enumType, long value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern CorElementType InternalGetCorElementType();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType InternalGetUnderlyingTypeImpl(RuntimeType enumType);

        internal static RuntimeType InternalGetUnderlyingType(RuntimeType enumType)
        {
            Type type = Type.GetTypeCode(enumType) switch
            {
                TypeCode.Boolean => typeof(bool),
                TypeCode.Char => typeof(char),
                TypeCode.SByte => typeof(sbyte),
                TypeCode.Byte => typeof(byte),
                TypeCode.Int16 => typeof(short),
                TypeCode.UInt16 => typeof(ushort),
                TypeCode.Int32 => typeof(int),
                TypeCode.UInt32 => typeof(uint),
                TypeCode.Int64 => typeof(long),
                TypeCode.UInt64 => typeof(ulong),
                TypeCode.Single => typeof(float),
                TypeCode.Double => typeof(double),
                _ => InternalGetUnderlyingTypeImpl(enumType)
            };
            return (RuntimeType)type;
        }

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
