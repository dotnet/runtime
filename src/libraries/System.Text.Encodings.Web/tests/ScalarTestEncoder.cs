// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;

namespace System.Text.Encodings.Web.Tests
{
    /// <summary>
    /// Dummy encoder used for unit testing.
    /// </summary>
    public sealed class ScalarTestEncoder : TextEncoder
    {
        private const int Int32Length = 8;

        /// <summary>
        /// Returns 0.
        /// </summary>
        public override unsafe int FindFirstCharacterToEncode(char* text, int textLength)
        {
            return (textLength == 0) ? -1 : 0;
        }

        /// <summary>
        /// Returns true.
        /// </summary>
        public override bool WillEncode(int unicodeScalar)
        {
            return true;
        }

        /// <summary>
        /// Returns 8.
        /// </summary>
        public override int MaxOutputCharactersPerInputCharacter
        {
            get { return Int32Length; }
        }

        /// <summary>
        /// Encodes scalar as a hexadecimal number.
        /// </summary>
        public override unsafe bool TryEncodeUnicodeScalar(int unicodeScalar, char* buffer, int bufferLength, out int numberOfCharactersWritten)
        {
            if (unicodeScalar.ToString("X8", CultureInfo.InvariantCulture).AsSpan().TryCopyTo(new Span<char>(buffer, bufferLength)))
            {
                numberOfCharactersWritten = Int32Length;
                return true;
            }
            else
            {
                numberOfCharactersWritten = 0;
                return false;
            }
        }
    }
}
