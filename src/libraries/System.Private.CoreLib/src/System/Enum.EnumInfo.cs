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

                if (!AreSorted(values))
                {
                    Array.Sort(keys: values, items: names);
                }

                ValuesAreSequentialFromZero = AreSequentialFromZero(values);
            }

            /// <summary>Create a copy of <see cref="Values"/>.</summary>
            public TUnderlyingValue[] CloneValues() =>
                new ReadOnlySpan<TUnderlyingValue>(Values).ToArray();
        }
    }
}
