// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace Microsoft.Framework.WebEncoders
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
            return text == null ? -1 : 0;
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
            fixed (char* chars = unicodeScalar.ToString("X8"))
                for (int i = 0; i < Int32Length; i++)
                    buffer[i] = chars[i];

            numberOfCharactersWritten = Int32Length;
            return true;
        }
    }
}
