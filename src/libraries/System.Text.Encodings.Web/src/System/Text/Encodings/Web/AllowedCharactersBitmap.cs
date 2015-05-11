// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Unicode;

namespace System.Text.Internal
{
    internal struct AllowedCharactersBitmap
    {
        private const int ALLOWED_CHARS_BITMAP_LENGTH = 0x10000 / (8 * sizeof(uint));
        private readonly uint[] _allowedCharacters;

        // should be called in place of the default ctor
        public static AllowedCharactersBitmap CreateNew()
        {
            return new AllowedCharactersBitmap(new uint[ALLOWED_CHARS_BITMAP_LENGTH]);
        }

        private AllowedCharactersBitmap(uint[] allowedCharacters)
        {
            if(allowedCharacters == null)
            {
                throw new ArgumentNullException("allowedCharacters");
            }
            _allowedCharacters = allowedCharacters;
        }

        // Marks a character as allowed (can be returned unencoded)
        public void AllowCharacter(char character)
        {
            int codePoint = character;
            int index = codePoint >> 5;
            int offset = codePoint & 0x1F;
            _allowedCharacters[index] |= 0x1U << offset;
        }

        // Marks a character as forbidden (must be returned encoded)
        public void ForbidCharacter(char character)
        {
            int codePoint = character;
            int index = codePoint >> 5;
            int offset = codePoint & 0x1F;
            _allowedCharacters[index] &= ~(0x1U << offset);
        }

        // Forbid codepoints which aren't mapped to characters or which are otherwise always disallowed
        // (includes categories Cc, Cs, Co, Cn, Zs [except U+0020 SPACE], Zl, Zp)
        public void ForbidUndefinedCharacters()
        {
            uint[] definedCharactersBitmap = UnicodeHelpers.GetDefinedCharacterBitmap();
            Debug.Assert(definedCharactersBitmap.Length == _allowedCharacters.Length);
            for (int i = 0; i < _allowedCharacters.Length; i++)
            {
                _allowedCharacters[i] &= definedCharactersBitmap[i];
            }
        }

        // Marks all characters as forbidden (must be returned encoded)
        public void Clear()
        {
            Array.Clear(_allowedCharacters, 0, _allowedCharacters.Length);
        }

        // Creates a deep copy of this bitmap
        public AllowedCharactersBitmap Clone()
        {
            return new AllowedCharactersBitmap((uint[])_allowedCharacters.Clone());
        }

        // Determines whether the given character can be returned unencoded.
        public bool IsCharacterAllowed(char character)
        {
            int codePoint = character;
            int index = codePoint >> 5;
            int offset = codePoint & 0x1F;
            return ((_allowedCharacters[index] >> offset) & 0x1U) != 0;
        }

        // Determines whether the given character can be returned unencoded.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsUnicodeScalarAllowed(int unicodeScalar)
        {
            int index = unicodeScalar >> 5;
            int offset = unicodeScalar & 0x1F;
            return ((_allowedCharacters[index] >> offset) & 0x1U) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            for (int i = 0; i < textLength; i++)
            {
                if (!IsCharacterAllowed(text[i])) { return i; }
            }
            return -1;
        }
    }
}
