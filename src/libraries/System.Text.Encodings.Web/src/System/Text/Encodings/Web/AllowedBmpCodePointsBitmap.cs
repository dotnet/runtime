// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

#if NET
using System.Buffers;
using System.Numerics;
#endif

namespace System.Text.Encodings.Web
{
    /// <summary>
    /// A bitmap which represents all 64k codepoints in the
    /// Basic Multilingual Plane.
    /// </summary>
    internal unsafe struct AllowedBmpCodePointsBitmap
    {
        private const int BitmapLengthInDWords = 64 * 1024 / 32;
        private fixed uint Bitmap[BitmapLengthInDWords];

        /// <summary>
        /// Adds the given <see cref="char"/> to the bitmap's allow list.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AllowChar(char value)
        {
            _GetIndexAndOffset(value, out nuint index, out int offset);
            Bitmap[index] |= 1u << offset;
        }

        /// <summary>
        /// Removes the given <see cref="char"/> from the bitmap's allow list.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ForbidChar(char value)
        {
            _GetIndexAndOffset(value, out nuint index, out int offset);
            Bitmap[index] &= ~(1u << offset);
        }

        /// <summary>
        /// Removes all HTML-sensitive characters from the bitmap's allow list.
        /// </summary>
        public void ForbidHtmlCharacters()
        {
            ForbidChar('<');
            ForbidChar('>');
            ForbidChar('&');
            ForbidChar('\''); // can be used to escape attributes
            ForbidChar('\"'); // can be used to escape attributes
            ForbidChar('+'); // technically not HTML-specific, but can be used to perform UTF7-based attacks
        }

        /// <summary>
        /// Removes from the bitmap's allow list all code points which aren't mapped to defined characters
        /// or which are otherwise always disallowed.
        /// </summary>
        /// <remarks>
        /// Always-disallowed categories include Cc, Cs, Co, Cn, Zs [except U+0020 SPACE], Zl, and Zp.
        /// </remarks>
        public void ForbidUndefinedCharacters()
        {
            fixed (uint* pBitmap = Bitmap)
            {
                ReadOnlySpan<byte> definedCharsBitmapAsLittleEndian = UnicodeHelpers.GetDefinedBmpCodePointsBitmapLittleEndian();
                Span<uint> thisAllowedCharactersBitmap = new Span<uint>(pBitmap, BitmapLengthInDWords);
                Debug.Assert(definedCharsBitmapAsLittleEndian.Length == thisAllowedCharactersBitmap.Length * sizeof(uint));

#if NET
                if (Vector.IsHardwareAccelerated && BitConverter.IsLittleEndian)
                {
                    while (!definedCharsBitmapAsLittleEndian.IsEmpty)
                    {
                        (new Vector<uint>(definedCharsBitmapAsLittleEndian) & new Vector<uint>(thisAllowedCharactersBitmap)).CopyTo(thisAllowedCharactersBitmap);
                        definedCharsBitmapAsLittleEndian = definedCharsBitmapAsLittleEndian.Slice(Vector<byte>.Count);
                        thisAllowedCharactersBitmap = thisAllowedCharactersBitmap.Slice(Vector<uint>.Count);
                    }
                    Debug.Assert(thisAllowedCharactersBitmap.IsEmpty, "Both vectors should've been fully consumed.");
                    return;
                }
#endif

                // Not Core, or not little-endian, or not SIMD-optimized.
                for (int i = 0; i < thisAllowedCharactersBitmap.Length; i++)
                {
                    thisAllowedCharactersBitmap[i] &= BinaryPrimitives.ReadUInt32LittleEndian(definedCharsBitmapAsLittleEndian.Slice(i * sizeof(uint)));
                }
            }
        }

        /// <summary>
        /// Queries the bitmap to see if the given <see cref="char"/> is in the allow list.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsCharAllowed(char value)
        {
            // No bounds checks required: every char maps to a valid position in the bitmap
            _GetIndexAndOffset(value, out nuint index, out int offset);
            if ((Bitmap[index] & (1u << offset)) != 0) { return true; }
            else { return false; }
        }

        /// <summary>
        /// Queries the bitmap to see if the given code point is in the allow list.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool IsCodePointAllowed(uint value)
        {
            if (!UnicodeUtility.IsBmpCodePoint(value)) { return false; } // we only understand BMP
            _GetIndexAndOffset(value, out nuint index, out int offset);
            if ((Bitmap[index] & (1u << offset)) != 0) { return true; }
            else { return false; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void _GetIndexAndOffset(uint value, out nuint index, out int offset)
        {
            UnicodeDebug.AssertIsBmpCodePoint(value);
            index = value >> 5;
            offset = (int)value & 0x1F;
        }

#if NET
        /// <summary>
        /// Creates a <see cref="SearchValues{Char}"/> containing every <see cref="char"/> currently in the allow list.
        /// </summary>
        public readonly SearchValues<char> CreateSearchValues()
        {
            // Rather than probing all 64K code points individually, scan the bitmap one DWORD at a time and
            // use a vectorized search to quickly skip over runs of all-disallowed (zero) DWORDs.
            char[] allowedChars = ArrayPool<char>.Shared.Rent(64 * 1024);
            int count = 0;

            fixed (uint* pBitmap = Bitmap)
            {
                ReadOnlySpan<uint> bitmap = new ReadOnlySpan<uint>(pBitmap, BitmapLengthInDWords);

                int dwordIndex = 0;
                int next;
                while ((next = bitmap.IndexOfAnyExcept(0u)) >= 0)
                {
                    dwordIndex += next;
                    bitmap = bitmap.Slice(next);

                    uint dword = bitmap[0];
                    int charBase = dwordIndex * 32;
                    do
                    {
                        int bit = BitOperations.TrailingZeroCount(dword);
                        allowedChars[count++] = (char)(charBase + bit);
                        dword &= dword - 1; // clear the lowest set bit
                    }
                    while (dword != 0);

                    dwordIndex++;
                    bitmap = bitmap.Slice(1);
                }
            }

            SearchValues<char> result = SearchValues.Create(allowedChars.AsSpan(0, count));
            ArrayPool<char>.Shared.Return(allowedChars);
            return result;
        }
#endif
    }
}
