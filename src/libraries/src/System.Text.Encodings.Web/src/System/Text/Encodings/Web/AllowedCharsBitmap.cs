// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace System.Text.Internal
{
    internal struct AllowedCharsBitmap
    {
        private const int ALLOWED_CHARS_BITMAP_LENGTH = 0x10000 / (8 * sizeof(uint));
        private readonly uint[] _allowedCharsBitmap;

        private AllowedCharsBitmap(uint[] allowedCharsBitmap)
        {
            Debug.Assert(allowedCharsBitmap != null);
            _allowedCharsBitmap = allowedCharsBitmap;
        }

        // Marks a character as allowed (can be returned unencoded)
        public void AllowCharacter(char c)
        {
            int codePoint = c;
            int index = codePoint >> 5;
            int offset = codePoint & 0x1F;
            _allowedCharsBitmap[index] |= 0x1U << offset;
        }

        // Marks all characters as forbidden (must be returned encoded)
        public void Clear()
        {
            Array.Clear(_allowedCharsBitmap, 0, _allowedCharsBitmap.Length);
        }

        // Creates a deep copy of this bitmap
        public AllowedCharsBitmap Clone()
        {
            return new AllowedCharsBitmap((uint[])_allowedCharsBitmap.Clone());
        }

        // should be called in place of the ctor
        public static AllowedCharsBitmap CreateNew()
        {
            return new AllowedCharsBitmap(new uint[ALLOWED_CHARS_BITMAP_LENGTH]);
        }

        // Marks a character as forbidden (must be returned encoded)
        public void ForbidCharacter(char c)
        {
            int codePoint = c;
            int index = codePoint >> 5;
            int offset = codePoint & 0x1F;
            _allowedCharsBitmap[index] &= ~(0x1U << offset);
        }

        public void ForbidUndefinedCharacters()
        {
            // Forbid codepoints which aren't mapped to characters or which are otherwise always disallowed
            // (includes categories Cc, Cs, Co, Cn, Zs [except U+0020 SPACE], Zl, Zp)
            uint[] definedCharactersBitmap = UnicodeHelpers.GetDefinedCharacterBitmap();
            Debug.Assert(definedCharactersBitmap.Length == _allowedCharsBitmap.Length);
            for (int i = 0; i < _allowedCharsBitmap.Length; i++)
            {
                _allowedCharsBitmap[i] &= definedCharactersBitmap[i];
            }
        }

        // Determines whether the given character can be returned unencoded.
        public bool IsCharacterAllowed(char c)
        {
            int codePoint = c;
            int index = codePoint >> 5;
            int offset = codePoint & 0x1F;
            return ((_allowedCharsBitmap[index] >> offset) & 0x1U) != 0;
        }

        // Determines whether the given character can be returned unencoded.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUnicodeScalarAllowed(int unicodeScalar)
        {
            int index = unicodeScalar >> 5;
            int offset = unicodeScalar & 0x1F;
            return ((_allowedCharsBitmap[index] >> offset) & 0x1U) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static int FindFirstCharacterToEncode(AllowedCharsBitmap bitmap, char* text, int charCount)
        {
            for (int i = 0; i < charCount; i++)
            {
                if (!bitmap.IsCharacterAllowed(text[i])) { return i; }
            }
            return -1;
        }
    }
}
