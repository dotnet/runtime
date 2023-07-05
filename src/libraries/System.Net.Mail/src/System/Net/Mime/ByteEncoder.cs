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

        public int EncodeBytes(byte[] buffer, int offset, int count, bool dontDeferFinalBytes, bool shouldAppendSpaceToCRLF)
        {
            // Add Encoding header, if any. e.g. =?encoding?b?
            WriteState.AppendHeader();

            bool _hasSpecialEncodingForCRLF = HasSpecialEncodingForCRLF;

            int cur = offset;
            for (; cur < count + offset; cur++)
            {
                if (LineBreakNeeded(buffer[cur]))
                {
                    AppendPadding();
                    WriteState.AppendCRLF(shouldAppendSpaceToCRLF);
                }

                if (_hasSpecialEncodingForCRLF && IsCRLF(buffer, cur, count + offset))
                {
                    AppendEncodedCRLF();
                    cur++;  // Transformed two chars, so shift the index to account for that
                }
                else
                {
                    ApppendEncodedByte(buffer[cur]);
                }
            }

            if (dontDeferFinalBytes)
            {
                AppendPadding();
            }

            // Write out the last footer, if any.  e.g. ?=
            WriteState.AppendFooter();
            return cur - offset;
        }

        public int EncodeString(string value, Encoding encoding)
        {
            Debug.Assert(value != null, "value was null");
            Debug.Assert(WriteState != null, "writestate was null");
            Debug.Assert(WriteState.Buffer != null, "writestate.buffer was null");

            if (encoding == Encoding.Latin1) // we don't need to check for codepoints
            {
                byte[] buffer = encoding.GetBytes(value);
                return EncodeBytes(buffer, 0, buffer.Length, true, true);
            }

            // Add Encoding header, if any. e.g. =?encoding?b?
            WriteState.AppendHeader();

            bool _hasSpecialEncodingForCRLF = HasSpecialEncodingForCRLF;

            int totalBytesCount = 0;
            byte[] bytes = new byte[encoding.GetMaxByteCount(2)];
            for (int i = 0; i < value.Length; ++i)
            {
                int codepointSize = GetCodepointSize(value, i);
                Debug.Assert(codepointSize == 1 || codepointSize == 2, "codepointSize was not 1 or 2");

                int bytesCount = encoding.GetBytes(value, i, codepointSize, bytes, 0);
                if (codepointSize == 2)
                {
                    ++i; // Transformed two chars, so shift the index to account for that
                }

                if (LineBreakNeeded(bytes, bytesCount))
                {
                    AppendPadding();
                    WriteState.AppendCRLF(true);
                }

                if (_hasSpecialEncodingForCRLF && IsCRLF(bytes, bytesCount))
                {
                    AppendEncodedCRLF();
                }
                else
                {
                    AppendEncodedCodepoint(bytes, bytesCount);
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
        protected abstract bool LineBreakNeeded(byte[] bytes, int count);

        protected abstract int GetCodepointSize(string value, int i);

        public abstract void AppendPadding();

        protected abstract void ApppendEncodedByte(byte b);

        private void AppendEncodedCodepoint(byte[] bytes, int count)
        {
            for (int i = 0; i < count; ++i)
            {
                ApppendEncodedByte(bytes[i]);
            }
        }

        protected static bool IsSurrogatePair(string value, int i)
        {
            return char.IsSurrogate(value[i]) && i + 1 < value.Length && char.IsSurrogatePair(value[i], value[i + 1]);
        }

        protected static bool IsCRLF(byte[] bytes, int count)
        {
            return count == 2 && IsCRLF(bytes, 0, count);
        }

        private static bool IsCRLF(byte[] buffer, int i, int bufferSize)
        {
            return buffer[i] == '\r' && i + 1 < bufferSize && buffer[i + 1] == '\n';
        }
    }
}
