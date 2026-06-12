// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System.Net
{
    // Represents a domain name in DNS wire format (RFC 1035 §4.1.4).
    // Works for both the read path (responses with compression pointers) and the
    // write path (flat encoded names).
    internal readonly ref struct DnsEncodedName
    {
        private static readonly IdnMapping s_idnMapping = new IdnMapping { AllowUnassigned = false, UseStd3AsciiRules = true };

        // Maximum wire-format size of any valid domain name (including length
        // prefixes and the root label terminator).
        public const int MaxEncodedLength = 255;

        // The buffer containing the encoded name. For names parsed from responses,
        // this is the full message (needed to follow compression pointers). For
        // names created via TryEncode, this is the flat encoded buffer.
        private readonly ReadOnlySpan<byte> _buffer;

        // Offset within _buffer where this name starts.
        private readonly int _offset;

        // Whether any label is ACE-encoded (starts with "xn--"), indicating IDN/Punycode.
        private readonly bool _isAce;

        // Whether the wire encoding contains compression pointers.
        // False for names created via TryEncode (always flat).
        private readonly bool _hasPointers;

        internal DnsEncodedName(ReadOnlySpan<byte> buffer, int offset, bool isAce, bool hasPointers)
        {
            _buffer = buffer;
            _offset = offset;
            _isAce = isAce;
            _hasPointers = hasPointers;
        }

        // Attempts to parse a DNS name from a wire-format buffer at the given offset.
        // Validates that the name is well-formed (valid label lengths, no truncation).
        // The buffer is retained by the returned DnsEncodedName to support compression
        // pointer resolution. bytesConsumed receives the number of bytes consumed from
        // the buffer at offset (not following compression pointers).
        public static bool TryParse(ReadOnlySpan<byte> buffer, int offset, out DnsEncodedName name, out int bytesConsumed)
        {
            name = default;
            bytesConsumed = 0;

            if (offset < 0 || offset >= buffer.Length)
            {
                return false;
            }

            if (!ValidateName(buffer, offset, out int wireLen, out _, out bool isAce, out bool hasPointers))
            {
                return false;
            }

            if (!hasPointers)
            {
                // Non-pointer names: _buffer is sliced to exactly the encoded bytes.
                name = new DnsEncodedName(buffer[offset..(offset + wireLen)], 0, isAce, hasPointers: false);
            }
            else
            {
                // Pointer names: full message buffer needed for pointer resolution.
                name = new DnsEncodedName(buffer, offset, isAce, hasPointers: true);
            }
            bytesConsumed = wireLen;
            return true;
        }

        // Validates a domain name and encodes it into wire format.
        public static OperationStatus TryEncode(
            ReadOnlySpan<char> name,
            Span<byte> destination,
            out DnsEncodedName result,
            out int bytesWritten)
        {
            result = default;
            bytesWritten = 0;

            // Handle root name "." or empty string.
            if (name.Length == 0 || (name.Length == 1 && name[0] == '.'))
            {
                if (destination.Length < 1)
                {
                    return OperationStatus.DestinationTooSmall;
                }
                destination[0] = 0; // root label
                bytesWritten = 1;
                result = new DnsEncodedName(destination[..1], 0, isAce: false, hasPointers: false);
                return OperationStatus.Done;
            }

            // If the name contains non-ASCII characters, convert to ACE (Punycode)
            // form per RFC 5891 (IDNA 2008) before wire encoding.
            string? aceName = null;
            if (!Ascii.IsValid(name))
            {
                try
                {
                    aceName = s_idnMapping.GetAscii(name.ToString());
                }
                catch (ArgumentException)
                {
                    return OperationStatus.InvalidData;
                }
                name = aceName;
            }

            // Strip trailing dot if present (FQDN notation).
            if (name[^1] == '.')
            {
                name = name[..^1];
            }

            // Wire format length: each '.' becomes a length byte, plus one leading
            // length byte and trailing root label.
            int wireLen = name.Length + 2;
            if (wireLen > MaxEncodedLength)
            {
                return OperationStatus.InvalidData; // name too long
            }
            if (wireLen > destination.Length)
            {
                return OperationStatus.DestinationTooSmall;
            }

            // Copy the ASCII name at offset 1, so dots land where length prefixes will go.
            OperationStatus asciiStatus = Ascii.FromUtf16(name, destination.Slice(1, name.Length), out _);
            Debug.Assert(asciiStatus == OperationStatus.Done);

            // Walk through and replace dots with label lengths, validating labels.
            Span<byte> body = destination.Slice(1, name.Length);
            int labelStart = 0;
            bool isAce = aceName != null;
            while (true)
            {
                int dotIdx = body[labelStart..].IndexOf((byte)'.');
                int labelLen = dotIdx >= 0 ? dotIdx : body.Length - labelStart;

                Span<byte> label = body.Slice(labelStart, labelLen);
                if (!IsValidLabel(label))
                {
                    return OperationStatus.InvalidData;
                }

                if (!isAce && labelLen >= 4)
                {
                    isAce = IsAceLabel(label);
                }

                // Overwrite the dot (or the leading slot at destination[0]) with the label length.
                destination[labelStart] = (byte)labelLen;

                if (dotIdx < 0)
                {
                    break;
                }

                labelStart += labelLen + 1;
            }

            // Write root (empty) label.
            destination[wireLen - 1] = 0;

            bytesWritten = wireLen;
            result = new DnsEncodedName(destination[..wireLen], 0, isAce, hasPointers: false);
            return OperationStatus.Done;
        }

        // Compares this name to a dotted string representation. Case-insensitive.
        // Non-ASCII (Unicode) names are converted to ACE form before comparison.
        public bool Equals(ReadOnlySpan<char> name)
        {
            if (!Ascii.IsValid(name))
            {
                try
                {
                    name = s_idnMapping.GetAscii(name.ToString());
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            // Strip trailing dot from the comparison name.
            if (name.Length > 0 && name[^1] == '.')
            {
                name = name[..^1];
            }

            DnsLabelEnumerator enumerator = EnumerateLabels();
            int nameIdx = 0;

            while (enumerator.MoveNext())
            {
                ReadOnlySpan<byte> label = enumerator.Current;

                if (nameIdx > 0)
                {
                    // Expect a dot separator.
                    if (nameIdx >= name.Length || name[nameIdx] != '.')
                    {
                        return false;
                    }
                    nameIdx++;
                }

                if (nameIdx + label.Length > name.Length)
                {
                    return false;
                }

                if (!Ascii.EqualsIgnoreCase(label, name.Slice(nameIdx, label.Length)))
                {
                    return false;
                }
                nameIdx += label.Length;
            }

            return nameIdx == name.Length;
        }

        // Decodes the domain name into the destination buffer as a dotted string.
        // ACE-encoded labels (starting with "xn--") are converted back to Unicode.
        public unsafe bool TryDecode(Span<char> destination, out int charsWritten)
        {
            charsWritten = 0;

            if (!_isAce)
            {
                // Fast path for non-ACE names: decode directly to destination.
                return TryDecodeAscii(destination, out charsWritten);
            }

            // For ACE names, the ASCII intermediate may be longer than the final
            // Unicode form. Decode to a local buffer first, then convert.
            Span<char> ascii = stackalloc char[256];
            if (!TryDecodeAscii(ascii, out int asciiWritten))
            {
                return false;
            }

            try
            {
                string unicode = s_idnMapping.GetUnicode(new string(ascii[..asciiWritten]));
                if (unicode.Length <= destination.Length)
                {
                    unicode.AsSpan().CopyTo(destination);
                    charsWritten = unicode.Length;
                    return true;
                }
            }
            catch (ArgumentException)
            {
                // IDN conversion failed, fall through to ACE form.
            }

            if (asciiWritten <= destination.Length)
            {
                ascii[..asciiWritten].CopyTo(destination);
                charsWritten = asciiWritten;
                return true;
            }

            return false;
        }

        private static bool IsAceLabel(ReadOnlySpan<byte> label)
        {
            return label.Length >= 4 &&
                   Ascii.EqualsIgnoreCase(label[..4], "xn--"u8);
        }

        // Enumerates the individual labels of this domain name.
        // Follows compression pointers transparently.
        public DnsLabelEnumerator EnumerateLabels() => new DnsLabelEnumerator(_buffer, _offset);

        // Copies the flat wire-format encoding of this name to the destination buffer,
        // expanding compression pointers if present.
        internal bool TryCopyEncodedTo(Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;

            if (!_hasPointers)
            {
                // Fast path: _buffer is sliced to exactly the encoded bytes starting at _offset.
                ReadOnlySpan<byte> encoded = _buffer[_offset..];
                if (encoded.Length > destination.Length)
                {
                    return false;
                }

                encoded.CopyTo(destination);
                bytesWritten = encoded.Length;
                return true;
            }

            // Slow path: expand compression pointers by copying labels as we go.
            // MaxEncodedLength bounds the output, so we won't overrun a properly sized buffer.
            foreach (ReadOnlySpan<byte> label in EnumerateLabels())
            {
                if (bytesWritten + 1 + label.Length > destination.Length)
                {
                    return false;
                }
                destination[bytesWritten] = (byte)label.Length;
                bytesWritten++;
                label.CopyTo(destination[bytesWritten..]);
                bytesWritten += label.Length;
            }

            if (bytesWritten >= destination.Length)
            {
                return false;
            }
            destination[bytesWritten] = 0; // root label
            bytesWritten++;

            return true;
        }

        public override unsafe string ToString()
        {
            Span<char> chars = stackalloc char[256];
            bool success = TryDecode(chars, out int charsWritten);
            Debug.Assert(success);
            return new string(chars[..charsWritten]);
        }

        // Decodes the domain name as raw ASCII without IDN conversion.
        private bool TryDecodeAscii(Span<char> destination, out int charsWritten)
        {
            charsWritten = 0;
            DnsLabelEnumerator enumerator = EnumerateLabels();
            bool first = true;

            while (enumerator.MoveNext())
            {
                ReadOnlySpan<byte> label = enumerator.Current;

                if (!first)
                {
                    if (charsWritten >= destination.Length)
                    {
                        return false;
                    }
                    destination[charsWritten] = '.';
                    charsWritten++;
                }
                first = false;

                if (charsWritten + label.Length > destination.Length)
                {
                    return false;
                }

                Ascii.ToUtf16(label, destination.Slice(charsWritten, label.Length), out _);
                charsWritten += label.Length;
            }

            if (charsWritten == 0)
            {
                // Root name produces "." in dotted form.
                if (destination.Length < 1)
                {
                    return false;
                }
                destination[0] = '.';
                charsWritten = 1;
            }

            return true;
        }

        // Validates the name and computes the wire-format byte count, the dotted ASCII
        // string length, and whether any label is ACE-encoded or uses compression pointers,
        // all in a single pass. Returns false if the name is malformed or exceeds RFC 1035 limits.
        // When validateContent is false (response parsing), only structural validation is
        // performed (label lengths, pointer safety, total length). When true (outbound
        // encoding), label content is also validated for LDH compliance.
        private static bool ValidateName(ReadOnlySpan<byte> buffer, int offset,
            out int wireLength, out int formattedLength, out bool isAce,
            out bool hasPointers, bool validateContent = false)
        {
            wireLength = 0;
            formattedLength = 0;
            isAce = false;
            hasPointers = false;

            int pos = offset;
            bool foundWireEnd = false;
            int hops = 0;

            while (pos < buffer.Length)
            {
                byte b = buffer[pos];

                if (b == 0)
                {
                    // Root label — end of name.
                    if (!foundWireEnd)
                    {
                        wireLength = pos + 1 - offset;
                    }
                    return true;
                }

                if ((b & 0xC0) == 0xC0)
                {
                    // Compression pointer.
                    if (pos + 1 >= buffer.Length)
                    {
                        return false; // truncated pointer
                    }

                    if (!foundWireEnd)
                    {
                        wireLength = pos + 2 - offset;
                        foundWireEnd = true;
                        hasPointers = true;
                    }

                    int pointer = ((b & 0x3F) << 8) | buffer[pos + 1];
                    if (pointer >= pos)
                    {
                        return false; // only backwards jumps allowed
                    }
                    pos = pointer;

                    if (++hops > 16)
                    {
                        return false; // too many pointer hops
                    }
                    continue;
                }

                if ((b & 0xC0) != 0x00)
                {
                    return false; // one of the upper 2 bits is nonzero, invalid per RFC 1035
                }
                Debug.Assert(b <= 63); // enforced by condition above

                if (pos + 1 + b > buffer.Length)
                {
                    return false; // label extends past buffer
                }

                // Account for dot separator in formatted length.
                formattedLength += formattedLength > 0 ? b + 1 : b;
                if (formattedLength > 253)
                {
                    return false; // RFC 1035: max 253 characters in dotted form
                }

                // Check for ACE label ("xn--" prefix).
                ReadOnlySpan<byte> label = buffer.Slice(pos + 1, b);
                if (!isAce && b >= 4)
                {
                    isAce = IsAceLabel(label);
                }

                // Validate label contents when required (outbound encoding).
                if (validateContent && !IsValidLabel(label))
                {
                    return false;
                }

                pos += 1 + b; // skip length byte + label
            }

            return false; // ran off the end of buffer without finding root label
        }

        private static readonly SearchValues<byte> s_ldhBytes =
            SearchValues.Create("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_"u8);

        // Validates that a label has valid length (1-63), contains only LDH (Letters,
        // Digits, Hyphens) characters and underscores (for SRV, DKIM, etc.), and does
        // not start or end with a hyphen.
        private static bool IsValidLabel(ReadOnlySpan<byte> label)
        {
            return label.Length > 0 &&
                   label.Length <= 63 &&
                   label[0] != (byte)'-' &&
                   label[^1] != (byte)'-' &&
                   label.IndexOfAnyExcept(s_ldhBytes) < 0;
        }
    }

    // Enumerates labels of a DNS name, following compression pointers. The name must
    // have been validated by DnsEncodedName.TryParse or DnsEncodedName.TryEncode before
    // enumeration.
    internal ref struct DnsLabelEnumerator
    {
        private readonly ReadOnlySpan<byte> _buffer;
        private int _pos;
        private ReadOnlySpan<byte> _current;

        internal DnsLabelEnumerator(ReadOnlySpan<byte> buffer, int offset)
        {
            _buffer = buffer;
            _pos = offset;
            _current = default;
        }

        public readonly ReadOnlySpan<byte> Current => _current;

        public bool MoveNext()
        {
            byte b = _buffer[_pos];

            while ((b & 0xC0) == 0xC0)
            {
                // Compression pointer: follow it.
                Debug.Assert(_pos + 1 < _buffer.Length, "Truncated compression pointer");
                int pointer = ((b & 0x3F) << 8) | _buffer[_pos + 1];
                Debug.Assert(pointer < _pos, "Forward or self-referencing compression pointer");
                _pos = pointer;
                b = _buffer[_pos];
            }

            if (b == 0)
            {
                // End, root label.
                return false;
            }

            Debug.Assert(b <= 63, "Invalid label length byte");
            int labelLen = b;
            _pos++;
            Debug.Assert(_pos + labelLen <= _buffer.Length, "Label extends past buffer");
            _current = _buffer.Slice(_pos, labelLen);
            _pos += labelLen;
            return true;
        }

        public readonly DnsLabelEnumerator GetEnumerator() => this;
    }
}
