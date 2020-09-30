// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Test.Cryptography;
using Xunit;

namespace System.Formats.Asn1.Tests.Writer
{
    public abstract partial class Asn1WriterTests
    {
        internal static void Verify(AsnWriter writer, string expectedHex)
        {
            int expectedSize = writer.GetEncodedLength();

            byte[] encoded = writer.Encode();
            Assert.Equal(expectedHex, encoded.ByteArrayToHex());
            Assert.Equal(expectedSize, encoded.Length);

            // Now verify TryEncode's boundary conditions.
            byte[] encoded2 = new byte[encoded.Length + 3];
            encoded2[0] = 255;
            encoded2[encoded.Length] = 254;

            Span<byte> dest = encoded2.AsSpan(0, encoded.Length - 1);
            Assert.False(writer.TryEncode(dest, out int bytesWritten), "writer.TryEncode (too small)");
            Assert.Equal(0, bytesWritten);
            AssertExtensions.Throws<ArgumentException>("destination", () => writer.Encode(encoded2.AsSpan(0, encoded.Length - 1)));
            Assert.Equal(255, encoded2[0]);
            Assert.Equal(254, encoded2[encoded.Length]);

            dest = encoded2.AsSpan(0, encoded.Length);
            Assert.True(writer.TryEncode(dest, out bytesWritten), "writer.TryEncode (exact length)");
            Assert.Equal(encoded.Length, bytesWritten);
            Assert.True(dest.SequenceEqual(encoded), "dest.SequenceEqual(encoded2) (exact length) from TryEncode");
            dest.Clear();
            Assert.Equal(encoded.Length, writer.Encode(dest));
            Assert.True(dest.SequenceEqual(encoded), "dest.SequenceEqual(encoded2) (exact length) from Encode");
            Assert.Equal(254, encoded2[encoded.Length]);

            // Start marker was obliterated, but the stop marker is still intact.  Keep it there.
            Array.Clear(encoded2, 0, bytesWritten);

            dest = encoded2.AsSpan();
            Assert.True(writer.TryEncode(dest, out bytesWritten), "writer.TryEncode (overly big)");
            Assert.Equal(encoded.Length, bytesWritten);
            Assert.True(dest.Slice(0, bytesWritten).SequenceEqual(encoded), "dest.SequenceEqual(encoded2) (overly big) from TryEncode");
            dest.Slice(0, bytesWritten).Clear();
            Assert.Equal(encoded.Length, writer.Encode(dest));
            Assert.True(dest.Slice(0, bytesWritten).SequenceEqual(encoded), "dest.SequenceEqual(encoded2) (overly big) from Encode");
            Assert.Equal(254, encoded2[encoded.Length]);

            Assert.True(writer.EncodedValueEquals(encoded));
            Assert.False(writer.EncodedValueEquals(encoded2));
            Assert.True(writer.EncodedValueEquals(encoded2.AsSpan(0, encoded.Length)));
            Assert.False(writer.EncodedValueEquals(encoded2.AsSpan(1, encoded.Length)));

            encoded2[encoded.Length - 1] ^= 0xFF;
            Assert.False(writer.EncodedValueEquals(encoded2.AsSpan(0, encoded.Length)));
        }

        internal static unsafe string Stringify(Asn1Tag tag)
        {
            byte* stackspace = stackalloc byte[10];
            Span<byte> dest = new Span<byte>(stackspace, 10);

            Assert.True(tag.TryEncode(dest, out int size));
            return dest.Slice(0, size).ByteArrayToHex();
        }
    }
}
