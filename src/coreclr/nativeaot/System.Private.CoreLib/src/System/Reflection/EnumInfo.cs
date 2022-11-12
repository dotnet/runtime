// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    [ReflectionBlocked]
    public sealed class EnumInfo
    {
        public EnumInfo(Type underlyingType, object[] rawValues, string[] names, bool isFlags)
        {
            Debug.Assert(rawValues.Length == names.Length);

            UnderlyingType = underlyingType;

            int numValues = rawValues.Length;
            ulong[] values = new ulong[numValues];
            for (int i = 0; i < numValues; i++)
            {
                object rawValue = rawValues[i];

                ulong rawUnboxedValue;
                if (rawValue is ulong)
                {
                    rawUnboxedValue = (ulong)rawValue;
                }
                else
                {
                    // This conversion is this way for compatibility: do a value-preseving cast to long - then store (and compare) as ulong. This affects
                    // the order in which the Enum apis return names and values.
                    rawUnboxedValue = (ulong)(((IConvertible)rawValue).ToInt64(null));
                }
                values[i] = rawUnboxedValue;
            }

            // Need to sort the `names` and `rawValues` arrays according to the `values` array
            ulong[] valuesCopy = (ulong[])values.Clone();
            Array.Sort(keys: valuesCopy, items: rawValues, comparer: Comparer<ulong>.Default);
            Array.Sort(keys: values, items: names, comparer: Comparer<ulong>.Default);

            Names = names;
            Values = values;

            // Create the unboxed version of values for the Values property to return. (We didn't do this earlier because
            // declaring "rawValues" as "Array" would prevent us from using the generic overload of Array.Sort()).
            //
            // The array element type is the underlying type, not the enum type. (The enum type could be an open generic.)
            ValuesAsUnderlyingType = Type.GetTypeCode(UnderlyingType) switch
            {
                TypeCode.Byte => new byte[numValues],
                TypeCode.SByte => new sbyte[numValues],
                TypeCode.UInt16 => new ushort[numValues],
                TypeCode.Int16 => new short[numValues],
                TypeCode.UInt32 => new uint[numValues],
                TypeCode.Int32 => new int[numValues],
                TypeCode.UInt64 => new ulong[numValues],
                TypeCode.Int64 => new long[numValues],
                _ => throw new NotSupportedException(),
            };
            Array.Copy(rawValues, ValuesAsUnderlyingType, numValues);

            HasFlagsAttribute = isFlags;

            ValuesAreSequentialFromZero = true;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != (ulong)i)
                {
                    ValuesAreSequentialFromZero = false;
                    break;
                }
            }
        }

        internal Type UnderlyingType { get; }
        internal string[] Names { get; }
        internal ulong[] Values { get; }
        internal Array ValuesAsUnderlyingType { get; }
        internal bool HasFlagsAttribute { get; }
        internal bool ValuesAreSequentialFromZero { get; }
    }
}
