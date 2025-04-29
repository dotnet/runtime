// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

#if NET
#error This PemEncoding implementation is intended to be used only from downlevel targets. .NET should use the in-box implementation.
#endif

namespace System.Security.Cryptography
{
    // Downlevel implementation of PemEncoding. This was originally taken from the .NET 5 implementation of PemEncoding
    // https://github.com/dotnet/runtime/blob/4aadfea70082ae23e6c54a449268341e9429434e/src/libraries/System.Security.Cryptography.Encoding/src/System/Security/Cryptography/PemEncoding.cs
    internal static class PemEncoding
    {
        private const string PreEBPrefix = "-----BEGIN ";
        private const string PostEBPrefix = "-----END ";
        private const string Ending = "-----";

        internal static PemFields Find(ReadOnlySpan<char> pemData)
        {
            if (!TryFind(pemData, out PemFields fields))
            {
                throw new ArgumentException(SR.Argument_PemEncoding_NoPemFound, nameof(pemData));
            }

            return fields;
        }

        internal static bool TryFind(ReadOnlySpan<char> pemData, out PemFields fields)
        {
            // Check for the minimum possible encoded length of a PEM structure
            // and exit early if there is no way the input could contain a well-formed
            // PEM.
            if (pemData.Length < PreEBPrefix.Length + Ending.Length * 2 + PostEBPrefix.Length)
            {
                fields = default;
                return false;
            }

            const int PostebStackBufferSize = 256;
            Span<char> postebStackBuffer = stackalloc char[PostebStackBufferSize];
            int areaOffset = 0;
            int preebIndex;
            while ((preebIndex = pemData.IndexOfByOffset(PreEBPrefix, areaOffset)) >= 0)
            {
                int labelStartIndex = preebIndex + PreEBPrefix.Length;

                // If there are any previous characters, the one prior to the PreEB
                // must be a white space character.
                if (preebIndex > 0 && !IsWhiteSpaceCharacter(pemData[preebIndex - 1]))
                {
                    areaOffset = labelStartIndex;
                    continue;
                }

                int preebEndIndex = pemData.IndexOfByOffset(Ending, labelStartIndex);

                // There is no ending sequence, -----, in the remainder of
                // the document. Therefore, there can never be a complete PreEB
                // and we can exit.
                if (preebEndIndex < 0)
                {
                    fields = default;
                    return false;
                }

                Range labelRange = labelStartIndex..preebEndIndex;
                ReadOnlySpan<char> label = pemData[labelRange];

                // There could be a preeb that is valid after this one if it has an invalid
                // label, so move from there.
                if (!IsValidLabel(label))
                {
                    goto NextAfterLabel;
                }

                int contentStartIndex = preebEndIndex + Ending.Length;
                int postebLength = PostEBPrefix.Length + label.Length + Ending.Length;

                Span<char> postebBuffer = postebLength > PostebStackBufferSize
                    ? new char[postebLength]
                    : postebStackBuffer;
                ReadOnlySpan<char> posteb = WritePostEB(label, postebBuffer);
                int postebStartIndex = pemData.IndexOfByOffset(posteb, contentStartIndex);

                if (postebStartIndex < 0)
                {
                    goto NextAfterLabel;
                }

                int pemEndIndex = postebStartIndex + postebLength;

                // The PostEB must either end at the end of the string, or
                // have at least one white space character after it.
                if (pemEndIndex < pemData.Length - 1 &&
                    !IsWhiteSpaceCharacter(pemData[pemEndIndex]))
                {
                    goto NextAfterLabel;
                }

                Range contentRange = contentStartIndex..postebStartIndex;

                if (!TryCountBase64(pemData[contentRange], out int base64start, out int base64end, out int decodedSize))
                {
                    goto NextAfterLabel;
                }

                Range pemRange = preebIndex..pemEndIndex;
                Range base64range = (contentStartIndex + base64start)..(contentStartIndex + base64end);
                fields = new PemFields(labelRange, base64range, pemRange, decodedSize);
                return true;

                NextAfterLabel:
                if (preebEndIndex <= areaOffset)
                {
                    // We somehow ended up in a situation where we will advance
                    // backward or not at all, which means we'll probably end up here again,
                    // advancing backward, in a loop. To avoid getting stuck,
                    // detect this situation and return.
                    fields = default;
                    return false;
                }
                areaOffset = preebEndIndex;
            }

            fields = default;
            return false;

            static ReadOnlySpan<char> WritePostEB(ReadOnlySpan<char> label, Span<char> destination)
            {
                int size = PostEBPrefix.Length + label.Length + Ending.Length;
                Debug.Assert(destination.Length >= size);
                PostEBPrefix.AsSpan().CopyTo(destination);
                label.CopyTo(destination.Slice(PostEBPrefix.Length));
                Ending.AsSpan().CopyTo(destination.Slice(PostEBPrefix.Length + label.Length));
                return destination.Slice(0, size);
            }
        }

