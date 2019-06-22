// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Encodings.Web;

namespace Microsoft.Framework.WebEncoders
{
    /// <summary>
    /// Dummy encoder used for unit testing.
    /// </summary>
    public sealed class ConfigurableScalarTextEncoder : TextEncoder
    {
        private readonly Predicate<int> _isScalarAllowed;

        public ConfigurableScalarTextEncoder(Predicate<int> isScalarAllowed)
        {
            _isScalarAllowed = isScalarAllowed;
        }

        public override int MaxOutputCharactersPerInputCharacter => throw new NotImplementedException();

        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength) => throw new NotImplementedException();

        public override bool WillEncode(int unicodeScalar) => !_isScalarAllowed(unicodeScalar);

        /// <summary>
        /// Encodes scalar as an unsigned hexadecimal number (min. 4 hex digits) surrounded by square brackets: "[XXXX]".
        /// </summary>
        public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            string encoded = FormattableString.Invariant($"[{(uint)unicodeScalar:X4}]");
            numberOfCharactersWritten = (encoded.Length <= (uint)bufferLength) ? encoded.Length : 0;
            return encoded.AsSpan().TryCopyTo(new Span<char>(buffer, bufferLength));
        }
    }
}
