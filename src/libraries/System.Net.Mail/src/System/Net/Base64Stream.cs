// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    internal sealed class Base64Stream : DelegatedStream, IEncodableStream
    {
        private static ReadOnlySpan<byte> Base64DecodeMap =>
        [
            //0   1   2    3    4    5    6    7    8    9    A    B     C    D    E    F
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // 0
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // 1
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 62,   255, 255, 255,  63, // 2
             52,  53,  54,  55,  56,  57,  58,  59,  60,  61, 255, 255,  255, 255, 255, 255, // 3
            255,   0,   1,   2,   3,   4,   5,   6,   7,   8,   9,  10,   11,  12,  13,  14, // 4
             15,  16,  17,  18,  19,  20,  21,  22,  23,  24,  25,  255, 255, 255, 255, 255, // 5
            255,  26,  27,  28,  29,  30,  31,  32,  33,  34,  35,  36,   37,  38,  39,  40, // 6
             41,  42,  43,  44,  45,  46,  47,  48,  49,  50,  51, 255,  255, 255, 255, 255, // 7
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // 8
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // 9
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // A
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // B
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // C
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // D
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // E
            255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,  255, 255, 255, 255, // F
        ];

        private readonly Base64WriteStateInfo _writeState;
        private ReadStateInfo? _readState;
        private readonly Base64Encoder _encoder;

        //bytes with this value in the decode map are invalid
        private const byte InvalidBase64Value = 255;

        internal Base64Stream(Stream stream, Base64WriteStateInfo writeStateInfo) : base(stream)
        {
            _writeState = new Base64WriteStateInfo();
            _encoder = new Base64Encoder(_writeState, writeStateInfo.MaxLineLength);
        }

        internal Base64Stream(Base64WriteStateInfo writeStateInfo) : base(new MemoryStream())
        {
            _writeState = writeStateInfo;
            _encoder = new Base64Encoder(_writeState, writeStateInfo.MaxLineLength);
        }

        private ReadStateInfo ReadState => _readState ??= new ReadStateInfo();

        internal WriteStateInfoBase WriteState
        {
            get
            {
                Debug.Assert(_writeState != null, "_writeState was null");
                return _writeState;
            }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state) =>
            TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);

        public override void Close()
        {
            if (_writeState != null && WriteState.Length > 0)
            {
                _encoder.AppendPadding();
                FlushInternal();
            }

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

                while (source < end)
                {
                    //space and tab are ok because folding must include a whitespace char.
                    if (*source == '\r' || *source == '\n' || *source == '=' || *source == ' ' || *source == '\t')
                    {
                        source++;
                        continue;
                    }

                    byte s = Base64DecodeMap[*source];

                    if (s == InvalidBase64Value)
                    {
                        throw new FormatException(SR.MailBase64InvalidCharacter);
                    }

                    switch (ReadState.Pos)
                    {
                        case 0:
                            ReadState.Val = (byte)(s << 2);
                            ReadState.Pos++;
                            break;
                        case 1:
                            *dest++ = (byte)(ReadState.Val + (s >> 4));
                            ReadState.Val = unchecked((byte)(s << 4));
                            ReadState.Pos++;
                            break;
                        case 2:
                            *dest++ = (byte)(ReadState.Val + (s >> 2));
                            ReadState.Val = unchecked((byte)(s << 6));
                            ReadState.Pos++;
                            break;
                        case 3:
                            *dest++ = (byte)(ReadState.Val + s);
                            ReadState.Pos = 0;
                            break;
                    }
                    source++;
                }

                return (int)(dest - start);
            }
        }

        public int EncodeBytes(byte[] buffer, int offset, int count) =>
            EncodeBytes(buffer, offset, count, true, true);

        internal int EncodeBytes(byte[] buffer, int offset, int count, bool dontDeferFinalBytes, bool shouldAppendSpaceToCRLF)
        {
            return _encoder.EncodeBytes(buffer, offset, count, dontDeferFinalBytes, shouldAppendSpaceToCRLF);
        }

        public int EncodeString(string value, Encoding encoding) => _encoder.EncodeString(value, encoding);

        public string GetEncodedString() => _encoder.GetEncodedString();

        public override int EndRead(IAsyncResult asyncResult) =>
            TaskToAsyncResult.End<int>(asyncResult);

        public override void EndWrite(IAsyncResult asyncResult) =>
            TaskToAsyncResult.End(asyncResult);

        public override void Flush()
        {
            if (_writeState != null && WriteState.Length > 0)
            {
                FlushInternal();
            }

            base.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_writeState != null && WriteState.Length > 0)
            {
                await base.WriteAsync(WriteState.Buffer.AsMemory(0, WriteState.Length), cancellationToken).ConfigureAwait(false);
                WriteState.Reset();
            }

            await base.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private void FlushInternal()
        {
            base.Write(WriteState.Buffer, 0, WriteState.Length);
            WriteState.Reset();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            while (true)
            {
                // read data from the underlying stream
                int read = base.Read(buffer, offset, count);

                // if the underlying stream returns 0 then there
                // is no more data - ust return 0.
                if (read == 0)
                {
                    return 0;
                }

                // while decoding, we may end up not having
                // any bytes to return pending additional data
                // from the underlying stream.
                read = DecodeBytes(buffer, offset, read);
                if (read > 0)
                {
                    return read;
                }
            }
        }
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsyncCore(buffer, offset, count, cancellationToken);

            async Task<int> ReadAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                while (true)
                {
                    // read data from the underlying stream
                    int read = await base.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);

                    // if the underlying stream returns 0 then there
                    // is no more data - ust return 0.
                    if (read == 0)
                    {
                        return 0;
                    }

                    // while decoding, we may end up not having
                    // any bytes to return pending additional data
                    // from the underlying stream.
                    read = DecodeBytes(buffer, offset, read);
                    if (read > 0)
                    {
                        return read;
                    }
                }
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            int written = 0;

            // do not append a space when writing from a stream since this means
            // it's writing the email body
            while (true)
            {
                written += EncodeBytes(buffer, offset + written, count - written, false, false);
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

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsyncCore(buffer, offset, count, cancellationToken);

            async Task WriteAsyncCore(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                int written = 0;

                // do not append a space when writing from a stream since this means
                // it's writing the email body
                while (true)
                {
                    written += EncodeBytes(buffer, offset + written, count - written, false, false);
                    if (written < count)
                    {
                        await FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private sealed class ReadStateInfo
        {
            internal byte Val { get; set; }
            internal byte Pos { get; set; }
        }
    }
}
