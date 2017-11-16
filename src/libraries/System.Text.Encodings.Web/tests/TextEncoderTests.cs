// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Xunit;

namespace Microsoft.Framework.WebEncoders
{
    public class TextEncoderTests
    {
        [Fact]
        public void EncodeIntoBuffer_SurrogatePairs()
        {
            // Arange
            ScalarTestEncoder encoder = new ScalarTestEncoder();

            const string X = "\U00000058"; // LATIN CAPITAL LETTER X (ascii)
            const string Pair = "\U0001033A"; // GOTHIC LETTER KUSMA (surrogate pair)

            const string eX = "00000058";
            const string ePair = "0001033A";

            // Act & assert
            Assert.Equal("", encoder.Encode(""));

            Assert.Equal(eX, encoder.Encode(X)); // no iteration, block
            Assert.Equal(eX + eX, encoder.Encode(X + X)); // two iterations, no block
            Assert.Equal(eX + eX + eX, encoder.Encode(X + X + X)); // two iterations, block

            Assert.Equal(ePair, encoder.Encode(Pair)); // one iteration, no block
            Assert.Equal(ePair + ePair, encoder.Encode(Pair + Pair)); // two iterations, no block

            Assert.Equal(eX + ePair, encoder.Encode(X + Pair)); // two iterations, no block
            Assert.Equal(ePair + eX, encoder.Encode(Pair + X)); // one iteration, block

            Assert.Equal(eX + ePair + eX, encoder.Encode(X + Pair + X)); // two iterations, block, even length
            Assert.Equal(ePair + eX + ePair, encoder.Encode(Pair + X + Pair)); // three iterations, no block, odd length
        }
    }
}