        private static int IndexOfByOffset(this ReadOnlySpan<char> str, ReadOnlySpan<char> value, int startPosition)
        {
            Debug.Assert(startPosition <= str.Length);
            int index = str.Slice(startPosition).IndexOf(value);
            return index == -1 ? -1 : index + startPosition;
        }

        private static bool IsValidLabel(ReadOnlySpan<char> data)
        {
            static bool IsLabelChar(char c) => (uint)(c - 0x21u) <= 0x5du && c != '-';

            // Empty labels are permitted per RFC 7468.
            if (data.IsEmpty)
                return true;

            // The first character must be a labelchar, so initialize to false
            bool previousIsLabelChar = false;

            for (int index = 0; index < data.Length; index++)
            {
                char c = data[index];

                if (IsLabelChar(c))
                {
                    previousIsLabelChar = true;
                    continue;
                }

                bool isSpaceOrHyphen = c == ' ' || c == '-';

                // IETF RFC 7468 states that every character in a label must
                // be a labelchar, and each labelchar may have zero or one
                // preceding space or hyphen, except the first labelchar.
                // If this character is not a space or hyphen, then this characer
                // is invalid.
                // If it is a space or hyphen, and the previous character was
                // also not a labelchar (another hyphen or space), then we have
                // two consecutive spaces or hyphens which is is invalid.
                if (!isSpaceOrHyphen || !previousIsLabelChar)
                {
                    return false;
                }

                previousIsLabelChar = false;
            }

            // The last character must also be a labelchar. It cannot be a
            // hyphen or space since these are only allowed to precede
            // a labelchar.
            return previousIsLabelChar;
        }

        private static bool TryCountBase64(
            ReadOnlySpan<char> str,
            out int base64Start,
            out int base64End,
            out int base64DecodedSize)
        {
            base64Start = 0;
            base64End = str.Length;

            if (str.IsEmpty)
            {
                base64DecodedSize = 0;
                return true;
            }

            int significantCharacters = 0;
            int paddingCharacters = 0;

            for (int i = 0; i < str.Length; i++)
            {
                char ch = str[i];

                if (IsWhiteSpaceCharacter(ch))
                {
                    if (significantCharacters == 0)
                    {
                        base64Start++;
                    }
                    else
                    {
                        base64End--;
                    }

                    continue;
                }

                base64End = str.Length;

                if (ch == '=')
                {
                    paddingCharacters++;
                }
                else if (paddingCharacters == 0 && IsBase64Character(ch))
                {
                    significantCharacters++;
                }
                else
                {
                    base64DecodedSize = 0;
                    return false;
                }
            }

            int totalChars = paddingCharacters + significantCharacters;

            if (paddingCharacters > 2 || (totalChars & 0b11) != 0)
            {
                base64DecodedSize = 0;
                return false;
            }

            base64DecodedSize = (totalChars >> 2) * 3 - paddingCharacters;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsBase64Character(char ch)
        {
            uint c = (uint)ch;
            return c == '+' || c == '/' ||
                   c - '0' < 10 || c - 'A' < 26 || c - 'a' < 26;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsWhiteSpaceCharacter(char ch)
        {
            // Match white space characters from Convert.Base64
            return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';
        }
    }

    internal readonly struct PemFields
    {
        internal PemFields(Range label, Range base64data, Range location, int decodedDataLength)
        {
            Location = location;
            DecodedDataLength = decodedDataLength;
            Base64Data = base64data;
            Label = label;
        }

        public Range Location { get; }
        public Range Label { get; }
        public Range Base64Data { get; }
        public int DecodedDataLength { get; }
    }
}
