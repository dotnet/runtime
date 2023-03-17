// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;

namespace System
{
    public abstract partial class Enum
    {
        internal sealed class EnumInfo<TUnderlyingValue>
            where TUnderlyingValue : struct, INumber<TUnderlyingValue>
        {
            public readonly bool HasFlagsAttribute;
            public readonly bool ValuesAreSequentialFromZero;
            public readonly TUnderlyingValue[] Values;
            public readonly string[] Names;

            // Each entry contains a list of sorted pair of enum field names and values, sorted by values
            public EnumInfo(bool hasFlagsAttribute, TUnderlyingValue[] values, string[] names)
            {
                HasFlagsAttribute = hasFlagsAttribute;
                Values = values;
                Names = names;

                // Sort unsigned to maintain invariants for formatting
                if (typeof(TUnderlyingValue) == typeof(sbyte)) Sort<byte>(values, names);
                else if (typeof(TUnderlyingValue) == typeof(short)) Sort<ushort>(values, names);
                else if (typeof(TUnderlyingValue) == typeof(int)) Sort<uint>(values, names);
                else if (typeof(TUnderlyingValue) == typeof(long)) Sort<ulong>(values, names);
                else if (typeof(TUnderlyingValue) == typeof(nint)) Sort<nuint>(values, names);
                else Sort<TUnderlyingValue>(values, names);

                ValuesAreSequentialFromZero = AreSequentialFromZero(values);
            }

            /// <summary>Create a copy of <see cref="Values"/>.</summary>
            public TUnderlyingValue[] CloneValues() =>
                new ReadOnlySpan<TUnderlyingValue>(Values).ToArray();

            private static void Sort<TUnsignedValue>(TUnderlyingValue[] keys, string[] values)
                where TUnsignedValue : struct, INumber<TUnsignedValue>
            {
                // Rely on the runtime's ability to cast between primitive integer signed/unsigned counterparts
                TUnsignedValue[] unsignedKeys = (TUnsignedValue[])(object)keys;
                if (!AreSorted(unsignedKeys))
                {
                    Array.Sort(unsignedKeys, values);
                }
            }
        }
    }
}
