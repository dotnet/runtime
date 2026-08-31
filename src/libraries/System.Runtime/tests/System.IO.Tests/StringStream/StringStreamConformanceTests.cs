// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading.Tasks;

namespace System.IO.Tests
{
    public class StringStreamConformanceTests_Memory : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => false;
        protected override bool CanSetLength => false;
        protected override bool NopFlushCompletesSynchronously => true;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            if (initialData is null || initialData.Length == 0)
            {
                return Task.FromResult<Stream?>(new StringStream(ReadOnlyMemory<char>.Empty, Encoding.UTF8));
            }

            char[] chars = new char[initialData.Length];
            for (int i = 0; i < initialData.Length; i++)
                chars[i] = (char)initialData[i];

            return Task.FromResult<Stream?>(
                new StringStream(chars.AsMemory(), IdentityEncoding.Instance));
        }

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);
    }

    public class StringStreamConformanceTests_String : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => false;
        protected override bool CanSetLength => false;
        protected override bool NopFlushCompletesSynchronously => true;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            if (initialData is null || initialData.Length == 0)
            {
                return Task.FromResult<Stream?>(new StringStream("", IdentityEncoding.Instance));
            }

            char[] chars = new char[initialData.Length];
            for (int i = 0; i < initialData.Length; i++)
                chars[i] = (char)initialData[i];

            return Task.FromResult<Stream?>(
                new StringStream(new string(chars), IdentityEncoding.Instance));
        }

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);
    }

    /// <summary>
    /// Maps each char to/from a single byte (1:1), allowing conformance tests to
    /// exercise StringStream with arbitrary byte data without UTF-8 round-trip issues.
    /// Provides a custom Encoder because the base Encoder.Convert throws
    /// "Conversion buffer overflow" when called with 0 chars (encoder flush path).
    /// </summary>
    internal sealed class IdentityEncoding : Encoding
    {
        public static IdentityEncoding Instance { get; } = new();

        public override int GetByteCount(char[] chars, int index, int count) => count;

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            for (int i = 0; i < charCount; i++)
            {
                bytes[byteIndex + i] = (byte)chars[charIndex + i];
            }

            return charCount;
        }

        public override int GetCharCount(byte[] bytes, int index, int count) => count;

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            for (int i = 0; i < byteCount; i++)
            {
                chars[charIndex + i] = (char)bytes[byteIndex + i];
            }

            return byteCount;
        }

        public override int GetMaxByteCount(int charCount) => charCount;

        public override int GetMaxCharCount(int byteCount) => byteCount;

        public override byte[] GetPreamble() => Array.Empty<byte>();

        public override Encoder GetEncoder() => new IdentityEncoder();

        private sealed class IdentityEncoder : Encoder
        {
            public override int GetByteCount(char[] chars, int index, int count, bool flush) => count;

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
            {
                for (int i = 0; i < charCount; i++)
                {
                    bytes[byteIndex + i] = (byte)chars[charIndex + i];
                }

                return charCount;
            }

            public override void Convert(ReadOnlySpan<char> chars, Span<byte> bytes, bool flush, out int charsUsed, out int bytesUsed, out bool completed)
            {
                int count = Math.Min(chars.Length, bytes.Length);
                for (int i = 0; i < count; i++)
                {
                    bytes[i] = (byte)chars[i];
                }

                charsUsed = count;
                bytesUsed = count;
                completed = chars.Length <= bytes.Length;
            }
        }
    }
}
