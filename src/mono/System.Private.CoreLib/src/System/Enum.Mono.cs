// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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

        private static unsafe CorElementType InternalGetCorElementType(RuntimeType rt)
        {
            Debug.Assert(rt.IsActualEnum);
            return InternalGetCorElementType(new QCallTypeHandle(ref rt));
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

        private static unsafe EnumInfo<TUnderlyingValue> GetEnumInfo<TUnderlyingValue>(RuntimeType enumType, bool getNames = true)
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>
        {
            EnumInfo<TUnderlyingValue>? entry = enumType.Cache.EnumInfo as EnumInfo<TUnderlyingValue>;
            Debug.Assert(entry is null || entry.Names is not null);

            if (entry == null)
            {
                GetEnumValuesAndNames(new QCallTypeHandle(ref enumType), out ulong[]? uint64Values, out string[]? names);
                Debug.Assert(names is not null);
                Debug.Assert(uint64Values is not null);

                TUnderlyingValue[] values;
                if (typeof(TUnderlyingValue) == typeof(ulong))
                {
                    values = (TUnderlyingValue[])(object)uint64Values;
                }
                else
                {
#pragma warning disable 8500 // pointer to / sizeof managed types
                    values = new TUnderlyingValue[uint64Values.Length];
                    switch (sizeof(TUnderlyingValue))
                    {
                        case 1:
                            for (int i = 0; i < values.Length; i++)
                            {
                                byte value = (byte)uint64Values[i];
                                values[i] = *(TUnderlyingValue*)(&value);
                            }
                            break;

                        case 2:
                            for (int i = 0; i < values.Length; i++)
                            {
                                ushort value = (ushort)uint64Values[i];
                                values[i] = *(TUnderlyingValue*)(&value);
                            }
                            break;

                        case 4:
                            for (int i = 0; i < values.Length; i++)
                            {
                                uint value = (uint)uint64Values[i];
                                values[i] = *(TUnderlyingValue*)(&value);
                            }
                            break;

                        case 8:
                            for (int i = 0; i < values.Length; i++)
                            {
                                ulong value = uint64Values[i];
                                values[i] = *(TUnderlyingValue*)(&value);
                            }
                            break;
                    }
#pragma warning restore 8500
                }

                bool hasFlagsAttribute = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
                entry = new EnumInfo<TUnderlyingValue>(hasFlagsAttribute, values, names);
                enumType.Cache.EnumInfo = entry;
            }

            return entry;
        }
    }
}
