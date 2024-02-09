// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    public partial class CompareInfo
    {
        private SortKey InvariantCreateSortKey(string source, CompareOptions options)
        {
            ArgumentNullException.ThrowIfNull(source);

            if ((options & ValidCompareMaskOffFlags) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidFlag, nameof(options));
            }

            byte[] keyData;
            if (source.Length == 0)
            {
                keyData = Array.Empty<byte>();
            }
            else
            {
                // In the invariant mode, all string comparisons are done as ordinal so when generating the sort keys we generate it according to this fact
                keyData = new byte[source.Length * sizeof(char)];

                if ((options & (CompareOptions.IgnoreCase | CompareOptions.OrdinalIgnoreCase)) != 0)
                {
                    InvariantCreateSortKeyOrdinalIgnoreCase(source, keyData);
                }
                else
                {
                    InvariantCreateSortKeyOrdinal(source, keyData);
                }
            }

            return new SortKey(this, source, options, keyData);
        }

        private static void InvariantCreateSortKeyOrdinal(ReadOnlySpan<char> source, Span<byte> sortKey)
        {
            Debug.Assert(sortKey.Length >= source.Length * sizeof(char));

            for (int i = 0; i < source.Length; i++)
            {
                // convert machine-endian to big-endian
                BinaryPrimitives.WriteUInt16BigEndian(sortKey, (ushort)source[i]);
                sortKey = sortKey.Slice(sizeof(ushort));
            }
        }

        private static void InvariantCreateSortKeyOrdinalIgnoreCase(ReadOnlySpan<char> source, Span<byte> sortKey)
        {
            Debug.Assert(sortKey.Length >= source.Length * sizeof(char));

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                if (char.IsHighSurrogate(c) && i < source.Length - 1)
                {
                    char cl = source[i + 1];
                    if (char.IsLowSurrogate(cl))
                    {
                        SurrogateCasing.ToUpper(c, cl, out char hr, out char lr);
                        BinaryPrimitives.WriteUInt16BigEndian(sortKey, hr);
                        BinaryPrimitives.WriteUInt16BigEndian(sortKey, lr);
                        i++;
                        sortKey = sortKey.Slice(2 * sizeof(ushort));
                        continue;
                    }
                }

                // convert machine-endian to big-endian
                BinaryPrimitives.WriteUInt16BigEndian(sortKey, (ushort)InvariantModeCasing.ToUpper(c));
                sortKey = sortKey.Slice(sizeof(ushort));
            }
        }

        private static int InvariantGetSortKey(ReadOnlySpan<char> source, Span<byte> destination, CompareOptions options)
        {
            Debug.Assert(GlobalizationMode.Invariant);
            Debug.Assert((options & ValidCompareMaskOffFlags) == 0);

            // Make sure the destination buffer is large enough to hold the source projection.
            // Using unsigned arithmetic below also checks for buffer overflow since the incoming
            // length is always a non-negative signed integer.

            if ((uint)destination.Length < (uint)source.Length * sizeof(char))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            if ((options & CompareOptions.IgnoreCase) == 0)
            {
                InvariantCreateSortKeyOrdinal(source, destination);
            }
            else
            {
                InvariantCreateSortKeyOrdinalIgnoreCase(source, destination);
            }

            return source.Length * sizeof(char);
        }

        private static int InvariantGetSortKeyLength(ReadOnlySpan<char> source, CompareOptions options)
        {
            Debug.Assert(GlobalizationMode.Invariant);
            Debug.Assert((options & ValidCompareMaskOffFlags) == 0);

            // In invariant mode, sort keys are simply a byte projection of the source input,
            // optionally with casing modifications. We need to make sure we don't overflow
            // while computing the length.

            int byteLength = source.Length * sizeof(char);

            if (byteLength < 0)
            {
                throw new ArgumentException(
                    paramName: nameof(source),
                    message: SR.ArgumentOutOfRange_GetByteCountOverflow);
            }

            return byteLength;
        }

        private static int InvariantGetHashCode(ReadOnlySpan<char> source, CompareOptions options)
        {
            if ((options & CompareOptions.IgnoreCase) == 0)
            {
                return string.GetHashCode(source);
            }

            return string.GetHashCodeOrdinalIgnoreCase(source);
        }
    }
}
