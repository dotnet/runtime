// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Unicode
{
    internal static class UnicodeTestHelpers
    {
        /// <summary>
        /// Returns a value stating whether a character is defined per the checked-in version
        /// of the Unicode specification. Certain classes of characters (control chars,
        /// private use, surrogates, some whitespace) are considered "undefined" for
        /// our purposes.
        /// </summary>
        internal static bool IsCharacterDefined(char c)
        {
            uint codePoint = (uint)c;
            int index = (int)(codePoint >> 5);
            int offset = (int)(codePoint & 0x1FU);
            return ((UnicodeHelpers.GetDefinedCharacterBitmap()[index] >> offset) & 0x1U) != 0;
        }
    }
}
