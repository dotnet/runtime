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
            Type type;
            TypeCode typeCode = Type.GetTypeCode(enumType);
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    type = typeof(bool);
                    break;
                case TypeCode.Char:
                    type = typeof(char);
                    break;
                case TypeCode.SByte:
                    type = typeof(sbyte);
                    break;
                case TypeCode.Byte:
                    type = typeof(byte);
                    break;
                case TypeCode.Int16:
                    type = typeof(short);
                    break;
                case TypeCode.UInt16:
                    type = typeof(ushort);
                    break;
                case TypeCode.Int32:
                    type = typeof(int);
                    break;
                case TypeCode.UInt32:
                    type = typeof(uint);
                    break;
                case TypeCode.Int64:
                    type = typeof(long);
                    break;
                case TypeCode.UInt64:
                    type = typeof(ulong);
                    break;
                case TypeCode.Single:
                    type = typeof(float);
                    break;
                default:
                    Debug.Assert(typeCode == TypeCode.Double);
                    type = typeof(double);
                    break;
            }
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
