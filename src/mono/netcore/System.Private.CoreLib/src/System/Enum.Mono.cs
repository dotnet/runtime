// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
    public partial class Enum
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool GetEnumValuesAndNames(RuntimeType enumType, out ulong[] values, out string[] names);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object InternalBoxEnum(RuntimeType enumType, long value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern CorElementType InternalGetCorElementType();

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern RuntimeType InternalGetUnderlyingType(RuntimeType enumType);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern bool InternalHasFlag(Enum flags);

        private static EnumInfo GetEnumInfo(RuntimeType enumType, bool getNames = true)
        {
            EnumInfo? entry = enumType.Cache.EnumInfo;

            if (entry == null || (getNames && entry.Names == null))
            {
                if (!GetEnumValuesAndNames(enumType, out ulong[]? values, out string[]? names))
                    Array.Sort(values, names, Collections.Generic.Comparer<ulong>.Default);

                bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
                entry = new EnumInfo(hasFlagsAttribute, values, names);
                enumType.Cache.EnumInfo = entry;
            }

            return entry;
        }
    }
}
