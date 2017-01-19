// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Diagnostics;

namespace System.Net.Mime
{
    /// <summary>
    /// This stream performs in-place decoding of quoted-printable
    /// encoded streams.  Encoding requires copying into a separate
    /// buffer as the data being encoded will most likely grow.
    /// Encoding and decoding is done transparently to the caller.
    /// </summary>
    internal class QEncodedStream : DelegatedStream, IEncodableStream
    {
        //folding takes up 3 characters "\r\n "
        private const int SizeOfFoldingCRLF = 3;

        private static readonly byte[] s_hexDecodeMap = new byte[]
        {
            // 0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 0
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 1
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 2
             0,  1,  2,  3,  4,  5,  6,  7,  8,  9,255,255,255,255,255,255, // 3
             255, 10, 11, 12, 13, 14, 15,255,255,255,255,255,255,255,255,255, // 4
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 5
             255, 10, 11, 12, 13, 14, 15,255,255,255,255,255,255,255,255,255, // 6
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 7
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 8
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // 9
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // A
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // B
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // C
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // D
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // E
             255,255,255,255,255,255,255,255,255,255,255,255,255,255,255,255, // F
        };

        //bytes that correspond to the hex char representations in ASCII (0-9, A-F)
        private static readonly byte[] s_hexEncodeMap = new byte[] { 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 65, 66, 67, 68, 69, 70 };

        private ReadStateInfo _readState;
        private WriteStateInfoBase _writeState;

        internal QEncodedStream(WriteStateInfoBase wsi) : base(new MemoryStream())
        {
            _writeState = wsi;
        }

        private ReadStateInfo ReadState => _readState ?? (_readState = new ReadStateInfo());

        internal WriteStateInfoBase WriteState => _writeState;

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            WriteAsyncResult result = new WriteAsyncResult(this, buffer, offset, count, callback, state);
            result.Write();
            return result;
        }

        public override void Close()
        {
            FlushInternal();
            base.Close();
        }

        public unsafe int DecodeBytes(byte[] buffer, int offset, int count)
        {
            fixed (byte* pBuffer = buffer)
            {
                byte* start = pBuffer + offset;
                byte* source = start;
                byte* dest = start;
                byte* end = start + count;

                // if the last read ended in a partially decoded
                // sequence, pick up where we left off.
                if (ReadState.IsEscaped)
                {
                    // this will be -1 if the previous read ended
                    // with an escape character.
                    if (ReadState.Byte == -1)
                    {
                        // if we only read one byte from the underlying
                        // stream, we'll need to save the byte and
                        // ask for more.
                        if (count == 1)
                        {
                            ReadState.Byte = *source;
                            return 0;
                        }

                        // '=\r\n' means a soft (aka. invisible) CRLF sequence...
                        if (source[0] != '\r' || source[1] != '\n')
                        {
                            byte b1 = s_hexDecodeMap[source[0]];
                            byte b2 = s_hexDecodeMap[source[1]];
                            if (b1 == 255)
                                throw new FormatException(SR.Format(SR.InvalidHexDigit, b1));
                            if (b2 == 255)
                                throw new FormatException(SR.Format(SR.InvalidHexDigit, b2));

                            *dest++ = (byte)((b1 << 4) + b2);
                        }

                        source += 2;
                    }
                    else
                    {
                        // '=\r\n' means a soft (aka. invisible) CRLF sequence...
                        if (ReadState.Byte != '\r' || *source != '\n')
                        {
                            byte b1 = s_hexDecodeMap[ReadState.Byte];
                            byte b2 = s_hexDecodeMap[*source];
                            if (b1 == 255)
                                throw new FormatException(SR.Format(SR.InvalidHexDigit, b1));
                            if (b2 == 255)
                                throw new FormatException(SR.Format(SR.InvalidHexDigit, b2));
                            *dest++ = (byte)((b1 << 4) + b2);
                        }
                        source++;
                    }
                    // reset state for next read.
                    ReadState.IsEscaped = false;
                    ReadState.Byte = -1;
                }

                // Here's where most of the decoding takes place.
                // We'll loop around until we've inspected all the
                // bytes read.
                while (source < end)
                {
                    // if the source is not an escape character, then
                    // just copy as-is.
                    if (*source != '=')
                    {
                        if (*source == '_')
                        {
                            *dest++ = (byte)' ';
                            source++;
                        }
                        else
                        {
                            *dest++ = *source++;
                        }
                    }
                    else
                    {
                        // determine where we are relative to the end
                        // of the data.  If we don't have enough data to 
                        // decode the escape sequence, save off what we
                        // have and continue the decoding in the next
                        // read.  Otherwise, decode the data and copy
                        // into dest.
                        switch (end - source)
                        {
                            case 2:
                                ReadState.Byte = source[1];
                                goto case 1;
                            case 1:
                                ReadState.IsEscaped = true;
                                goto EndWhile;
                            default:
                                if (source[1] != '\r' || source[2] != '\n')
                                {
                                    byte b1 = s_hexDecodeMap[source[1]];
                                    byte b2 = s_hexDecodeMap[source[2]];
                                    if (b1 == 255)
                                        throw new FormatException(SR.Format(SR.InvalidHexDigit, b1));
                                    if (b2 == 255)
                                        throw new FormatException(SR.Format(SR.InvalidHexDigit, b2));

                                    *dest++ = (byte)((b1 << 4) + b2);
                                }
                                source += 3;
                                break;
                        }
                    }
                }
            EndWhile:
                return (int)(dest - start);
            }
        }

