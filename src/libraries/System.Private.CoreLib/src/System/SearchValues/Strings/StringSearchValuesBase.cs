// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Buffers
{
    /// <summary>
    /// Implements the base <see cref="SearchValues{T}"/> {Last}IndexOfAny{Except} operations.
    /// While these operations are exposed such that you can call string[].IndexOfAny(searchValues),
    /// they are not expected to be used in performance-critical paths.
    /// <see cref="MemoryExtensions.IndexOfAny(ReadOnlySpan{char}, SearchValues{string})"/> is the main
    /// reason why someone would create an instance of <see cref="string"/> <see cref="SearchValues{T}"/>.
    /// </summary>
    internal abstract class StringSearchValuesBase : SearchValues<string>
    {
        private readonly HashSet<string>? _uniqueValues;

        /// <summary>
        /// This exists to allow <see cref="SingleStringSearchValuesThreeChars{TValueLength, TCaseSensitivity}"/> to avoid the HashSet allocation.
        /// </summary>
        protected bool HasUniqueValues => _uniqueValues is not null;

        public StringSearchValuesBase(HashSet<string>? uniqueValues) =>
            _uniqueValues = uniqueValues;

        internal override bool ContainsCore(string value)
        {
            Debug.Assert(_uniqueValues is not null, "ContainsCore should be overridden if uniqueValues weren't provided.");
            return _uniqueValues.Contains(value);
        }

        internal override string[] GetValues()
        {
            Debug.Assert(_uniqueValues is not null, "GetValues should be overridden if uniqueValues weren't provided.");
            string[] values = new string[_uniqueValues.Count];
            _uniqueValues.CopyTo(values);
            return values;
        }

        internal sealed override int IndexOfAny(ReadOnlySpan<string> span) =>
            IndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(span);

        internal sealed override int IndexOfAnyExcept(ReadOnlySpan<string> span) =>
            IndexOfAny<IndexOfAnyAsciiSearcher.Negate>(span);

        internal sealed override int LastIndexOfAny(ReadOnlySpan<string> span) =>
            LastIndexOfAny<IndexOfAnyAsciiSearcher.DontNegate>(span);

        internal sealed override int LastIndexOfAnyExcept(ReadOnlySpan<string> span) =>
            LastIndexOfAny<IndexOfAnyAsciiSearcher.Negate>(span);

        private int IndexOfAny<TNegator>(ReadOnlySpan<string> span)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (TNegator.NegateIfNeeded(ContainsCore(span[i])))
                {
                    return i;
                }
            }

            return -1;
        }

        private int LastIndexOfAny<TNegator>(ReadOnlySpan<string> span)
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            for (int i = span.Length - 1; i >= 0; i--)
            {
                if (TNegator.NegateIfNeeded(ContainsCore(span[i])))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
