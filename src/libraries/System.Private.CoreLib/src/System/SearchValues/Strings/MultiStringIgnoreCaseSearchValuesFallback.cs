// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Buffers
{
    internal sealed class MultiStringIgnoreCaseSearchValuesFallback : StringSearchValuesBase
    {
        private readonly string[] _values;

        public MultiStringIgnoreCaseSearchValuesFallback(HashSet<string> uniqueValues) : base(uniqueValues)
        {
            _values = new string[uniqueValues.Count];
            uniqueValues.CopyTo(_values, 0);
        }

        /// <summary>
        /// This method is intentionally implemented in a way that checks haystack positions one at a time.
        /// See the description in <see cref="SpanHelpers.IndexOfAny{T}(ref T, int, ref T, int)"/>.
        /// </summary>
        internal override int IndexOfAnyMultiString(ReadOnlySpan<char> span)
        {
            string[] values = _values;

            for (int i = 0; i < span.Length; i++)
            {
                ReadOnlySpan<char> remaining = span.Slice(i);

                foreach (string value in values)
                {
                    if (remaining.StartsWith(value, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }
    }
}
