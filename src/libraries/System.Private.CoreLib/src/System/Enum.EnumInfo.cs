// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public abstract partial class Enum
    {
        internal sealed class EnumInfo
        {
            public readonly bool HasFlagsAttribute;
            public readonly bool ValuesAreSequentialFromZero;
            public readonly ulong[] Values;
            public readonly string[] Names;

            // Each entry contains a list of sorted pair of enum field names and values, sorted by values
            public EnumInfo(bool hasFlagsAttribute, ulong[] values, string[] names)
            {
                HasFlagsAttribute = hasFlagsAttribute;
                Values = values;
                Names = names;

                // Store whether all of the values are sequential starting from zero.
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
        }
    }
}
