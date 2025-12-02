// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;

namespace System.Net.Mime
{
    internal abstract class ByteEncoder : IByteEncoder
    {
        internal abstract WriteStateInfoBase WriteState { get; }

        protected abstract bool HasSpecialEncodingForCRLF { get; }

        public string GetEncodedString() => Encoding.ASCII.GetString(WriteState.Buffer, 0, WriteState.Length);

        public int EncodeBytes(ReadOnlySpan<byte> buffer, bool dontDeferFinalBytes, bool shouldAppendSpaceToCRLF)
        {
            // Add Encoding header, if any. e.g. =?encoding?b?
            WriteState.AppendHeader();

            bool _hasSpecialEncodingForCRLF = HasSpecialEncodingForCRLF;

            int cur = 0;
            for (; cur < buffer.Length; cur++)
            {
                if (LineBreakNeeded(buffer[cur]))
                {
                    AppendPadding();
                    WriteState.AppendCRLF(shouldAppendSpaceToCRLF);
                }

                if (_hasSpecialEncodingForCRLF && buffer.Slice(cur).StartsWith("\r\n"u8))
                {
                    AppendEncodedCRLF();
                    cur++;  // Transformed two chars, so shift the index to account for that
                }
                else
                {
                    AppendEncodedByte(buffer[cur]);
                }
            }

            if (dontDeferFinalBytes)
            {
                AppendPadding();
            }

            // Write out the last footer, if any.  e.g. ?=
            WriteState.AppendFooter();
            return cur;
        }

        public int EncodeString(string value, Encoding encoding)
        {
            Debug.Assert(value != null, "value was null");
            Debug.Assert(WriteState != null, "writestate was null");
            Debug.Assert(WriteState.Buffer != null, "writestate.buffer was null");

            byte[] buffer;
            if (encoding == Encoding.Latin1) // we don't need to check for codepoints
            {
                buffer = encoding.GetBytes(value);
                return EncodeBytes(buffer, true, true);
            }

            // Add Encoding header, if any. e.g. =?encoding?b?
            WriteState.AppendHeader();

            bool _hasSpecialEncodingForCRLF = HasSpecialEncodingForCRLF;

            int totalBytesCount = 0;
            buffer = new byte[encoding.GetMaxByteCount(2)];
            for (int i = 0; i < value.Length; ++i)
            {
                int codepointSize = GetCodepointSize(value, i);
                Debug.Assert(codepointSize == 1 || codepointSize == 2, "codepointSize was not 1 or 2");

                int bytesCount = encoding.GetBytes(value, i, codepointSize, buffer, 0);
                Span<byte> bytes = buffer.AsSpan(0, bytesCount);

                if (codepointSize == 2)
                {
                    ++i; // Transformed two chars, so shift the index to account for that
                }

                if (LineBreakNeeded(bytes))
                {
                    AppendPadding();
                    WriteState.AppendCRLF(true);
                }

                if (_hasSpecialEncodingForCRLF && IsCRLF(bytes))
                {
                    AppendEncodedCRLF();
                }
                else
                {
                    AppendEncodedCodepoint(bytes);
                }
                totalBytesCount += bytesCount;
            }

            AppendPadding();

            // Write out the last footer, if any.  e.g. ?=
            WriteState.AppendFooter();

            return totalBytesCount;
        }

        protected abstract void AppendEncodedCRLF();

        protected abstract bool LineBreakNeeded(byte b);
        protected abstract bool LineBreakNeeded(ReadOnlySpan<byte> bytes);

        protected abstract int GetCodepointSize(string value, int i);

        public abstract void AppendPadding();

        protected abstract void AppendEncodedByte(byte b);

        private void AppendEncodedCodepoint(ReadOnlySpan<byte> bytes)
        {
            foreach (byte b in bytes)
            {
                AppendEncodedByte(b);
            }
        }

        protected static bool IsSurrogatePair(string value, int i)
        {
            return char.IsSurrogate(value[i]) && i + 1 < value.Length && char.IsSurrogatePair(value[i], value[i + 1]);
        }

        protected static bool IsCRLF(ReadOnlySpan<byte> buffer)
        {
            return buffer.SequenceEqual("\r\n"u8);
        }
    }
}
