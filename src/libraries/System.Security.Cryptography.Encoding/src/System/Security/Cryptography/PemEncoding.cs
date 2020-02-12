// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public static class PemEncoding
    {
        private const string s_Preeb = "-----BEGIN ";
        private const string s_Posteb = "-----END ";
        private const string s_Ending = "-----";

        public static PemFields Find(ReadOnlySpan<char> pemData)
        {
            if (!TryFind(pemData, out PemFields fields))
            {
                throw new ArgumentException(SR.Argument_PemEncoding_NoPemFound, nameof(pemData));
            }
            return fields;
        }

        public static bool TryFind(ReadOnlySpan<char> pemData, out PemFields fields)
        {
            // Check for the minimum possible encoded length of a PEM structure
            // and exit early if there is no way the input could contain a well-formed
            // PEM.
            if (pemData.Length < s_Preeb.Length + s_Ending.Length * 2 + s_Posteb.Length)
            {
                fields = default;
                return false;
            }

            int preebLinePosition = 0;
            while (TryReadNextLine(pemData, ref preebLinePosition, out Range lineRange))
            {
                ReadOnlySpan<char> line = pemData[lineRange];
                int preebIndex = line.IndexOf(s_Preeb);

                if (preebIndex == -1 ||
                    (preebIndex > 0 && !line[..preebIndex].IsWhiteSpace())) // can only be preceeded by whitespace
                {
                    continue;
                }

                int preebEndingIndex = line[(preebIndex + s_Preeb.Length)..].IndexOf(s_Ending);

                if (preebEndingIndex == -1)
                {
                    continue;
                }

                (int preebOffset, _) = lineRange.GetOffsetAndLength(pemData.Length);
                int preebStartIndex = preebOffset + preebIndex;
                int startLabelIndex = preebStartIndex + s_Preeb.Length;
                int endLabelIndex = startLabelIndex + preebEndingIndex;
                Range labelRange = startLabelIndex..endLabelIndex;
                ReadOnlySpan<char> label = pemData[labelRange];

                if (!IsValidLabel(label))
                {
                    continue;
                }

                ReadOnlySpan<char> posteb = string.Concat(s_Posteb, label, s_Ending);
                Range postebLineRange = lineRange;
                int postebLinePosition = preebLinePosition;

                // in lax decoding a posteb may appear on the same line as the preeb.
                // start on the current line. We do not need to check that this posteb
                // comes after the preeb because the preeb's prior content has already
                // been validated to be whitespace.
                do
                {
                    ReadOnlySpan<char> postebLine = pemData[postebLineRange];
                    int postebIndex = postebLine.IndexOf(posteb);

                    if (postebIndex == -1)
                    {
                        continue;
                    }

                    (int postebOffset, _) = postebLineRange.GetOffsetAndLength(pemData.Length);
                    int postebEndIndex = postebOffset + postebIndex + posteb.Length;
                    Range location = preebStartIndex..postebEndIndex;
                    Range content = (endLabelIndex + s_Ending.Length)..(postebOffset + postebIndex);

                    if (!postebLine[(postebIndex + posteb.Length)..].IsWhiteSpace())
                    {
                        break;
                    }

                    if (IsValidBase64(pemData, content, out Range base64range, out int decodedBase64Size))
                    {
                        fields = new PemFields(labelRange, base64range, location, decodedBase64Size);
                        return true;
                    }
                    break;
                }
                while (TryReadNextLine(pemData, ref postebLinePosition, out postebLineRange));
            }

            fields = default;
            return false;
        }

        private static bool IsValidLabel(ReadOnlySpan<char> data)
        {
            static bool IsLabelChar(char c) => c >= 0x21 && c <= 0x7e && c != '-';

            if (data.Length == 0)
                return true;

            // First character of label must be a labelchar
            if (!IsLabelChar(data[0]))
                return false;

            for (int index = 1; index < data.Length; index++)
            {
                char c = data[index];
                if (!IsLabelChar(c) && c != ' ' && c != '-')
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsValidBase64(
            ReadOnlySpan<char> data,
            Range content,
            out Range base64range,
            out int decodedSize)
        {
            static bool IsBase64Char(char c) =>
                (c >= '0' && c <= '9') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z') ||
                c == '+' || c == '/' || c == '=';

            int base64chars = 0;
            int precedingWhiteSpace = 0;
            int trailingWhiteSpace = 0;
            (int offset, int length) = content.GetOffsetAndLength(data.Length);
            for (int index = offset; index < offset + length; index++)
            {
                char c = data[index];

                if (IsBase64Char(c))
                {
                    trailingWhiteSpace = 0;
                    base64chars++;
                }
                else if (c == ' ' || c == '\n' || c == '\r' ||
                         c == '\t' || c == '\v')
                {
                    if (base64chars == 0)
                    {
                        precedingWhiteSpace++;
                    }
                    else
                    {
                        trailingWhiteSpace++;
                    }
                }
                else
                {
                    base64range = default;
                    decodedSize = 0;
                    return false;
                }
            }

            base64range = (offset + precedingWhiteSpace)..(offset + (length - trailingWhiteSpace));
            decodedSize = (base64chars * 3) / 4;
            return true;
        }

        private static bool TryReadNextLine(ReadOnlySpan<char> data, ref int position, out Range nextLineContent)
        {
            if (position < 0)
            {
                nextLineContent = default;
                return false;
            }

            int newLineIndex = data[position..].IndexOfAny('\n', '\r');

            if (newLineIndex == -1)
            {
                nextLineContent = position..;
                position = -1;
                return true;
            }
            else if (data[newLineIndex] == '\r' &&
                     newLineIndex < data.Length - 1 &&
                     data[newLineIndex + 1] == '\n')
            {
                // We landed at a Windows new line, we should consume both the \r and \n.
                nextLineContent = position..(position + newLineIndex);
                position += newLineIndex + 2;
                return true;
            }
            else
            {
                nextLineContent = position..(position + newLineIndex);
                position += newLineIndex + 1;
                return true;
            }
        }

        public static int GetEncodedSize(int labelLength, int dataLength) =>
            throw new System.NotImplementedException();

        public static bool TryWrite(ReadOnlySpan<char> label, ReadOnlySpan<byte> data, Span<char> destination, out int charsWritten) =>
            throw new System.NotImplementedException();
        public static char[] Write(ReadOnlySpan<char> label, ReadOnlySpan<byte> data) =>
             throw new System.NotImplementedException();
    }
}
