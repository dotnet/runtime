// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Text.Tests
{
    public partial class EncodingTest
    {
        static partial void ValidateSerializeDeserialize(Encoding e)
        {
            // Make sure the Encoding roundtrips
            Assert.Equal(e, BinaryFormatterHelpers.Clone(e));

            // Get an encoder and decoder from the encoding, and clone them
            Encoder origEncoder = e.GetEncoder();
            Decoder origDecoder = e.GetDecoder();
            Encoder clonedEncoder = BinaryFormatterHelpers.Clone(origEncoder);
            Decoder clonedDecoder = BinaryFormatterHelpers.Clone(origDecoder);

            // Encode and decode some text with each pairing
            const string InputText = "abcdefghijklmnopqrstuvwxyz";
            char[] inputTextChars = InputText.ToCharArray();
            var pairs = new[]
            {
                Tuple.Create(origEncoder, origDecoder),
                Tuple.Create(origEncoder, clonedDecoder),
                Tuple.Create(clonedEncoder, origDecoder),
                Tuple.Create(clonedEncoder, clonedDecoder),
            };
            var results = new List<char[]>();
            foreach (Tuple<Encoder, Decoder> pair in pairs)
            {
                byte[] encodedBytes = new byte[pair.Item1.GetByteCount(inputTextChars, 0, inputTextChars.Length, true)];
                Assert.Equal(encodedBytes.Length, pair.Item1.GetBytes(inputTextChars, 0, inputTextChars.Length, encodedBytes, 0, true));
                char[] decodedChars = new char[pair.Item2.GetCharCount(encodedBytes, 0, encodedBytes.Length)];
                Assert.Equal(decodedChars.Length, pair.Item2.GetChars(encodedBytes, 0, encodedBytes.Length, decodedChars, 0));
                results.Add(decodedChars);
            }

            // Validate that all of the pairings produced the same results
            foreach (char[] a in results)
            {
                foreach (char[] b in results)
                {
                    Assert.Equal(a, b);
                }
            }
        }
    }
}
