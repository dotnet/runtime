// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
    public partial class Enum
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern void GetEnumValuesAndNames(QCallTypeHandle enumType, out ulong[] values, out string[] names);

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

        /// <summary>Creates a new TUnderlyingValue[] from a ulong[] array of values.</summary>
        private static TUnderlyingValue[] ToUnderlyingValues<TUnderlyingValue>(ulong[] uint64Values)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>
        {
            TUnderlyingValue[] values;

            if (typeof(TUnderlyingValue) == typeof(ulong))
            {
                values = (TUnderlyingValue[])(object)uint64Values;
            }
            else
            {
                values = new TUnderlyingValue[uint64Values.Length];

                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = TUnderlyingValue.CreateTruncating(uint64Values[i]);
                }
            }

            return values;
        }

        private static EnumInfo<TUnderlyingValue> GetEnumInfo<TUnderlyingValue>(RuntimeType enumType, bool getNames = true)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>
        {
            EnumInfo<TUnderlyingValue>? entry = enumType.Cache.EnumInfo as EnumInfo<TUnderlyingValue>;

            if (entry == null || (getNames && entry.Names == null))
            {
                GetEnumValuesAndNames(new QCallTypeHandle(ref enumType), out ulong[]? uint64Values, out string[]? names);

                TUnderlyingValue[] values = ToUnderlyingValues<TUnderlyingValue>(uint64Values!);

                bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
                entry = new EnumInfo<TUnderlyingValue>(hasFlagsAttribute, values, names);
                enumType.Cache.EnumInfo = entry;
            }

            return entry;
        }
    }
}
