// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

namespace System
{
    public abstract partial class Enum
    {
        internal sealed class EnumInfo<TStorage>
            where TStorage : struct, INumber<TStorage>
        {
            public readonly bool HasFlagsAttribute;
            public readonly bool ValuesAreSequentialFromZero;
            public readonly TStorage[] Values;
            public readonly string[] Names;

            // Each entry contains a list of sorted pair of enum field names and values, sorted by values
            public EnumInfo(bool hasFlagsAttribute, TStorage[] values, string[] names)
            {
                HasFlagsAttribute = hasFlagsAttribute;
                Values = values;
                Names = names;

                if (!AreSorted(values))
                {
                    Array.Sort(values, names);
                }

                ValuesAreSequentialFromZero = AreSequentialFromZero(values);
            }

            /// <summary>Create a copy of <see cref="Values"/>.</summary>
            public unsafe TResult[] CloneValues<TResult>() where TResult : struct
            {
                Debug.Assert(sizeof(TStorage) == sizeof(TResult));
                return MemoryMarshal.Cast<TStorage, TResult>(Values).ToArray();
            }
        }
    }
}
