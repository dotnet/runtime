// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers.Text
{
    public static class Base64Url
    {
        public static byte[] DecodeFromChars(System.ReadOnlySpan<char> source) { throw null; }
        public static int DecodeFromChars(System.ReadOnlySpan<char> source, System.Span<byte> destination) { throw null; }
        public static System.Buffers.OperationStatus DecodeFromChars(System.ReadOnlySpan<char> source, System.Span<byte> destination, out int charsConsumed, out int bytesWritten, bool isFinalBlock = true) { throw null; }
        public static byte[] DecodeFromUtf8(System.ReadOnlySpan<byte> source) { throw null; }
        public static int DecodeFromUtf8(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static System.Buffers.OperationStatus DecodeFromUtf8(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) { throw null; }
        public static int DecodeFromUtf8InPlace(System.Span<byte> buffer) { throw null; }
        public static char[] EncodeToChars(System.ReadOnlySpan<byte> source) { throw null; }
        public static int EncodeToChars(System.ReadOnlySpan<byte> source, System.Span<char> destination) { throw null; }
        public static System.Buffers.OperationStatus EncodeToChars(System.ReadOnlySpan<byte> source, System.Span<char> destination, out int bytesConsumed, out int charsWritten, bool isFinalBlock = true) { throw null; }
        public static string EncodeToString(System.ReadOnlySpan<byte> source) { throw null; }
        public static byte[] EncodeToUtf8(System.ReadOnlySpan<byte> source) { throw null; }
        public static int EncodeToUtf8(System.ReadOnlySpan<byte> source, System.Span<byte> destination) { throw null; }
        public static System.Buffers.OperationStatus EncodeToUtf8(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) { throw null; }
        public static int GetEncodedLength(int bytesLength) { throw null; }
        public static int GetMaxDecodedLength(int base64Length) { throw null; }
        public static bool IsValid(System.ReadOnlySpan<char> base64UrlText) { throw null; }
        public static bool IsValid(System.ReadOnlySpan<char> base64UrlText, out int decodedLength) { throw null; }
        public static bool IsValid(System.ReadOnlySpan<byte> utf8Base64UrlText) { throw null; }
        public static bool IsValid(System.ReadOnlySpan<byte> utf8Base64UrlText, out int decodedLength) { throw null; }
        public static bool TryDecodeFromChars(System.ReadOnlySpan<char> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static bool TryDecodeFromUtf8(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static bool TryEncodeToChars(System.ReadOnlySpan<byte> source, System.Span<char> destination, out int charsWritten) { throw null; }
        public static bool TryEncodeToUtf8(System.ReadOnlySpan<byte> source, System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static bool TryEncodeToUtf8InPlace(System.Span<byte> buffer, int dataLength, out int bytesWritten) { throw null; }
    }
}
