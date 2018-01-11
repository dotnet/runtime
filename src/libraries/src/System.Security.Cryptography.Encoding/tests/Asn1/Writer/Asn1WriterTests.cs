// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Cryptography.Asn1;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Tests.Asn1
{
    public abstract partial class Asn1WriterTests : Asn1ReaderTests
    {
        internal static void Verify(AsnWriter writer, string expectedHex)
        {
            byte[] encoded = writer.Encode();
            Assert.Equal(expectedHex, encoded.ByteArrayToHex());

            // Now verify TryEncode's boundary conditions.
            byte[] encoded2 = new byte[encoded.Length + 3];
            encoded2[0] = 255;
            encoded2[encoded.Length] = 254;

            Span<byte> dest = encoded2.AsSpan().Slice(0, encoded.Length - 1);
            Assert.False(writer.TryEncode(dest, out int bytesWritten), "writer.TryEncode (too small)");
            Assert.Equal(0, bytesWritten);
            Assert.Equal(255, encoded2[0]);
            Assert.Equal(254, encoded2[encoded.Length]);

            dest = encoded2.AsSpan().Slice(0, encoded.Length);
            Assert.True(writer.TryEncode(dest, out bytesWritten), "writer.TryEncode (exact length)");
            Assert.Equal(encoded.Length, bytesWritten);
            Assert.True(dest.SequenceEqual(encoded), "dest.SequenceEqual(encoded2) (exact length)");
            Assert.Equal(254, encoded2[encoded.Length]);

            // Start marker was obliterated, but the stop marker is still intact.  Keep it there.
            Array.Clear(encoded2, 0, bytesWritten);

            dest = encoded2.AsSpan();
            Assert.True(writer.TryEncode(dest, out bytesWritten), "writer.TryEncode (overly big)");
            Assert.Equal(encoded.Length, bytesWritten);
            Assert.True(dest.Slice(0, bytesWritten).SequenceEqual(encoded), "dest.SequenceEqual(encoded2) (overly big)");
            Assert.Equal(254, encoded2[encoded.Length]);
        }

        internal static unsafe string Stringify(Asn1Tag tag)
        {
            byte* stackspace = stackalloc byte[10];
            Span<byte> dest = new Span<byte>(stackspace, 10);

            Assert.True(tag.TryWrite(dest, out int size));
            return dest.Slice(0, size).ByteArrayToHex();
        }
    }
}
