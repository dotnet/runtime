// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection
{
    public abstract class EnumInfo
    {
        private protected EnumInfo(Type underlyingType, string[] names, bool isFlags)
        {
            UnderlyingType = underlyingType;
            Names = names;
            HasFlagsAttribute = isFlags;
        }

        internal Type UnderlyingType { get; }
        internal string[] Names { get; }
        internal bool HasFlagsAttribute { get; }
    }

    public sealed class EnumInfo<TStorage> : EnumInfo
        where TStorage : struct, INumber<TStorage>
    {
        public EnumInfo(Type underlyingType, TStorage[] values, string[] names, bool isFlags) :
            base(underlyingType, names, isFlags)
        {
            Debug.Assert(values.Length == names.Length);
            Debug.Assert(Enum.AreSorted(values));

            Values = values;
            ValuesAreSequentialFromZero = Enum.AreSequentialFromZero(values);
        }

        internal TStorage[] Values { get; }
        internal bool ValuesAreSequentialFromZero { get; }

        // Lazily-built, cached case-insensitive name-to-value lookup used to accelerate
        // case-insensitive parsing. Null until the first case-insensitive parse of this enum type.
        private Dictionary<string, TStorage>? _namesToValuesIgnoreCase;

        /// <summary>
        /// Gets a case-insensitive name-to-value lookup, building and caching it on first use.
        /// </summary>
        public Dictionary<string, TStorage> GetNamesToValuesIgnoreCase()
        {
            return _namesToValuesIgnoreCase ?? Initialize();

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
                // reuse theirs so every caller observes a single cached instance. The plain
                // read on the fast path above is safe: the reference is published with release
                // semantics and readers reach the contents through a data-dependent access.
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