        public int EncodeBytes(byte[] buffer, int offset, int count)
        {
            // Add Encoding header, if any. e.g. =?encoding?b?
            _writeState.AppendHeader();

            // Scan one character at a time looking for chars that need to be encoded.
            int cur = offset;
            for (; cur < count + offset; cur++)
            {
                if ( // Fold if we're before a whitespace and encoding another character would be too long
                    ((WriteState.CurrentLineLength + SizeOfFoldingCRLF + WriteState.FooterLength >= WriteState.MaxLineLength)
                        && (buffer[cur] == ' ' || buffer[cur] == '\t' || buffer[cur] == '\r' || buffer[cur] == '\n'))
                    // Or just adding the footer would be too long.
                    || (WriteState.CurrentLineLength + _writeState.FooterLength >= WriteState.MaxLineLength)
                   )
                {
                    WriteState.AppendCRLF(true);
                }

                // We don't need to worry about RFC 2821 4.5.2 (encoding first dot on a line),
                // it is done by the underlying 7BitStream

                //always encode CRLF
                if (buffer[cur] == '\r' && cur + 1 < count + offset && buffer[cur + 1] == '\n')
                {
                    cur++;

                    //the encoding for CRLF is =0D=0A
                    WriteState.Append((byte)'=', (byte)'0', (byte)'D', (byte)'=', (byte)'0', (byte)'A');
                }
                else if (buffer[cur] == ' ')
                {
                    //spaces should be escaped as either '_' or '=20' and
                    //we have chosen '_' for parity with other email client
                    //behavior
                    WriteState.Append((byte)'_');
                }
                // RFC 2047 Section 5 part 3 also allows for !*+-/ but these arn't required in headers.
                // Conservatively encode anything but letters or digits.
                else if (IsAsciiLetterOrDigit((char)buffer[cur]))
                {
                    // Just a regular printable ascii char.
                    WriteState.Append(buffer[cur]);
                }
                else
                {
                    //append an = to indicate an encoded character
                    WriteState.Append((byte)'=');
                    //shift 4 to get the first four bytes only and look up the hex digit
                    WriteState.Append(s_hexEncodeMap[buffer[cur] >> 4]);
                    //clear the first four bytes to get the last four and look up the hex digit
                    WriteState.Append(s_hexEncodeMap[buffer[cur] & 0xF]);
                }
            }
            WriteState.AppendFooter();
            return cur - offset;
        }

        private static bool IsAsciiLetterOrDigit(char character) =>
            IsAsciiLetter(character) || (character >= '0' && character <= '9');

        private static bool IsAsciiLetter(char character) =>
            (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z');

        public Stream GetStream() => this;

        public string GetEncodedString() => Encoding.ASCII.GetString(WriteState.Buffer, 0, WriteState.Length);

        public override void EndWrite(IAsyncResult asyncResult) => WriteAsyncResult.End(asyncResult);

        public override void Flush()
        {
            FlushInternal();
            base.Flush();
        }

        private void FlushInternal()
        {
            if (_writeState != null && _writeState.Length > 0)
            {
                base.Write(WriteState.Buffer, 0, WriteState.Length);
                WriteState.Reset();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            int written = 0;
            for (;;)
            {
                written += EncodeBytes(buffer, offset + written, count - written);
                if (written < count)
                {
                    FlushInternal();
                }
                else
                {
                    break;
                }
            }
        }

        private sealed class ReadStateInfo
        {
            internal bool IsEscaped { get; set; }
            internal short Byte { get; set; } = -1;
        }

        private class WriteAsyncResult : LazyAsyncResult
        {
            private static readonly AsyncCallback s_onWrite = OnWrite;

            private readonly QEncodedStream _parent;
            private readonly byte[] _buffer;
            private readonly int _offset;
            private readonly int _count;

            private int _written;

            internal WriteAsyncResult(QEncodedStream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state)
                : base(null, state, callback)
            {
                _parent = parent;
                _buffer = buffer;
                _offset = offset;
                _count = count;
            }

            private void CompleteWrite(IAsyncResult result)
            {
                _parent.BaseStream.EndWrite(result);
                _parent.WriteState.Reset();
            }

            internal static void End(IAsyncResult result)
            {
                WriteAsyncResult thisPtr = (WriteAsyncResult)result;
                thisPtr.InternalWaitForCompletion();
                Debug.Assert(thisPtr._written == thisPtr._count);
            }

            private static void OnWrite(IAsyncResult result)
            {
                if (!result.CompletedSynchronously)
                {
                    WriteAsyncResult thisPtr = (WriteAsyncResult)result.AsyncState;
                    try
                    {
                        thisPtr.CompleteWrite(result);
                        thisPtr.Write();
                    }
                    catch (Exception e)
                    {
                        thisPtr.InvokeCallback(e);
                    }
                }
            }

            internal void Write()
            {
                for (;;)
                {
                    _written += _parent.EncodeBytes(_buffer, _offset + _written, _count - _written);
                    if (_written < _count)
                    {
                        IAsyncResult result = _parent.BaseStream.BeginWrite(_parent.WriteState.Buffer, 0, _parent.WriteState.Length, s_onWrite, this);
                        if (!result.CompletedSynchronously)
                        {
                            break;
                        }
                        CompleteWrite(result);
                    }
                    else
                    {
                        InvokeCallback();
                        break;
                    }
                }
            }
        }
    }
}
