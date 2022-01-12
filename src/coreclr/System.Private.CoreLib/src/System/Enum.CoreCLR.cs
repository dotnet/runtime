// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public abstract partial class Enum
    {
        [DllImport(RuntimeHelpers.QCall, EntryPoint = "Enum_GetValuesAndNames")]
        private static extern void GetEnumValuesAndNames(QCallTypeHandle enumType, ObjectHandleOnStack values, ObjectHandleOnStack names, Interop.BOOL getNames);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object InternalBoxEnum(RuntimeType enumType, long value);

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
