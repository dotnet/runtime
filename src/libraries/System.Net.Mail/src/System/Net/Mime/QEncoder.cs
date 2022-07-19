// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Mime
{
    internal sealed class QEncoder : ByteEncoder
    {
        private const int SizeOfQEncodedChar = 3; // e.g. "=3A"

        private readonly WriteStateInfoBase _writeState;

        internal override WriteStateInfoBase WriteState => _writeState;

        protected override bool HasSpecialEncodingForCRLF => true;

        internal QEncoder(WriteStateInfoBase wsi)
        {
            _writeState = wsi;
        }

        protected override void AppendEncodedCRLF()
        {
            //the encoding for CRLF is =0D=0A
            WriteState.Append("=0D=0A"u8);
        }

        protected override bool LineBreakNeeded(byte b)
        {
            // Fold if we're before a whitespace and encoding another character would be too long
            int lengthAfterAddingCharAndFooter = WriteState.CurrentLineLength + SizeOfQEncodedChar + WriteState.FooterLength;
            bool isWhitespace = b == ' ' || b == '\t' || b == '\r' || b == '\n';
            if (lengthAfterAddingCharAndFooter >= WriteState.MaxLineLength && isWhitespace)
            {
                return true;
            }

            // Or just adding the footer would be too long.
            int lengthAfterAddingFooter = WriteState.CurrentLineLength + WriteState.FooterLength;
            if (lengthAfterAddingFooter >= WriteState.MaxLineLength)
            {
                return true;
            }

            return false;
        }

        protected override bool LineBreakNeeded(byte[] bytes, int count)
        {
            if (count == 1 || IsCRLF(bytes, count)) // preserve same behavior as in EncodeBytes
            {
                return LineBreakNeeded(bytes[0]);
            }

            int numberOfCharsToAppend = count * SizeOfQEncodedChar;
            return WriteState.CurrentLineLength + numberOfCharsToAppend + _writeState.FooterLength > WriteState.MaxLineLength;
        }

        protected override int GetCodepointSize(string value, int i)
        {
            // specific encoding for CRLF
            if (value[i] == '\r' && i + 1 < value.Length && value[i + 1] == '\n')
            {
                return 2;
            }

            if (IsSurrogatePair(value, i))
            {
                return 2;
            }

            return 1;
        }

        // no padding in q-encoding
        public override void AppendPadding() { }

        protected override void ApppendEncodedByte(byte b)
        {
            if (b == ' ')
            {
                //spaces should be escaped as either '_' or '=20' and
                //we have chosen '_' for parity with other email client
                //behavior
                WriteState.Append((byte)'_');
            }
            // RFC 2047 Section 5 part 3 also allows for !*+-/ but these arn't required in headers.
            // Conservatively encode anything but letters or digits.
            else if (char.IsAsciiLetterOrDigit((char)b))
            {
                // Just a regular printable ascii char.
                WriteState.Append(b);
            }
            else
            {
                //append an = to indicate an encoded character
                WriteState.Append((byte)'=');
                //shift 4 to get the first four bytes only and look up the hex digit
                WriteState.Append((byte)HexConverter.ToCharUpper(b >> 4));
                //clear the first four bytes to get the last four and look up the hex digit
                WriteState.Append((byte)HexConverter.ToCharUpper(b));
            }
        }
    }
}
