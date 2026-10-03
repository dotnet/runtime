// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    public abstract partial class Enum
    {
        internal sealed partial class EnumInfo<TStorage>
            where TStorage : struct, INumber<TStorage>
        {
            public readonly bool HasFlagsAttribute;
            public readonly bool ValuesAreSequentialFromZero;
            public readonly TStorage[] Values;
            public readonly string[] Names;

            // Lazily-built, cached case-insensitive name-to-value lookup used to accelerate
            // case-insensitive parsing. Null until the first case-insensitive parse of this enum type.
            private Dictionary<string, TStorage>? _namesToValuesIgnoreCase;

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

            /// <summary>
            /// Gets a case-insensitive name-to-value lookup, building and caching it on first use.
            /// </summary>
            public Dictionary<string, TStorage> GetNamesToValuesIgnoreCase()
            {
                return Volatile.Read(ref _namesToValuesIgnoreCase) ?? Initialize();

                Dictionary<string, TStorage> Initialize()
                {
                    string[] names = Names;
                    TStorage[] values = Values;
                    var lookup = new Dictionary<string, TStorage>(names.Length, StringComparer.OrdinalIgnoreCase);

                    // Insert in Names order (which is sorted by value). For names that differ only by case,
                    // TryAdd keeps the first (lowest-value) entry, matching the original linear scan's
                    // "first match wins" behavior.
                    for (int i = 0; i < names.Length; i++)
                    {
                        lookup.TryAdd(names[i], values[i]);
                    }

                    // Publish atomically. If another thread raced and already published a lookup,
                    // reuse theirs so every caller observes a single cached instance. The fast-path
                    // Volatile.Read pairs an acquire load with this release publish so readers always
                    // observe a fully-initialized dictionary.
                    return Interlocked.CompareExchange(ref _namesToValuesIgnoreCase, lookup, null) ?? lookup;
                }
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
