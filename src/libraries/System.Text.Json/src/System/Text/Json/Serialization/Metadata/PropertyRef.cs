// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Represents a UTF-8 encoded JSON property name and its associated <see cref="JsonPropertyInfo"/>, if available.
    /// PropertyRefs use byte sequence equality, so equal JSON strings with alternate encodings or casings are not equal.
    /// Used as a first-level cache for property lookups before falling back to UTF decoding and string comparison.
    /// </summary>
    internal readonly struct PropertyRef(ulong key, JsonPropertyInfo? info, byte[] utf8PropertyName) : IEquatable<PropertyRef>
    {
        // The length of the property name embedded in the key (in bytes).
        // The key is a ulong (8 bytes) containing the first 7 bytes of the property name
        // followed by a byte representing the length.
        private const int PropertyNameKeyLength = 7;

        /// <summary>
        /// A custom hashcode produced from the UTF-8 encoded property name.
        /// </summary>
        public readonly ulong Key = key;

        /// <summary>
        /// The <see cref="JsonPropertyInfo"/> associated with the property name, if available.
        /// </summary>
        public readonly JsonPropertyInfo? Info = info;

        /// <summary>
        /// Caches a heap allocated copy of the UTF-8 encoded property name.
        /// </summary>
        public readonly byte[] Utf8PropertyName = utf8PropertyName;

        public bool Equals(PropertyRef other) => Equals(other.Utf8PropertyName, other.Key);
        public override bool Equals(object? obj) => obj is PropertyRef other && Equals(other);
        public override int GetHashCode() => Key.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ReadOnlySpan<byte> propertyName, ulong key)
        {
            if (key == Key)
            {
                // We compare the whole name, although we could skip the first 7 bytes (but it's not any faster)
                if (propertyName.Length <= PropertyNameKeyLength ||
                    propertyName.SequenceEqual(Utf8PropertyName))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get a key from the property name.
        /// The key consists of the first 7 bytes of the property name and then the length.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong GetKey(ReadOnlySpan<byte> name)
        {
            ulong key;

            ref byte reference = ref MemoryMarshal.GetReference(name);
            int length = name.Length;

            if (length > 7)
            {
                key = Unsafe.ReadUnaligned<ulong>(ref reference) & 0x00ffffffffffffffL;
                key |= (ulong)Math.Min(length, 0xff) << 56;
            }
            else
            {
                key =
                    length > 5 ? Unsafe.ReadUnaligned<uint>(ref reference) | (ulong)Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref reference, 4)) << 32 :
                    length > 3 ? Unsafe.ReadUnaligned<uint>(ref reference) :
                    length > 1 ? Unsafe.ReadUnaligned<ushort>(ref reference) : 0UL;
                key |= (ulong)length << 56;

                if ((length & 1) != 0)
                {
                    int offset = length - 1;
                    key |= (ulong)Unsafe.Add(ref reference, offset) << (offset * 8);
                }
            }

#if DEBUG
            // Verify key contains the embedded bytes as expected.
            // Note: the expected properties do not hold true on big-endian platforms
            if (BitConverter.IsLittleEndian)
            {
                const int BitsInByte = 8;
                Debug.Assert(
                    // Verify embedded property name.
                    (name.Length < 1 || name[0] == ((key & ((ulong)0xFF << BitsInByte * 0)) >> BitsInByte * 0)) &&
                    (name.Length < 2 || name[1] == ((key & ((ulong)0xFF << BitsInByte * 1)) >> BitsInByte * 1)) &&
                    (name.Length < 3 || name[2] == ((key & ((ulong)0xFF << BitsInByte * 2)) >> BitsInByte * 2)) &&
                    (name.Length < 4 || name[3] == ((key & ((ulong)0xFF << BitsInByte * 3)) >> BitsInByte * 3)) &&
                    (name.Length < 5 || name[4] == ((key & ((ulong)0xFF << BitsInByte * 4)) >> BitsInByte * 4)) &&
                    (name.Length < 6 || name[5] == ((key & ((ulong)0xFF << BitsInByte * 5)) >> BitsInByte * 5)) &&
                    (name.Length < 7 || name[6] == ((key & ((ulong)0xFF << BitsInByte * 6)) >> BitsInByte * 6)) &&
                    // Verify embedded length.
                    (name.Length >= 0xFF || (key & ((ulong)0xFF << BitsInByte * 7)) >> BitsInByte * 7 == (ulong)name.Length) &&
                    (name.Length < 0xFF || (key & ((ulong)0xFF << BitsInByte * 7)) >> BitsInByte * 7 == 0xFF),
                    "Embedded bytes not as expected");
            }
#endif
            return key;
        }
    }
}
