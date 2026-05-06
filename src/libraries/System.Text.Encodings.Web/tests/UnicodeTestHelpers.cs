// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;

namespace System.Text.Unicode
{
    internal static class UnicodeTestHelpers
    {
        private static Lazy<StrongBox<AllowedBmpCodePointsBitmap>> _lazyBitmap = new Lazy<StrongBox<AllowedBmpCodePointsBitmap>>(InitializeLazyBitmap);

        /// <summary>
        /// Returns a value stating whether a character is defined per the checked-in version
        /// of the Unicode specification. Certain classes of characters (control chars,
        /// private use, surrogates, some whitespace) are considered "undefined" for
        /// our purposes.
        /// </summary>
        internal static bool IsCharacterDefined(char c) => _lazyBitmap.Value.Value.IsCharAllowed(c);

        private static unsafe StrongBox<AllowedBmpCodePointsBitmap> InitializeLazyBitmap()
        {
            // Initialize the bitmap to all-ones (everything allowed), then mask it with
            // our carried list of defined chars. Everything that's disallowed will be set
            // to zero.

            AllowedBmpCodePointsBitmap bitmap = default;
            MemoryMarshal.AsBytes(new Span<AllowedBmpCodePointsBitmap>(&bitmap, 1)).Fill(0xFF);
            bitmap.ForbidUndefinedCharacters();
            return new StrongBox<AllowedBmpCodePointsBitmap>(bitmap);
        }
    }
}
