// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
    public partial class Enum
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool GetEnumValuesAndNames(QCallTypeHandle enumType, out ulong[] values, out string[] names);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalBoxEnum(QCallTypeHandle enumType, ObjectHandleOnStack res, long value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern CorElementType InternalGetCorElementType(QCallTypeHandle enumType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void InternalGetUnderlyingType(QCallTypeHandle enumType, ObjectHandleOnStack res);

        private static object InternalBoxEnum(RuntimeType enumType, long value)
        {
            object? res = null;
            InternalBoxEnum(new QCallTypeHandle(ref enumType), ObjectHandleOnStack.Create(ref res), value);
            return res!;
        }

        private CorElementType InternalGetCorElementType()
        {
            RuntimeType this_type = (RuntimeType)GetType();
            return InternalGetCorElementType(new QCallTypeHandle(ref this_type));
        }

        internal static RuntimeType InternalGetUnderlyingType(RuntimeType enumType)
        {
            RuntimeType? res = null;
            InternalGetUnderlyingType(new QCallTypeHandle(ref enumType), ObjectHandleOnStack.Create(ref res));
            return res!;
        }

        private static EnumInfo GetEnumInfo(RuntimeType enumType, bool getNames = true)
        {
            EnumInfo? entry = enumType.Cache.EnumInfo;

            if (entry == null || (getNames && entry.Names == null))
            {
                if (!GetEnumValuesAndNames(new QCallTypeHandle(ref enumType), out ulong[]? values, out string[]? names))
                    Array.Sort(values, names, Collections.Generic.Comparer<ulong>.Default);

                bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
                entry = new EnumInfo(hasFlagsAttribute, values, names);
                enumType.Cache.EnumInfo = entry;
            }

            return entry;
        }
    }
}
