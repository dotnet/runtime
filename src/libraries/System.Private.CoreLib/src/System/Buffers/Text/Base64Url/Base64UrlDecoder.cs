// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Text
{
    public static partial class Base64Url
    {
        /*// Decode from utf8 => bytes
        public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> utf8, Span<byte> bytes, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) => throw new NotImplementedException();
        public static OperationStatus DecodeFromUtf8InPlace(Span<byte> buffer, out int bytesWritten) => throw new NotImplementedException();

        /// <summary>
        /// Returns the maximum length (in bytes) of the result if you were to decode base 64 encoded text within a byte span of size "length".
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the specified <paramref name="length"/> is less than 0.
        /// </exception>
        public static int GetMaxDecodedFromUtf8Length(int length) => throw new NotImplementedException();

        // IsValid
        public static bool IsValid(ReadOnlySpan<char> base64UrlText) => throw new NotImplementedException();
        public static bool IsValid(ReadOnlySpan<char> base64UrlText, out int decodedLength) => throw new NotImplementedException();
        public static bool IsValid(ReadOnlySpan<byte> base64UrlTextUtf8) => throw new NotImplementedException();
        public static bool IsValid(ReadOnlySpan<byte> base64UrlTextUtf8, out int decodedLength) => throw new NotImplementedException();

        // Up to this point, this is a mirror of System.Buffers.Text.Base64
        // Below are more helpers that bring over functionality similar to Convert.*Base64*

        // Encode to / decode from chars
        public static bool TryDecodeFromChars(ReadOnlySpan<char> chars, Span<byte> bytes, out int bytesWritten) => throw new NotImplementedException();


        // These are just accelerator methods.
        // Should be efficiently implementable on top of the other ones in just a few lines.

        // Decode from chars => string
        // Decode from chars => byte[]
        // The names could also just be "Decode" without naming the return type
        public static string DecodeToString(ReadOnlySpan<char> chars, Encoding encoding) => throw new NotImplementedException();
        public static byte[] DecodeToByteArray(ReadOnlySpan<char> chars) => throw new NotImplementedException();*/
    }
}
