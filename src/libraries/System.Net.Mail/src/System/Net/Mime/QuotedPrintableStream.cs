// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mime
{
    /// <summary>
    /// This stream performs in-place decoding of quoted-printable
    /// encoded streams.  Encoding requires copying into a separate
    /// buffer as the data being encoded will most likely grow.
    /// Encoding and decoding is done transparently to the caller.
    ///
    /// This stream should only be used for the e-mail content.
    /// Use QEncodedStream for encoding headers.
    /// </summary>
    internal sealed class QuotedPrintableStream : DelegatedStream, IEncodableStream
    {
        //should we encode CRLF or not?
        private readonly bool _encodeCRLF;

        //number of bytes needed for a soft CRLF in folding
        private const int SizeOfSoftCRLF = 3;

        //each encoded byte occupies three bytes when encoded
        private const int SizeOfEncodedChar = 3;

        //it takes six bytes to encode a CRLF character (a CRLF that does not indicate folding)
        private const int SizeOfEncodedCRLF = 6;

        //if we aren't encoding CRLF then it occupies two chars
        private const int SizeOfNonEncodedCRLF = 2;

        private static ReadOnlySpan<byte> HexDecodeMap =>
        [
            // 0   1   2   3   4   5   6   7   8   9   A   B   C   D   E   F
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // 0
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // 1
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // 2
             0,   1,   2,   3,   4,   5,   6,   7,   8,   9,   255, 255, 255, 255, 255, 255, // 3
             255, 10,  11,  12,  13,  14,  15,  255, 255, 255, 255, 255, 255, 255, 255, 255, // 4
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // 5
             255, 10,  11,  12,  13,  14,  15,  255, 255, 255, 255, 255, 255, 255, 255, 255, // 6
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // 7
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // 8
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // 9
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // A
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // B
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // C
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // D
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // E
             255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, // F
        ];

        private static ReadOnlySpan<byte> HexEncodeMap => "0123456789ABCDEF"u8;

        private readonly int _lineLength;
        private WriteStateInfoBase? _writeState;

        /// <summary>
        /// ctor.
        /// </summary>
        /// <param name="stream">Underlying stream</param>
        /// <param name="lineLength">Preferred maximum line-length for writes</param>
        internal QuotedPrintableStream(Stream stream, int lineLength) : base(stream)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(lineLength);

            _lineLength = lineLength;
        }

        internal QuotedPrintableStream(Stream stream, bool encodeCRLF) : this(stream, EncodedStreamFactory.DefaultMaxLineLength)
        {
            _encodeCRLF = encodeCRLF;
        }

        public override bool CanRead => false;
        public override bool CanWrite => BaseStream.CanWrite;

        private ReadStateInfo ReadState => field ??= new ReadStateInfo();

        internal WriteStateInfoBase WriteState => _writeState ??= new WriteStateInfoBase(1024, null, null, _lineLength);

        public override void Close()
        {
            FlushInternal();
            base.Close();
        }

        public unsafe int DecodeBytes(Span<byte> buffer)
        {
            fixed (byte* pBuffer = buffer)
            {
                byte* start = pBuffer;
                byte* source = start;
                byte* dest = start;
                byte* end = start + buffer.Length;

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
                        if (buffer.Length == 1)
                        {
                            ReadState.Byte = *source;
                            return 0;
                        }

                        // '=\r\n' means a soft (aka. invisible) CRLF sequence...
                        if (source[0] != '\r' || source[1] != '\n')
                        {
                            byte b1 = HexDecodeMap[source[0]];
                            byte b2 = HexDecodeMap[source[1]];
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
                            byte b1 = HexDecodeMap[ReadState.Byte];
                            byte b2 = HexDecodeMap[*source];
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
                        *dest++ = *source++;
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
                                    byte b1 = HexDecodeMap[source[1]];
                                    byte b2 = HexDecodeMap[source[2]];
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

        public int EncodeBytes(ReadOnlySpan<byte> buffer)
        {
            int processed = 0;
            for (; processed < buffer.Length; processed++)
            {
                //only fold if we're before a whitespace or if we're at the line limit
                //add two to the encoded Byte Length to be conservative so that we guarantee that the line length is acceptable
                if ((_lineLength != -1 && WriteState.CurrentLineLength + SizeOfEncodedChar + 2 >= _lineLength && (buffer[processed] == ' ' ||
                    buffer[processed] == '\t' || buffer[processed] == '\r' || buffer[processed] == '\n')) ||
                    _writeState!.CurrentLineLength + SizeOfEncodedChar + 2 >= EncodedStreamFactory.DefaultMaxLineLength)
                {
                    if (WriteState.Buffer.Length - WriteState.Length < SizeOfSoftCRLF)
                    {
                        return processed;  //ok because folding happens externally
                    }

                    WriteState.Append((byte)'=');
                    WriteState.AppendCRLF(false);
                }

                // We don't need to worry about RFC 2821 4.5.2 (encoding first dot on a line),
                // it is done by the underlying 7BitStream

                //detect a CRLF in the input and encode it.
                if (buffer[processed] == '\r' && processed + 1 < buffer.Length && buffer[processed + 1] == '\n')
                {
                    if (WriteState.Buffer.Length - WriteState.Length < (_encodeCRLF ? SizeOfEncodedCRLF : SizeOfNonEncodedCRLF))
                    {
                        return processed;
                    }
                    processed++;

                    if (_encodeCRLF)
                    {
                        // The encoding for CRLF is =0D=0A
                        WriteState.Append("=0D=0A"u8);
                    }
                    else
                    {
                        WriteState.AppendCRLF(false);
                    }
                }
                //ascii chars less than 32 (control chars) and greater than 126 (non-ascii) are not allowed so we have to encode
                else if ((buffer[processed] < 32 && buffer[processed] != '\t') ||
                    buffer[processed] == '=' ||
                    buffer[processed] > 126)
                {
                    if (WriteState.Buffer.Length - WriteState.Length < SizeOfSoftCRLF)
                    {
                        return processed;
                    }

                    //append an = to indicate an encoded character
                    WriteState.Append((byte)'=');
                    //shift 4 to get the first four bytes only and look up the hex digit
                    WriteState.Append(HexEncodeMap[buffer[processed] >> 4]);
                    //clear the first four bytes to get the last four and look up the hex digit
                    WriteState.Append(HexEncodeMap[buffer[processed] & 0xF]);
                }
                else
                {
                    if (WriteState.Buffer.Length - WriteState.Length < 1)
                    {
                        return processed;
                    }

                    //detect special case:  is whitespace at end of line?  we must encode it if it is
                    if ((buffer[processed] == (byte)'\t' || buffer[processed] == (byte)' ') &&
                        (processed + 1 >= buffer.Length))
                    {
                        if (WriteState.Buffer.Length - WriteState.Length < SizeOfEncodedChar)
                        {
                            return processed;
                        }

                        //append an = to indicate an encoded character
                        WriteState.Append((byte)'=');
                        //shift 4 to get the first four bytes only and look up the hex digit
                        WriteState.Append(HexEncodeMap[buffer[processed] >> 4]);
                        //clear the first four bytes to get the last four and look up the hex digit
                        WriteState.Append(HexEncodeMap[buffer[processed] & 0xF]);
                    }
                    else
                    {
                        WriteState.Append(buffer[processed]);
                    }
                }
            }
            return processed;
        }

        public int EncodeString(string value, Encoding encoding)
        {
            byte[] buffer = encoding.GetBytes(value);
            return EncodeBytes(buffer);
        }

        public string GetEncodedString() => Encoding.ASCII.GetString(WriteState.Buffer, 0, WriteState.Length);

        public override void Flush()
        {
            FlushInternal();
            base.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await FlushInternalAsync(cancellationToken).ConfigureAwait(false);
            await base.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private async ValueTask FlushInternalAsync(CancellationToken cancellationToken)
        {
            if (_writeState != null && _writeState.Length > 0)
            {
                await BaseStream.WriteAsync(WriteState.Buffer.AsMemory(0, WriteState.Length), cancellationToken).ConfigureAwait(false);
                WriteState.BufferFlushed();
            }
        }

        private void FlushInternal()
        {
            if (_writeState != null && _writeState.Length > 0)
            {
                BaseStream.Write(WriteState.Buffer, 0, WriteState.Length);
                WriteState.BufferFlushed();
            }
        }

        protected override int ReadInternal(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }

        protected override ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        protected override void WriteInternal(ReadOnlySpan<byte> buffer)
        {
            int written = 0;
            while (true)
            {
                written += EncodeBytes(buffer.Slice(written));
                if (written < buffer.Length)
                {
                    FlushInternal();
                }
                else
                {
                    break;
                }
            }
        }

        protected override async ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int written = 0;
            while (true)
            {
                written += EncodeBytes(buffer.Span.Slice(written));
                if (written < buffer.Length)
                {
                    await FlushInternalAsync(cancellationToken).ConfigureAwait(false);
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
    }
}
